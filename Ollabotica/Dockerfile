# Stage 1: Build the application
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

# Set the working directory inside the container
WORKDIR /app

# Add the custom NuGet source before restoring dependencies
RUN dotnet nuget add source https://pkgs.dev.azure.com/tgbots/Telegram.Bot/_packaging/release/nuget/v3/index.json -n Telegram.Bot

# Copy the .csproj file and restore any dependencies from both default and custom sources
COPY Ollabotica.csproj ./
RUN dotnet restore

# Copy the rest of the application source code
COPY . ./

# Build the application in Release mode
RUN dotnet publish -c Release -o /app/publish

# Stage 2: Use the ASP.NET Core runtime to run the application
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime

# Set the working directory in the final runtime container
WORKDIR /app

# Copy the built application from the previous stage
COPY --from=build /app/publish .

# Set the entry point to the console application
ENTRYPOINT ["dotnet", "Ollabotica.dll"]
