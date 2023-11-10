FROM unityci/editor:ubuntu-2021.3.8f1-linux-il2cpp-3

COPY Assets /unityproject/Assets
COPY ProjectSettings /unityproject/ProjectSettings
COPY Packages /unityproject/Packages

ARG USERNAME
ARG PASSWORD
ARG SERIAL
ENV USERNAME=$USERNAME
ENV PASSWORD=$PASSWORD
ENV SERIAL=$SERIAL

CMD \
/usr/bin/unity-editor -batchmode -quit -logFile /dev/stdout -serial $SERIAL -username $USERNAME -password $PASSWORD && \
/usr/bin/unity-editor -batchmode -nographics -silent-crashes -logFile /dev/stdout -stackTraceLogType None -projectPath /unityproject -runTests -requiresPlayMode true -testFilter 'StressTests.ClientPlayForever' || \
/usr/bin/unity-editor -batchmode -quit -returnlicense -serial $SERIAL -username $USERNAME -password $PASSWORD
