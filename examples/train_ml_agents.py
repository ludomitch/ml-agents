# import os
# os.chdir("examples/")
from mlagents.trainers.trainer_util import load_config
from animalai.envs.arena_config import ArenaConfig

from utils.run_options import RunOptions
from utils.run_training import run_training
# from examples.utils.run_options import RunOptions
# from examples.utils.run_training import run_training

trainer_config_path = "configurations/training_configurations/train_ml_agents_config.yaml"
environment_path = "/home/ben/AnimalAI/builds-ml-agents-aaio/aaio"
arena_config_path = "configurations/arena_configurations/train_ml_agents_arenas.yml"
run_id = "train_ml_agents"
base_port = 5005
number_of_environments = 4
number_of_arenas_per_environment = 8

# trainer_config_path=None
# base_port=5004

if __name__ == "__main__":
    args = RunOptions(
        trainer_config=load_config(trainer_config_path),
        env_path=environment_path,
        run_id=run_id,
        base_port=base_port,
        num_envs=number_of_environments,
        arena_config=ArenaConfig(arena_config_path),
        n_arenas_per_env=number_of_arenas_per_environment,
    )

    run_training(0, args)
