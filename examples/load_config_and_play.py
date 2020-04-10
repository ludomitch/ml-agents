import os
from animalai.envs.arena_config import ArenaConfig
from animalai.envs.environment import AnimalAIEnvironment

env_path = "/home/ben/AnimalAI/builds-ml-agents-aaio/aaio"  # TODO: 'exemples/env/aaio'
env_path = None
easy_path = "configurations/arena_configurations/easy/"
hard_path = "configurations/arena_configurations/hard/"
port = 5005
play_hard = True

if play_hard:
    configurations = [ArenaConfig(easy_path + config) for config in os.listdir(easy_path)]
else:
    configurations = [ArenaConfig(hard_path + config) for config in os.listdir(hard_path)]

environment = AnimalAIEnvironment(
    file_name=env_path,
    base_port=5005,
    play=True
)

agent_group = environment.get_agent_groups()[0]
environment.reset(configurations[0])

i = 1
n_configurations = len(configurations)
try:
    while True:
        step_result = environment.get_step_result(agent_group)
        if any(step_result.done):
            environment.reset(arenas_configurations=configurations[i])
            i += 1
except KeyboardInterrupt:
    environment.close()
