import uuid
from typing import Optional, List
from mlagents_envs.environment import UnityEnvironment
from mlagents_envs.side_channel.raw_bytes_channel import RawBytesChannel
from mlagents_envs.side_channel.side_channel import SideChannel
from animalai.envs.arena_config import ArenaConfig


class AnimalAIEnvironment(UnityEnvironment):

    API_VERSION = "aai-1.0.0"

    def __init__(
            self,
            file_name: Optional[str] = None,
            worker_id: int = 0,
            base_port: int = 5005,
            seed: int = 0,
            docker_training: bool = False,
            n_arenas: int = 1,
            play: bool = False,
            arenas_configurations: ArenaConfig = None,
            inference: bool = False,
            camera_width: int = None,
            camera_height: int = None,
            side_channels: Optional[List[SideChannel]] = None,
    ):

        args = self.executable_args(n_arenas, play, camera_height, camera_width)
        self.arenas_parameters_side_channel = RawBytesChannel(channel_id=uuid.UUID("9c36c837-cad5-498a-b675-bc19c9370072"))
        side_channels = [] if side_channels is None else side_channels
        super().__init__(file_name=file_name,
                       worker_id=worker_id,
                       base_port=base_port,
                       seed=seed,
                       docker_training=docker_training,
                       no_graphics=False,
                       timeout_wait=60,
                       args=args,
                       side_channels=side_channels+[self.arenas_parameters_side_channel],
                       )
        self.reset()
        if arenas_configurations:
            self.reset(arenas_configurations)
            # arenas_configurations_proto = arenas_configurations.to_proto()
            # arenas_configurations_proto_string = arenas_configurations_proto.SerializeToString()
            # arenas_parameters_side_channel.send_raw_data(bytearray(arenas_configurations_proto_string))
            # env.reset()

    def reset(self, arenas_configurations=None):
        if arenas_configurations:
            arenas_configurations_proto = arenas_configurations.to_proto()
            arenas_configurations_proto_string = arenas_configurations_proto.SerializeToString()
            self.arenas_parameters_side_channel.send_raw_data(bytearray(arenas_configurations_proto_string))
        super().reset()

    @staticmethod
    def executable_args(n_arenas, play, camera_height, camera_width):
        args = ["--playerMode"]
        if play:
            args.append("1")
        else:
            args.append("0")
        args.append("--numberOfArenas")
        args.append(str(n_arenas))
        if camera_width:
            args.append("--cameraWidth")
            args.append(str(camera_width))
        if camera_height:
            args.append("--cameraHeight")
            args.append(str(camera_height))

        return args
