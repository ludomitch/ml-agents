import time
from collections import namedtuple
from itertools import product
import pickle
from mlagents.trainers.trainer_util import load_config
from animalai.envs.arena_config import ArenaConfig

from animalai_train.run_options_aai import RunOptionsAAI
from animalai_train.run_training_aai import run_training_aai

trainer_config_path = "configurations/training_configurations/test_speed.yaml"
config = load_config(trainer_config_path)
max_steps = config["AnimalAI"]["max_steps"]
environment_path = "env/aaio"
arena_config_path = "configurations/arena_configurations/test_speed.yml"
results_file = "results.pkl"
run_id = "test_run"
docker_folder = "/aaio"
base_port = 5005

numbers_envs = [1,2]#,4,8,16,32]
numbers_arenas = [1,2]#,4,8,16,32]
Conf = namedtuple('Conf', ['n_envs', 'n_arenas'])


def save_results(key, val):
    try:
        res = pickle.load(open("results.pkl", "rb"))
    except FileNotFoundError:
        res = {}
    res[key] = val
    pickle.dump(res, open("results.pkl", "wb"))


for n_envs, n_arenas in product(numbers_envs, numbers_arenas):
    base_port += n_envs * n_arenas
    print(f'n_envs={n_envs} n_arenas={n_arenas}')
    args = RunOptionsAAI(
        trainer_config=config,
        env_path=environment_path,
        # docker_target_name=docker_folder,
        run_id=run_id,
        base_port=base_port,
        num_envs=n_envs,
        arena_config=ArenaConfig(arena_config_path),
        n_arenas_per_env=n_arenas,
    )

    start = time.time()
    run_training_aai(0, args)
    total = time.time() - start
    print(f'end running in {total} seconds')
    save_results(Conf(n_envs=n_envs, n_arenas=n_arenas), max_steps/total)
