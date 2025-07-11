# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution file and restore dependencies
COPY aevatar-workshop.sln .
COPY Directory.Build.props .
COPY Directory.Packages.props .
COPY NuGet.docker.config ./NuGet.config

# Copy project files for dependency resolution
COPY src/Aevatar.Workshop.Host/*.csproj ./src/Aevatar.Workshop.Host/
COPY src/Aevatar.Workshop.GAgent/*.csproj ./src/Aevatar.Workshop.GAgent/
COPY src/Aevatar.Workshop.AIRouterWorkflowGAgent/*.csproj ./src/Aevatar.Workshop.AIRouterWorkflowGAgent/

# Copy gAgents project files
COPY gAgents/src/Aevatar.GAgents.Basic/*.csproj ./gAgents/src/Aevatar.GAgents.Basic/
COPY gAgents/src/Aevatar.GAgents.AI.Abstractions/*.csproj ./gAgents/src/Aevatar.GAgents.AI.Abstractions/
COPY gAgents/src/Aevatar.GAgents.AIGAgent/*.csproj ./gAgents/src/Aevatar.GAgents.AIGAgent/
COPY gAgents/src/Aevatar.GAgents.MCP/*.csproj ./gAgents/src/Aevatar.GAgents.MCP/
COPY gAgents/src/Aevatar.GAgents.SemanticKernel/*.csproj ./gAgents/src/Aevatar.GAgents.SemanticKernel/
COPY gAgents/src/Aevatar.GAgents.Router/*.csproj ./gAgents/src/Aevatar.GAgents.Router/
COPY gAgents/src/Aevatar.GAgents.Telegram/*.csproj ./gAgents/src/Aevatar.GAgents.Telegram/
COPY gAgents/src/Aevatar.GAgents.Twitter/*.csproj ./gAgents/src/Aevatar.GAgents.Twitter/
COPY gAgents/src/Aevatar.GAgents.ChatAgent/*.csproj ./gAgents/src/Aevatar.GAgents.ChatAgent/
COPY gAgents/src/Aevatar.GAgents.SocialGAgent/*.csproj ./gAgents/src/Aevatar.GAgents.SocialGAgent/
COPY gAgents/src/Aevatar.GAgents.GroupChat/*.csproj ./gAgents/src/Aevatar.GAgents.GroupChat/
COPY gAgents/src/Aevatar.GAgents.Executor/*.csproj ./gAgents/src/Aevatar.GAgents.Executor/
COPY gAgents/src/Aevatar.GAgents.MultiAIChatGAgent/*.csproj ./gAgents/src/Aevatar.GAgents.MultiAIChatGAgent/
COPY gAgents/src/Aevatar.GAgents.GraphRetrievalAgent/*.csproj ./gAgents/src/Aevatar.GAgents.GraphRetrievalAgent/
COPY gAgents/src/Aevatar.GAgents.AElf/*.csproj ./gAgents/src/Aevatar.GAgents.AElf/
COPY gAgents/src/Aevatar.GAgents.Pumpfun/*.csproj ./gAgents/src/Aevatar.GAgents.Pumpfun/

# Copy psi-omni project files if they exist (create empty directories to avoid warnings)
RUN mkdir -p psi-omni/src/PsiGAgent.Omni psi-omni/src/PsiGAgent.Plugins psi-omni/src/PsiGAgent.Common

# Copy test project files (create empty directories to avoid warnings)
RUN mkdir -p gAgents/test/Aevatar.GAgents.AIGAgent.Test

# Copy optional directories (using shell to handle missing directories)
RUN if [ -d "psi-omni" ]; then echo "Found psi-omni"; else echo "psi-omni not found, creating empty structure"; fi
RUN if [ -d "gAgents/test" ]; then echo "Found test projects"; else echo "test projects not found, creating empty structure"; fi

# Copy configuration files
COPY gAgents/Directory.Build.props ./gAgents/
COPY gAgents/Directory.Packages.props ./gAgents/
COPY gAgents/NuGet.docker.config ./gAgents/NuGet.Config

# Clear NuGet cache and restore
RUN dotnet nuget locals all --clear
RUN dotnet restore src/Aevatar.Workshop.Host/Aevatar.Workshop.Host.csproj --no-cache --force

# Copy all source files
COPY src/ ./src/
COPY gAgents/src/ ./gAgents/src/

# Build the application
RUN dotnet build src/Aevatar.Workshop.Host/Aevatar.Workshop.Host.csproj -c Release --no-restore
RUN dotnet publish src/Aevatar.Workshop.Host/Aevatar.Workshop.Host.csproj -c Release --no-build -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime

# Install Node.js for MCP servers
RUN apt-get update && apt-get install -y \
    curl \
    gnupg \
    && curl -fsSL https://deb.nodesource.com/setup_20.x | bash - \
    && apt-get install -y nodejs \
    && npm install -g npm@latest \
    && apt-get clean \
    && rm -rf /var/lib/apt/lists/*

# Install global MCP packages
RUN npm install -g @modelcontextprotocol/server-github minimax-mcp-js

# Create working directory
WORKDIR /app

# Copy published application
COPY --from=build /app/publish .

# Copy configuration files
COPY src/Aevatar.Workshop.Host/appsettings.json .
COPY src/Aevatar.Workshop.Host/appsettings.secrets.json .

# Create logs directory
RUN mkdir -p /app/Logs

# Set environment variables
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:80
ENV DOTNET_ENVIRONMENT=Production

# Expose ports
EXPOSE 80
EXPOSE 11111
EXPOSE 30000

# Set the entrypoint
ENTRYPOINT ["dotnet", "Aevatar.Workshop.Host.dll"]