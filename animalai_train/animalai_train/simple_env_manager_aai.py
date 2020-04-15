from typing import Dict, List, Optional

from mlagents.trainers.env_manager import EnvironmentStep
from mlagents.trainers.simple_env_manager import SimpleEnvManager

from animalai.envs.arena_config import ArenaConfig


class SimpleEnvManagerAAI(SimpleEnvManager):

    def _reset_env(
            self, config: ArenaConfig = None
    ) -> List[EnvironmentStep]:
        self.env.reset(arenas_configurations=config)
        all_step_result = self._generate_all_results()
        self.previous_step = EnvironmentStep(all_step_result, 0, {})
        return [self.previous_step]
