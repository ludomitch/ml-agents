from mlagents.trainers.trainer_controller import TrainerController
from mlagents.trainers.env_manager import EnvManager
from mlagents_envs.timers import timed


class TrainerControllerAAI(TrainerController):

    @timed
    def _reset_env(self, env: EnvManager) -> None:
        """Resets the environment.

        Returns:
            A Data structure corresponding to the initial reset state of the
            environment.
        """
        new_meta_curriculum_config = (
            self.meta_curriculum.get_config() if self.meta_curriculum else None
        )
        env.reset(config=new_meta_curriculum_config)
