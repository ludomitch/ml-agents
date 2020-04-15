from mlagents.trainers.trainer_controller import TrainerController
from mlagents_envs.timers import timed
from animalai_train.env_manager_aai import EnvManagerAAI


class TrainerControllerAAI(TrainerController):

    @timed
    def _reset_env(self, env: EnvManagerAAI) -> None:
        """Resets the environment.

        Returns:
            A Data structure corresponding to the initial reset state of the
            environment.
        """
        new_meta_curriculum_config = (
            self.meta_curriculum.get_config() if self.meta_curriculum else None
        )
        env.reset(config=new_meta_curriculum_config)
