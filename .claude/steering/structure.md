# Project Structure

## Architecture Overview
The Aevatar Workshop follows a clean architecture pattern with clear separation of concerns across three main components:

```
aevatar-workshop/
├── .claude/steering/           # Steering documents
├── src/                        # Source code
│   ├── Aevatar.Workshop.Host/  # Orleans Silo (Backend)
│   ├── Aevatar.Workshop.Client/# Web API & UI (Frontend)
│   └── Aevatar.Workshop.GAgent/# GAgent implementations
├── test/                       # Test projects
├── docs/                       # Documentation
├── docker/                     # Docker configuration
└── scripts/                    # Automation scripts
```

## Source Code Organization

### Aevatar.Workshop.Host (Backend)
**Purpose**: Orleans silo hosting GAgents and providing core infrastructure
- **Entry Point**: `Program.cs` - Application startup and configuration
- **Hosting**: `WorkshopHostModule.cs` - Dependency injection and service registration
- **Orleans Configuration**: `OrleansHostExtension.cs` - Orleans cluster setup
- **Kernel Factory**: `KernelFactory.cs` - Semantic Kernel integration
- **Demo Plugin**: `DemoPlugin.cs` - Plugin system for demos

### Aevatar.Workshop.Client (Frontend)
**Purpose**: Web API and user interface for interacting with agents
- **Entry Point**: `Program.cs` - Web application startup
- **Controllers**: `Controllers/` - API endpoints for different demos
  - `SmartHomeDemoController.cs` - Smart home interactions
  - `GAgentServiceController.cs` - GAgent lifecycle management
  - `MCPDemoController.cs` - MCP integration examples
  - `EventHandlerDemoController.cs` - Event handling demos
- **Services**: `Services/` - Business logic and service implementations
- **Static Content**: `wwwroot/` - Web interface and documentation

### Aevatar.Workshop.GAgent (Agent Library)
**Purpose**: Shared GAgent implementations and event definitions
- **Base Classes**: `WorkshopAIGAgentBase.cs` - Common AI agent base class
- **Event Definitions**: `Events/` - Event contracts for agent communication
- **Agent Implementations**: `GAgents/` - Specific agent implementations
  - `SmartHome/` - Smart home device agents
  - `Demo/` - Demo and tutorial agents
  - `AI/` - AI-powered agents

## GAgent Implementation Patterns

### File Organization
```
GAgents/
├── SmartHome/
│   ├── HomeAIGAgent.cs         # AI coordinator
│   ├── LightGAgent.cs          # Smart lighting
│   ├── ThermostatGAgent.cs     # Climate control
│   ├── SecurityGAgent.cs       # Security system
│   └── CurtainGAgent.cs        # Window coverings
├── Demo/
│   ├── CoordinatorDemoGAgent.cs
│   ├── EventLoggerGAgent.cs
│   ├── NotificationGAgent.cs
│   └── ProcessingGAgent.cs
└── AI/
    ├── DynamicToolAIGAgent.cs
    ├── ToolCallingAIGAgent.cs
    └── TimeConverterGAgent.cs
```

### GAgent Structure Convention
Each GAgent follows a consistent structure:
1. **State Class**: `[GenerateSerializer]` marked state object
2. **State Log Event**: Event for state transitions
3. **Event Handlers**: `[EventHandler]` marked methods
4. **Public Interface**: Contract for agent interaction
5. **Configuration**: Optional configuration class

### Event Organization
```
Events/
├── SmartHomeEvents.cs          # Smart home specific events
├── EventHandlerDemoEvents.cs   # Event handling demo events
├── ConfigUpdateEvent.cs        # Configuration management events
├── RecordEvent.cs              # Recording and logging events
└── ECommerceEvents.cs          # E-commerce demo events
```

## Testing Structure

### Test Projects
```
test/
├── Aevatar.Workshop.TestBase/  # Shared test infrastructure
├── Aevatar.Workshop.Tests/     # Main test suite
└── Aevatar.Workshop.GuideGAgents/ # Tutorial example tests
```

### Test Organization
- **Unit Tests**: Individual GAgent functionality
- **Integration Tests**: Multi-agent interaction scenarios
- **Demo Tests**: Smart home and other demo validation
- **Performance Tests**: Concurrent operation handling

## Naming Conventions

### Classes and Interfaces
- **GAgent Classes**: `{Feature}GAgent` (e.g., `LightGAgent`)
- **Interfaces**: `I{Feature}GAgent` (e.g., `ILightGAgent`)
- **State Classes**: `{Agent}State` (e.g., `LightState`)
- **Events**: `{Action}Event` (e.g., `TurnOnLightCommand`)
- **State Log Events**: `{Agent}StateLogEvent` (e.g., `LightStateLogEvent`)

### Files and Folders
- **Agent Files**: PascalCase (e.g., `HomeAIGAgent.cs`)
- **Event Files**: PascalCase (e.g., `SmartHomeEvents.cs`)
- **Test Files**: `{Feature}Tests.cs` (e.g., `SmartHomeDemoTests.cs`)
- **Documentation**: kebab-case for markdown files

## Configuration Management

### Settings Structure
- **Host Configuration**: `src/Aevatar.Workshop.Host/appsettings.json`
- **Client Configuration**: `src/Aevatar.Workshop.Client/appsettings.json`
- **Test Configuration**: `test/Aevatar.Workshop.Tests/appsettings.json`

### Environment Handling
- **Development**: Local development with detailed logging
- **Production**: Optimized for Docker deployment
- **Testing**: Isolated test environment configuration

## Documentation Standards

### API Documentation
- **Controller Methods**: XML documentation for all public methods
- **GAgent Interfaces**: Complete interface documentation
- **Event Contracts**: Clear event purpose and usage documentation

### User Documentation
- **Demo Guides**: Step-by-step demo usage instructions
- **Implementation Guides**: Best practices and patterns
- **Troubleshooting**: Common issues and solutions

## Build and Deployment

### Project Dependencies
- **Package Management**: Centralized via `Directory.Packages.props`
- **Version Management**: Consistent across all projects
- **Build Order**: Host → GAgent → Client → Tests

### Docker Configuration
- **Multi-stage builds**: Optimized container images
- **Service separation**: Independent client and host containers
- **Health checks**: Container health monitoring
- **Networking**: Service communication configuration

## Development Workflow

### Local Development
1. **Clone Repository**: `git clone` the workshop
2. **Run Quick Start**: Execute `./quickstart.sh`
3. **Access Interface**: Open `http://localhost:5000`
4. **Explore Demos**: Navigate through available demonstrations

### Feature Development
1. **Create GAgent**: Follow implementation patterns in `GAgents/`
2. **Define Events**: Add events in `Events/` directory
3. **Implement Tests**: Add corresponding tests in `test/`
4. **Update Documentation**: Document new features in `docs/`

### Code Quality
- **Consistent Style**: Follow existing code patterns
- **Comprehensive Testing**: Maintain test coverage
- **Documentation**: Keep documentation current
- **Performance**: Consider scalability and performance implications