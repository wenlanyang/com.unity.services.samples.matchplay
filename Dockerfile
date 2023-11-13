FROM unityci/editor:ubuntu-2021.3.8f1-linux-il2cpp-3

VOLUME /unityproject

COPY Assets /repo/Assets
COPY ProjectSettings /repo/ProjectSettings
COPY Packages /repo/Packages

ARG USERNAME
ARG PASSWORD
ARG SERIAL
ENV USERNAME=$USERNAME
ENV PASSWORD=$PASSWORD
ENV SERIAL=$SERIAL

CMD \
cp -a /repo/. /unityproject/ && \
/usr/bin/unity-editor -quit -logFile /dev/stdout -serial $SERIAL -username $USERNAME -password $PASSWORD && \
/usr/bin/unity-editor -nographics -silent-crashes -logFile /dev/stdout -stackTraceLogType None -projectPath /unityproject -runTests -requiresPlayMode true -testFilter 'StressTests.ClientPlayForever' || \
tail -f /dev/null
