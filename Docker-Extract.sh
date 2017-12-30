mkdir -p ./Publish
DIRECTORY="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"

DOCKER_VOLUME="$DIRECTORY/Publish:/tmp"
DOCKER_IMAGE="psythyst/psythyst-cli:latest"

docker run -it --rm -v $DOCKER_VOLUME --entrypoint cp $DOCKER_IMAGE -r /Psythyst/. /tmp