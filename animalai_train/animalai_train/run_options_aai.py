from typing import NamedTuple, Dict, Optional, List
from animalai.envs.arena_config import ArenaConfig


class RunOptionsAAI(NamedTuple):
    trainer_config: Dict = None
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
    n_arenas_per_env: int = 1
    arena_config: ArenaConfig = None
    camera_width: int = 84
    camera_height: int = 84
