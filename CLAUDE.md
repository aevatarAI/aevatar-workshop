# Claude Configuration for Aevatar Workshop

## Project Overview
This is an Aevatar framework workshop project that demonstrates GAgent development patterns, Event Sourcing implementation, and AI agent integration.

## Critical Documentation Directory
**IMPORTANT**: Before implementing or testing any GAgent, you MUST review the documentation in `.claude/docs/` directory:

### Key Documents to Read:
1. **`.claude/docs/gagent-implementation-guide.md`** - Comprehensive GAgent implementation rules and patterns
2. **`.claude/docs/gagent-unit-testing-guide.md`** - Complete testing patterns and best practices

## GAgent Development Rules

### Must-Follow Principles:
- **Event Sourcing**: All state changes MUST use `RaiseEvent` + `ConfirmEvents()` - NEVER modify State directly
- **Class Types**: Events, State, and StateLogEvent must be defined as `class` (not `record`)
- **No Constructor Injection**: Use `ServiceProvider.GetRequiredService<T>()` instead
- **Use IGAgentFactory**: Never use IGrainFactory directly for GAgent instantiation
- **Proper Attributes**: All classes need `[GenerateSerializer]` and properties need `[Id(n)]`

### Implementation Checklist:
- [ ] Read `.claude/docs/gagent-implementation-guide.md` before starting
- [ ] Define interface first with `IStateGAgent<TState>`
- [ ] Use `class` (not `record`) for State and StateLogEvent
- [ ] Add `[GAgent]` attribute to implementation
- [ ] Implement `GetDescriptionAsync()` method
- [ ] Use `RaiseEvent` + `ConfirmEvents()` for state changes
- [ ] Handle transitions in `GAgentTransitionState`
- [ ] Initialize collections in State constructor
- [ ] Use `Logger` property (not injected)

### Testing Checklist:
- [ ] Read `.claude/docs/gagent-unit-testing-guide.md` before testing
- [ ] Use `AevatarWorkshopTestBase<T>` as base class
- [ ] Add `[Collection(ClusterCollection.Name)]` attribute
- [ ] Use unique Guids for each test instance
- [ ] Test both success and failure scenarios
- [ ] Verify state changes through `GetStateAsync()`
- [ ] Use appropriate delays for event processing

## Project Structure
```
src/
├── Aevatar.Workshop.GAgent/     # GAgent implementations
│   ├── Events/                   # Shared events
│   ├── GAgents/                  # GAgent implementations
│   │   ├── Acquire/             # Acquire game GAgents (Event Sourcing example)
│   │   ├── SmartHome/           # Smart home demo GAgents
│   │   └── Demo/                # Various demo GAgents
│   └── Extensions/              # GAgent extensions
├── Aevatar.Workshop.Client/     # Web API client
├── Aevatar.Workshop.Host/        # Orleans host
└── test/                        # Test projects
    ├── Aevatar.Workshop.TestBase/
    ├── Aevatar.Workshop.Tests/
    └── Aevatar.Workshop.GuideGAgents/
```

## Existing GAgent Examples

### Acquire Game GAgents (Event Sourcing)
- **Location**: `src/Aevatar.Workshop.GAgent/GAgents/Acquire/`
- **Files**: `AcquireGameGAgent.cs`, `AcquirePlayerGAgent.cs`
- **Key Features**: Complete Event Sourcing implementation, game state management
- **Reference**: This is a GOOD example of proper Event Sourcing patterns

### Smart Home GAgents
- **Location**: `src/Aevatar.Workshop.GAgent/GAgents/SmartHome/`
- **Features**: Device control, AI coordination, event-driven architecture

### Demo GAgents
- **Location**: `src/Aevatar.Workshop.GAgent/GAgents/Demo/`
- **Features**: Various patterns including coordination, event logging, notifications

## Event Sourcing Compliance

### Critical Rules (from Acquire GAgent fixes):
1. **NEVER** modify State directly: `State.SomeProperty = value` is WRONG
2. **ALWAYS** use Event Sourcing: `RaiseEvent(new MyEvent()); await ConfirmEvents();`
3. **ALL** state changes must go through `GAgentTransitionState` method
4. **Events** must contain all information needed to reconstruct state

### Correct Pattern:
```csharp
// ❌ WRONG - Direct state modification
public async Task DoSomethingAsync()
{
    State.Counter++;
}

// ✅ CORRECT - Event Sourcing
public async Task DoSomethingAsync()
{
    RaiseEvent(new SomethingHappenedEvent { Amount = 1 });
    await ConfirmEvents();
}

protected override void GAgentTransitionState(MyState state, StateLogEventBase<MyStateLogEvent> @event)
{
    switch (@event)
    {
        case SomethingHappenedEvent e:
            state.Counter += e.Amount;
            break;
    }
}
```

## Commands to Run

### Building:
```bash
dotnet build src/Aevatar.Workshop.GAgent/Aevatar.Workshop.GAgent.csproj
dotnet build src/Aevatar.Workshop.Client/Aevatar.Workshop.Client.csproj
```

### Testing:
```bash
dotnet test test/Aevatar.Workshop.Tests/Aevatar.Workshop.Tests.csproj
```

### Running:
```bash
# Quick start
./quickstart.sh

# Docker
./docker-quickstart.sh
```

## Important Notes

1. **Always read the documentation in `.claude/docs/` before implementing**
2. **Follow Event Sourcing patterns strictly** - the Acquire GAgent shows the correct way
3. **Use the existing GAgents as references** for patterns and best practices
4. **Test thoroughly** using the patterns in the testing guide
5. **Never bypass the Event Sourcing system** - it's core to Aevatar architecture

## Development Workflow

1. **Planning**: Use the Spec Workflow commands (`/spec-create`, etc.) for new features
2. **Implementation**: Follow the GAgent Implementation Guide
3. **Testing**: Use the GAgent Unit Testing Guide
4. **Validation**: Ensure Event Sourcing compliance
5. **Documentation**: Update relevant guides with new patterns

## Configuration Files

- `.claude/spec-config.json` - Spec workflow configuration
- `.claude/settings.local.json` - Local Claude settings
- `appsettings.json` - Application configuration