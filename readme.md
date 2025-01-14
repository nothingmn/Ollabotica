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

Here is a breakdown of the settings:

```json
{
  "Bots": [
    {
      "Name": "DevelopmentBot",
      "ServiceType": "Telegram",
      "ChatAuthToken": "7534503146:AAF9N-VFUEBPP46XciOGDANRLTKzVUq1bRM",
      "OllamaUrl": "http://192.168.194.41:8080/ollama/api",
      "OllamaToken": "sk-9906764735554163917f85eb46071b8b",
      "DefaultModel": "llama3.2:3b",
      "AllowedChatIdsRaw": "502225909,1077146670",
      "AdminChatIdsRaw": "502225909",
      "TimeZone": "Pacific Standard Time"
    }
  ]
}
```


```
"Name": "DevelopmentBot"
``` 
- Used in logging to differentiate the log messages per bot
- Used to partition chats when saved and loaded to/from disk

```
"TelegramToken": "7536663146:AAF9N-VFU5555555NRLTKzVUq1bRM",
```
- BotFather issued token

```
"OllamaUrl": "http://192.168.194.41:3000/ollama/api",
"OllamaToken": "eyJhbGciasdfyhdfygjhgfyhsI6IkpXVCJ9.eyJpZCI6IjRmZjIyOTA2LTgwYTEtNDllNS05ODMxLWUzNDQ5YmU3YWFmNCJ9.s4hqSnlWGQX46HI3zeW46ZJLUDhhiOJEhPLwPn5MrRs",
```
- Your Ollama Web UI Instance API endoin (NOT ollama directly)
- The token, grab a JWT token from the UI under your user settings

```
"DefaultModel": "dev-bot",
```
- Use any model from the Workspaces in Ollama Web UI

```
"AllowedChatIdsRaw": "544445909,107787674540",
```
- A list of chat IDs which are allowed to talk to the bot

```
"AdminChatIdsRaw": "544445909"
```
- A list of chat Ids which are allowed to admin the bot (download new models, delete, etc.)


Keep in mind, "Bots" is an array, so have as many as you want!


