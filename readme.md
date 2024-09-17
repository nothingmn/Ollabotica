# Ollabotica - Yet another telegram bot for Ollama

It combines "Ollama" with a playful twist on "bot" and "automatica," giving it a dynamic and tech-savvy vibe. If you're looking for something unique, this could stand out and convey that it's a powerful and engaging framework.


## Notes:
This has been tested against the Ollama Open Web UI's API and should work against work just against their typical API without Open Web UI.

## Features:
1. Multiple Telegram Bots against multple Ollama models.
1. Multiple chat clients against any of these Telgram bots
1. Each bot can be configured to use a different model, and Ollama endpoint.
1. Start a new conversation, with an optional new chat prompt
1. Whitelist for telegram chatids to prevent unauthorized access
1. Admin Whitelist for telegram chatids to prevent unauthorized access
1. List, Save, Load, and Delete conversations
1. Skip Ollama API, and just echo back the user's message (for testing)
1. Optional Prompt for each chat message
1. Flexible hosting options, run the local executable or hostable via Docker and docker-compose
1. All configuration is done via a single file, appsettings.json
1. Optionally you can mount a volume for the Chat History, via docker settings
1. List models in the chat
1. Hot swap a model for a bot in a chat
1. Ask for debug information
1. Support for Slack - needs deeper testing from the community (Slack is not my thing)
1. Support for Discord - DMs or in channels with mentions.  

----

# Getting Started

## Running with Docker (preferred)

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
docker run -d -v ./appsettings.json:/app/appsettings.dev.json --restart unless-stopped robchartier/ollabotica 
```

All settings have been documented in the (appsettings.sample.json.txt) file.


## Docker Compose, if your into that sort of thing

```
version: '3'
services:
  service1:
    image: robchartier/ollabotica:latest
    container_name: telegram-bot-1
    restart: unless-stopped
    volumes:
      - ./appsettings.json:/app/appsettings.json
      - ./chats:/app/chats
    environment:
      - ASPNETCORE_ENVIRONMENT=Development

```
