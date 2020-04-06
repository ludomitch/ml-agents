from abc import ABC, abstractmethod
from typing import Dict

from animalai.envs import AllBrainInfo, BrainParameters


class BaseUnityEnvironment(ABC):
    @abstractmethod
    def step(
        self, vector_action=None, memory=None, text_action=None, value=None
    ) -> AllBrainInfo:
        pass

    @abstractmethod
    def reset(
        self, arenas_configurations=None, train_mode=True, seed=-1
    ) -> AllBrainInfo:
        pass

    @property
    @abstractmethod
    def global_done(self):
        pass

    @property
    @abstractmethod
    def external_brains(self) -> Dict[str, BrainParameters]:
        pass

    @abstractmethod
    def close(self):
        pass
