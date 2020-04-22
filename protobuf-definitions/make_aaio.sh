# variables

# GRPC-TOOLS required. Install with `nuget install Grpc.Tools`.
# Then un-comment and replace [DIRECTORY] with location of files.
# For example, on macOS, you might have something like:
COMPILER=Grpc.Tools.1.14.1/tools/linux_x64
# COMPILER=[DIRECTORY]

SRC_DIR=proto/animalai/communicator_objects
DST_DIR_C=../Project/Assets/AnimalAIOlympics/TrainEnv/Scripts/CommunicatorObjects
DST_DIR_P=../animalai
PROTO_PATH=proto
PYTHON_PACKAGE=mlagents_envs/communicator_objects

# clean
rm -rf $DST_DIR_C
rm -rf $DST_DIR_P/$PYTHON_PACKAGE
mkdir -p $DST_DIR_C
mkdir -p $DST_DIR_P/$PYTHON_PACKAGE

# generate proto objects in python and C#

$COMPILER/protoc --proto_path=proto --csharp_opt=internal_access --csharp_out $DST_DIR_C $SRC_DIR/*.proto
$COMPILER/protoc --proto_path=proto --python_out=$DST_DIR_P --mypy_out=$DST_DIR_P $SRC_DIR/*.proto

# Generate the init file for the python module
# rm -f $DST_DIR_P/$PYTHON_PACKAGE/__init__.py
for FILE in $DST_DIR_P/$PYTHON_PACKAGE/*.py
do
FILE=${FILE##*/}
# echo from .$(basename $FILE) import \* >> $DST_DIR_P/$PYTHON_PACKAGE/__init__.py
echo from .${FILE%.py} import \* >> $DST_DIR_P/$PYTHON_PACKAGE/__init__.py
done

# Remove the __init__.py file since it is not needed
rm $DST_DIR_P/$PYTHON_PACKAGE/__init__.py
touch $DST_DIR_P/$PYTHON_PACKAGE/__init__.py
