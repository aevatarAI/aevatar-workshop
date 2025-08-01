# Technology Stack

## Core Framework
- **Language**: C# 12.0 with .NET 9.0
- **Runtime**: .NET 9.0 with modern language features
- **Pattern**: Event-driven microservices architecture

## Distributed Computing
- **Orleans**: Microsoft Orleans 9.0.1 for distributed actor model
  - Virtual actor pattern for automatic scaling
  - Event sourcing for state management
  - Grain-based agent lifecycle management
  - Streaming for real-time communication

## AI Integration
- **Semantic Kernel**: 1.57.0-alpha for AI agent capabilities
  - Large Language Model integration
  - Tool calling and function execution
  - Prompt engineering and templating
  - Memory and context management

## Agent Framework
- **Aevatar Core**: 1.5.2-tool.2 for multi-agent infrastructure
  - GAgent lifecycle management
  - Event-driven communication
  - State persistence and recovery
  - Agent discovery and registration

## Model Context Protocol (MCP)
- **Aevatar MCP**: 1.5.5-tool.1 for external tool integration
  - Dynamic tool discovery
  - Real-time tool adaptation
  - External service integration
  - Protocol abstraction layer

## AI Agent Capabilities
- **Aevatar AI Agents**: Comprehensive AI agent framework
  - Natural language processing
  - Multi-agent coordination
  - Tool calling and execution
  - State management with AI integration

## Persistence Layer
- **In-Memory Storage**: Workshop-specific simplification
  - In-memory event storage (replaces MongoDB)
  - In-memory state management (replaces Elasticsearch)
  - In-memory messaging (replaces Kafka)
  - Simplified configuration for demonstration

## Web Framework
- **ASP.NET Core**: Modern web API and UI framework
  - RESTful API endpoints
  - SignalR for real-time updates
  - Static file serving for web interface
  - Dependency injection and middleware

## Containerization
- **Docker**: Container-based deployment
  - Multi-container architecture
  - Service isolation and scaling
  - Development and production configurations
  - Health checks and monitoring

## Logging and Monitoring
- **Serilog**: Structured logging framework
  - Console and file logging
  - Structured event data
  - Log rotation and retention
  - Integration with application insights

## Configuration Management
- **JSON Configuration**: Hierarchical configuration system
  - Environment-specific settings
  - Secure configuration management
  - Runtime configuration updates
  - Feature flags and toggles

## Testing Framework
- **xUnit**: Unit testing framework
  - Test discovery and execution
  - Assertion and mocking capabilities
  - Parallel test execution
  - Code coverage integration

## Development Tools
- **Package Management**: Centralized package versioning
  - Directory.Build.props for consistent configuration
  - Centralized package management
  - Version pinning and updates
  - Dependency management

## Build System
- **MSBuild**: .NET build system
  - Project references and dependencies
  - Conditional compilation
  - Build configuration management
  - Publish and deployment profiles

## Development Environment
- **IDE Support**: Multiple IDE compatibility
  - **Visual Studio**: Full project support with debugging
  - **JetBrains Rider**: Cross-platform C# development with debugging
  - **VS Code**: Lightweight editing with C# extension
  - Project templates and scaffolding
  - Debugging and profiling tools
  - IntelliSense and code completion
  - Git integration and version control

## Key Technical Constraints
- **No external dependencies**: Workshop runs entirely in-memory
- **Simplified configuration**: Minimal setup required
- **Fast startup**: Optimized for quick demonstration
- **Educational focus**: Clarity over production optimization
- **Self-contained**: No external service dependencies