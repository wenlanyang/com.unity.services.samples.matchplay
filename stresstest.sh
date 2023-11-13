#!/usr/bin/env bash

function finish {
    /usr/bin/unity-editor -logFile /dev/stdout -quit -returnlicense
}
trap finish EXIT

/usr/bin/unity-editor \
  -quit -logFile /dev/stdout \
  -serial $SERIAL -username $USERNAME -password $PASSWORD

/usr/bin/unity-editor \
  -nographics -silent-crashes \
  -logFile /dev/stdout -stackTraceLogType None \
  -projectPath /unityproject \
  -runTests -requiresPlayMode true -testNames 'Matchplay.Tests.StressTests.ClientPlayForever'

tail -f /dev/null
