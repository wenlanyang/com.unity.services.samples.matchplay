FROM unityci/editor:ubuntu-2021.3.8f1-linux-il2cpp-3

COPY Assets /repo/Assets
COPY ProjectSettings /repo/ProjectSettings
COPY Packages /repo/Packages
COPY stresstest.sh /repo/stresstest.sh
RUN chmod +x /repo/stresstest.sh

ARG USERNAME
ARG PASSWORD
ARG SERIAL
ENV USERNAME=$USERNAME
ENV PASSWORD=$PASSWORD
ENV SERIAL=$SERIAL

VOLUME /unityproject

CMD cp -a /repo/. /unityproject/ && /unityproject/stresstest.sh
