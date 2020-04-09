import os
import glob
import shutil
import logging

import numpy as np
from typing import Callable, Optional, List, NamedTuple, Dict

from mlagents.trainers.meta_curriculum import MetaCurriculum
from mlagents.trainers.sampler_class import SamplerManager
from mlagents.trainers.exception import SamplerException
# from mlagents.trainers.learn import RunOptions
from mlagents_envs.timers import hierarchical_timer
from mlagents.trainers.stats import (
    TensorboardWriter,
    CSVWriter,
    StatsReporter,
    GaugeWriter,
)
from mlagents_envs.side_channel.engine_configuration_channel import EngineConfig
from mlagents_envs.base_env import BaseEnv
from mlagents.trainers.subprocess_env_manager import SubprocessEnvManager
from mlagents_envs.side_channel.side_channel import SideChannel
from mlagents.trainers.trainer_util import load_config, TrainerFactory
from mlagents.trainers.trainer_controller import TrainerController
from mlagents_envs.exception import UnityEnvironmentException

from animalai.envs.environment import AnimalAIEnvironment
from animalai.envs.arena_config import ArenaConfig


class RunOptions():
    trainer_config: Dict
    debug: bool = False
    seed: int = 0
    env_path: Optional[str] = None
    run_id: str = "ppo"
    load_model: bool = False
    train_model: bool = True
    save_freq: int = 50000
    keep_checkpoints: int = 5
    base_port: int = 5005
    num_envs: int = 1
    curriculum_config: Optional[Dict] = None
    lesson: int = 0
    no_graphics: bool = False
    multi_gpu: bool = False
    sampler_config: Optional[Dict] = None
    docker_target_name: Optional[str] = None
    env_args: Optional[List[str]] = None
    cpu: bool = False
    width: int = 84
    height: int = 84
    quality_level: int = 5
    time_scale: float = 20
    target_frame_rate: int = -1
    n_arenas_per_env: int = 1
    camera_width: int = 84
    camera_height: int = 84


def run_training(run_seed: int, arena_config: ArenaConfig, options: RunOptions) -> None:
    """
    Launches training session.
    :param options: parsed command line arguments
    :param run_seed: Random seed used for training.
    :param run_options: Command line arguments for training.
    """
    with hierarchical_timer("run_training.setup"):
        # Recognize and use docker volume if one is passed as an argument
        if not options.docker_target_name:
            model_path = f"./models/{options.run_id}"
            summaries_dir = "./summaries"
        else:
            model_path = f"/{options.docker_target_name}/models/{options.run_id}"
            summaries_dir = f"/{options.docker_target_name}/summaries"
        port = options.base_port

        # Configure CSV, Tensorboard Writers and StatsReporter
        # We assume reward and episode length are needed in the CSV.
        csv_writer = CSVWriter(
            summaries_dir,
            required_fields=[
                "Environment/Cumulative Reward",
                "Environment/Episode Length",
            ],
        )
        tb_writer = TensorboardWriter(summaries_dir)
        gauge_write = GaugeWriter()
        StatsReporter.add_writer(tb_writer)
        StatsReporter.add_writer(csv_writer)
        StatsReporter.add_writer(gauge_write)

        if options.env_path is None:
            port = AnimalAIEnvironment.DEFAULT_EDITOR_PORT
        env_factory = create_environment_factory(
            options.env_path,
            options.docker_target_name,
            run_seed,
            port,
            options.n_arenas_per_env,
            arena_config,
            options.camera_width,
            options.camera_height
        )
        engine_config = EngineConfig(
            options.width,
            options.height,
            options.quality_level,
            options.time_scale,
            options.target_frame_rate,
        )
        env_manager = SubprocessEnvManager(env_factory, engine_config, options.num_envs)
        maybe_meta_curriculum = try_create_meta_curriculum(
            options.curriculum_config, env_manager, options.lesson
        )
        sampler_manager, resampling_interval = create_sampler_manager(
            options.sampler_config, run_seed
        )
        trainer_factory = TrainerFactory(
            options.trainer_config,
            summaries_dir,
            options.run_id,
            model_path,
            options.keep_checkpoints,
            options.train_model,
            options.load_model,
            run_seed,
            maybe_meta_curriculum,
            options.multi_gpu,
        )
        # Create controller and begin training.
        tc = TrainerController(
            trainer_factory,
            model_path,
            summaries_dir,
            options.run_id,
            options.save_freq,
            maybe_meta_curriculum,
            options.train_model,
            run_seed,
            sampler_manager,
            resampling_interval,
        )

    # Begin training
    try:
        tc.start_learning(env_manager)
    finally:
        env_manager.close()
        # write_timing_tree(summaries_dir, options.run_id)


def create_environment_factory(
        env_path: Optional[str],
        docker_target_name: Optional[str],
        seed: Optional[int],
        start_port: int,
        n_arenas_per_env: int,
        arenas_configurations: ArenaConfig,
        camera_width: Optional[int],
        camera_height: Optional[int]
) -> Callable[[int, List[SideChannel]], BaseEnv]:
    if env_path is not None:
        launch_string = AnimalAIEnvironment.validate_environment_path(env_path)
        if launch_string is None:
            raise UnityEnvironmentException(
                f"Couldn't launch the {env_path} environment. Provided filename does not match any environments."
            )
    docker_training = docker_target_name is not None
    if docker_training and env_path is not None:
        #     Comments for future maintenance:
        #         Some OS/VM instances (e.g. COS GCP Image) mount filesystems
        #         with COS flag which prevents execution of the Unity scene,
        #         to get around this, we will copy the executable into the
        #         container.
        # Navigate in docker path and find env_path and copy it.
        env_path = prepare_for_docker_run(docker_target_name, env_path)
    seed_count = 10000
    seed_pool = [np.random.randint(0, seed_count) for _ in range(seed_count)]

    def create_unity_environment(
            worker_id: int, side_channels: List[SideChannel]
    ) -> AnimalAIEnvironment:
        env_seed = seed
        if not env_seed:
            env_seed = seed_pool[worker_id % len(seed_pool)]
        return AnimalAIEnvironment(
            file_name=env_path,
            worker_id=worker_id,
            base_port=start_port,
            seed=env_seed,
            docker_training=docker_training,
            n_arenas=n_arenas_per_env,
            arenas_configurations=arenas_configurations,
            camera_width=camera_width,
            camera_height=camera_height,
            side_channels=side_channels,
        )

    return create_unity_environment


def create_sampler_manager(sampler_config, run_seed=None):
    resample_interval = None
    if sampler_config is not None:
        if "resampling-interval" in sampler_config:
            # Filter arguments that do not exist in the environment
            resample_interval = sampler_config.pop("resampling-interval")
            if (resample_interval <= 0) or (not isinstance(resample_interval, int)):
                raise SamplerException(
                    "Specified resampling-interval is not valid. Please provide"
                    " a positive integer value for resampling-interval"
                )

        else:
            raise SamplerException(
                "Resampling interval was not specified in the sampler file."
                " Please specify it with the 'resampling-interval' key in the sampler config file."
            )

    sampler_manager = SamplerManager(sampler_config, run_seed)
    return sampler_manager, resample_interval


def try_create_meta_curriculum(
        curriculum_config: Optional[Dict], env: SubprocessEnvManager, lesson: int
) -> Optional[MetaCurriculum]:
    if curriculum_config is None:
        return None
    else:
        meta_curriculum = MetaCurriculum(curriculum_config)
        # TODO: Should be able to start learning at different lesson numbers
        # for each curriculum.
        meta_curriculum.set_all_curricula_to_lesson_num(lesson)
        return meta_curriculum


def prepare_for_docker_run(docker_target_name, env_path):
    for f in glob.glob(
            "/{docker_target_name}/*".format(docker_target_name=docker_target_name)
    ):
        if env_path in f:
            try:
                b = os.path.basename(f)
                if os.path.isdir(f):
                    shutil.copytree(f, "/ml-agents/{b}".format(b=b))
                else:
                    src_f = "/{docker_target_name}/{b}".format(
                        docker_target_name=docker_target_name, b=b
                    )
                    dst_f = "/ml-agents/{b}".format(b=b)
                    shutil.copyfile(src_f, dst_f)
                    os.chmod(dst_f, 0o775)  # Make executable
            except Exception as e:
                logging.getLogger("mlagents.trainers").info(e)
    env_path = "/ml-agents/{env_path}".format(env_path=env_path)
    return env_path


if __name__ == "__main__":
    args = RunOptions()
    args.trainer_config = load_config("/home/ben/AnimalAI/ml-agents/dev/trainer_config.yaml")
    # args.debug: bool = False
    # args.seed: int = 0
    # args.env_path ="/home/ben/AnimalAI/builds-ml-agents-aaio/aaio"
    args.env_path = None
    # args.run_id: str = "ppo"
    args.load_model: bool = True
    # args.train_model: bool = True
    # args.save_freq: int = 50000
    # args.keep_checkpoints: int = 5
    # args.base_port: int = 5005
    # args.num_envs: int = 1
    # args.curriculum_config: Optional[Dict] = None
    # args.lesson: int = 0
    # args.no_graphics: bool = False
    # args.multi_gpu: bool = False
    # args.sampler_config: Optional[Dict] = None
    # args.docker_target_name: Optional[str] = False
    # args.env_args: Optional[List[str]] = None
    # args.cpu: bool = False
    # args.width = 800
    # args.height = 600
    args.quality_level: int = 1
    args.time_scale: float = 300
    args.target_frame_rate: int = -1

    arena_config_path = "/home/ben/AnimalAI/ml-agents/dev/conf_simple.yml"
    arena_config = ArenaConfig(arena_config_path)

    run_training(0, arena_config, args)
