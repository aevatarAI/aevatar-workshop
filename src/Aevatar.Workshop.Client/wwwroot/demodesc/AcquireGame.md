# Acquire Board Game Demo

## Overview

This demo demonstrates how to implement the classic Acquire board game using the Aevatar GAgent framework. This is a strategic board game for 2-6 players where players build their hotel empire by placing tiles, buying and trading hotel stocks.

## Game Features

### 🎮 Complete Game Implementation
- **9x12 Grid Board**: Classic Acquire game layout
- **7 Hotel Chains**: Tower, Luxor, American, Worldwide, Festival, Imperial, Continental
- **Complete Game Rules**: Including tile placement, stock trading, chain mergers, and more
- **Automatic Game End**: Game ends when any hotel chain reaches 41+ tiles

### 👥 Multi-Player Support
- **2-6 Players**: Supports the full range of player counts
- **Individual Player GAgents**: Each player has their own GAgent instance for state management
- **Real-time Updates**: Game state changes automatically propagate to all players
- **Turn Management**: Automated turn rotation and validation
- **Private Player Views**: Each player sees their own private UI with their tiles and stocks

### 🖥️ Modern Interface
- **Interactive Game Board**: Visual 9x12 grid with tile placement
- **Player Dashboard**: Shows money, stocks, and available tiles
- **Hotel Chain Status**: Real-time display of chain sizes and stock prices
- **Action Controls**: Intuitive buttons for game actions
- **Responsive Design**: Supports multiple screen sizes

## Technical Architecture

### GAgent Components

1. **AcquireGameGAgent** (`/src/Aevatar.Workshop.GAgent/GAgents/Acquire/AcquireGameGAgent.cs`)
   - Manages overall game state and rules
   - Handles game initialization, turn management, and game logic
   - Coordinates between players and manages hotel chain operations

2. **AcquirePlayerGAgent** (`/src/Aevatar.Workshop.GAgent/GAgents/Acquire/AcquirePlayerGAgent.cs`)
   - Manages individual player state and actions
   - Handles player-specific operations like tile placement and stock purchases
   - Provides isolated player experience with private game state view

3. **Game Events** (`/src/Aevatar.Workshop.GAgent/Events/AcquireGameEvents.cs`)
   - Defines all game events and data structures
   - Includes events for game management, tile placement, stock operations, and state updates

### API Layer

1. **AcquireController** (`/src/Aevatar.Workshop.Client/Controllers/AcquireController.cs`)
   - REST API endpoints for game operations
   - Handles game creation, player management, and action execution
   - Provides JSON responses for frontend integration

### Frontend

1. **Acquire Game UI** (`/src/Aevatar.Workshop.Client/wwwroot/demos/acquire-game.html`)
   - Complete web-based game interface
   - Real-time game board visualization
   - Player state management and interaction controls
   - Responsive design for multiple screen sizes

## Game Rules

### Game Objective
Accumulate wealth by strategically placing tiles, buying and trading hotel stocks. The player with the most total wealth (cash + stock value) at the end of the game wins.

### Game Flow
1. **Initial Setup**: Each player starts with $6000 and 6 tiles
2. **Turn Play**: Players take turns performing the following actions:
   - Place 1 tile
   - Buy up to 3 stocks
3. **Chain Formation**: Placing tiles can create new hotel chains or extend existing ones
4. **Chain Mergers**: When tiles connect multiple chains, the largest chain absorbs the others
5. **Game End**: Game ends when any chain reaches 41+ tiles

### Hotel Chain Rules
- Chains form when 2+ adjacent tiles are placed
- New chains can only be formed if there are available chain names
- Chains become "safe" at 11+ tiles and cannot be merged
- Chains become "terminal" at 41+ tiles, ending the game

### Stock Rules
- Each chain has 25 shares available for purchase
- Players can buy up to 3 shares per turn
- Stock prices increase as chains grow
- During mergers, majority and minority shareholders receive bonuses

## How to Play

1. **Start the Aevatar Host**: Run the Aevatar workshop application
2. **Access the Game UI**: Navigate to `/demos/acquire-game.html` in your browser
3. **Create or Join**: Either create a new game or join an existing one
4. **Play**: Use the interface to place tiles, buy stocks, and build your hotel empire

## Game Strategy

### Basic Strategy
- **Early Investment**: Buy stocks early in promising chains
- **Diversification**: Spread investments across multiple chains
- **Position Control**: Strategically place tiles to influence chain development

### Advanced Strategy
- **Merger Timing**: Anticipate and benefit from chain mergers
- **Majority Control**: Try to gain majority ownership in specific chains
- **Risk Balance**: Balance high-risk high-reward with stable investments

## API Endpoints

### Game Management
- `POST /api/acquire/create` - Create a new game
- `GET /api/acquire/game/{gameId}/state` - Get game state
- `POST /api/acquire/game/{gameId}/join` - Join a game
- `POST /api/acquire/player/{playerId}/leave` - Leave a game

### Player Actions
- `POST /api/acquire/player/{playerId}/place-tile` - Place a tile
- `POST /api/acquire/player/{playerId}/buy-stock` - Buy stock
- `POST /api/acquire/player/{playerId}/merge-stocks` - Handle stock merging

### Player State
- `GET /api/acquire/player/{playerId}/state` - Get player state
- `GET /api/acquire/player/{playerId}/history` - Get player game history
- `POST /api/acquire/player/{playerId}/connection` - Set connection status

## Extension Features

### Implemented Features
- ✅ Complete game rules implementation
- ✅ Multi-player support
- ✅ Real-time state synchronization
- ✅ Responsive web interface
- ✅ Individual player GAgents
- ✅ Event-driven architecture

### Potential Enhancements
- 🔄 WebSocket integration (real-time updates without polling)
- 🤖 AI players (computer opponents)
- 🏆 Tournament mode (multi-game tournaments with scoring)
- 📱 Mobile apps (native mobile applications)
- 💾 Persistent storage (long-term game statistics and leaderboards)
- 🎮 Game replay (detailed game replay and analysis)

## Technical Details

### State Management
- **Game State**: Centralized game state managed by AcquireGameGAgent
- **Player State**: Individual player state managed by AcquirePlayerGAgent
- **Event Sourcing**: All game actions are stored as events for auditability
- **Real-time Updates**: Events propagate state changes to all relevant components

### Communication
- **Event-Driven Architecture**: GAgents communicate through events
- **REST API**: Frontend communicates through HTTP endpoints
- **State Synchronization**: Automatic state updates across all player views

### Scalability
- **Individual Player GAgents**: Each player gets their own GAgent instance
- **Isolated State**: Player state is managed separately from game state
- **Concurrent Actions**: Multiple players can interact with their own GAgents simultaneously

## Troubleshooting

### Common Issues
1. **Game cannot be created**: Ensure Aevatar host is running
2. **Player cannot join**: Check if game ID is correct
3. **Actions not responding**: Verify if it's the player's turn
4. **State out of sync**: Refresh page or wait for automatic updates

### Debugging Tips
- Use browser developer tools to check network requests
- Check browser console for error messages
- Check Aevatar host logs for GAgent status

## File Structure

```
src/
├── Aevatar.Workshop.GAgent/
│   ├── Events/
│   │   └── AcquireGameEvents.cs          # Game events and data models
│   └── GAgents/
│       └── Acquire/
│           ├── AcquireGameGAgent.cs       # Main game logic GAgent
│           └── AcquirePlayerGAgent.cs     # Individual player GAgent
└── Aevatar.Workshop.Client/
    ├── Controllers/
    │   └── AcquireController.cs           # REST API endpoints
    └── wwwroot/
        └── demos/
            └── acquire-game.html           # Frontend game interface
```

## Contributing

This demo showcases the power of the Aevatar GAgent framework in building complex multi-player games. It demonstrates the following key concepts:

- **Distributed State Management**: Using multiple GAgents to manage different types of state
- **Event-Driven Communication**: Loose coupling between components through events
- **Real-time User Experience**: Synchronized gaming experience for multiple users
- **Scalable Architecture**: Easy to add new features and more players

## License

This demo project is for educational and demonstration purposes only.