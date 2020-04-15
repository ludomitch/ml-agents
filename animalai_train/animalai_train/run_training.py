from typing import Optional, Dict

from mlagents.trainers.meta_curriculum import MetaCurriculum
from mlagents_envs.timers import hierarchical_timer
from mlagents.trainers.stats import (
    TensorboardWriter,
    CSVWriter,
    StatsReporter,
    GaugeWriter,
)
from mlagents_envs.side_channel.engine_configuration_channel import EngineConfig
# from mlagents.trainers.subprocess_env_manager import SubprocessEnvManager
from mlagents.trainers.trainer_util import TrainerFactory
from mlagents.trainers.trainer_controller import TrainerController
from mlagents.trainers.learn import create_sampler_manager

from animalai.envs.environment import AnimalAIEnvironment

from animalai_train.subprocess_env_manager_aai import SubprocessEnvManagerAAI
from animalai_train.run_options import RunOptions
from animalai_train.environment_factory import create_environment_factory


def run_training(run_seed: int, options: RunOptions) -> None:
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
            options.arena_config,
            options.camera_width,
            options.camera_height
        )
        engine_config = EngineConfig(
            options.width,
            options.height,
            AnimalAIEnvironment.QUALITY_LEVEL.train,
            AnimalAIEnvironment.TIMESCALE.train,
            AnimalAIEnvironment.TARGET_FRAME_RATE.train,
        )
        env_manager = SubprocessEnvManagerAAI(env_factory, engine_config, options.num_envs)
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


def try_create_meta_curriculum(
        curriculum_config: Optional[Dict], env: SubprocessEnvManagerAAI, lesson: int
) -> Optional[MetaCurriculum]:
    # TODO: may need rewrite for arena configuration curricula
    if curriculum_config is None:
        return None
    else:
        meta_curriculum = MetaCurriculum(curriculum_config)
        # TODO: Should be able to start learning at different lesson numbers
        # for each curriculum.
        meta_curriculum.set_all_curricula_to_lesson_num(lesson)
        return meta_curriculum
