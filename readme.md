Ollabotica - Yet another telegram bot for Ollama
====


Notes:
----
This has been tested against the Ollama Web UI's API


Running with Docker (preferred)
----

Copy the file: 
```
appsettings.sample.json.txt
```

to

```
appsettings.json
```


Edit the file to include your bot token and other settings.

Run the following command:

```
docker run -d -v ./appsettings.json:/app/appsettings.dev.json --restart unless-stopped robchartier/Ollabotica 
```

All settings have been documented in the (appsettings.sample.json.txt) file.


Docker Compose, if your into that sort of thing
----

```
version: '3'
services:
  service1:
    image: robchartier/Ollabotica:latest
    container_name: telegram-bot-1
    restart: unless-stopped
    volumes:
      - ./appsettings.json:/app/appsettings.json
    environment:
      - ASPNETCORE_ENVIRONMENT=Development

```
