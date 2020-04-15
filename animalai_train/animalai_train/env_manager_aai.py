from abc import abstractmethod
from typing import List
from mlagents.trainers.env_manager import EnvManager, EnvironmentStep
from animalai.envs.arena_config import ArenaConfig


class EnvManagerAAI(EnvManager):

    @abstractmethod
    def _reset_env(self, config: ArenaConfig = None) -> List[EnvironmentStep]:
        pass

    def reset(self, config: ArenaConfig = None) -> int:
        for manager in self.agent_managers.values():
            manager.end_episode()
        # Save the first step infos, after the reset.
        # They will be processed on the first advance().
        self.first_step_infos = self._reset_env(config)
        return len(self.first_step_infos)