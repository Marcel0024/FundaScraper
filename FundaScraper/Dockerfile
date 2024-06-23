FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base

RUN apt-get update \
    && apt-get install -y wget gnupg xvfb  \
    && wget -q -O - https://dl-ssl.google.com/linux/linux_signing_key.pub | gpg --dearmor -o /usr/share/keyrings/googlechrome-linux-keyring.gpg \
    && sh -c 'echo "deb [arch=amd64 signed-by=/usr/share/keyrings/googlechrome-linux-keyring.gpg] http://dl.google.com/linux/chrome/deb/ stable main" >> /etc/apt/sources.list.d/google.list' \
    && apt-get update \
    && apt-get install -y google-chrome-stable fonts-ipafont-gothic fonts-wqy-zenhei fonts-thai-tlwg fonts-khmeros fonts-kacst fonts-freefont-ttf libxss1 dbus dbus-x11 \
    --no-install-recommends \
    && service dbus start \
    && rm -rf /var/lib/apt/lists/*

ENV PUPPETEER_EXECUTABLE_PATH "/usr/bin/google-chrome-stable"

RUN apt-get update
RUN apt-get install -y xvfb

USER app
WORKDIR /app

COPY ./publish .

ENTRYPOINT ["/bin/sh", "-c", "xvfb-run -a $@", ""]
CMD ["dotnet", "FundaScraper.dll"]

