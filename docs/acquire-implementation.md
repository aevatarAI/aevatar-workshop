# Acquire Board Game Implementation

This project implements the classic board game Acquire using the Aevatar GAgent framework. The implementation supports 2-6 players with real-time gameplay and individual player UI pages.

## Architecture

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

## Game Features

### Core Gameplay
- **9x12 Grid Board**: Classic Acquire board layout
- **Hotel Chains**: 7 different hotel chains (Tower, Luxor, American, Worldwide, Festival, Imperial, Continental)
- **Tile Placement**: Strategic tile placement to form and grow hotel chains
- **Stock Trading**: Buy and sell hotel chain stocks
- **Chain Mergers**: Handle mergers between competing hotel chains
- **Game End Conditions**: Game ends when any hotel chain reaches 41+ tiles

### Multi-Player Support
- **2-6 Players**: Supports the full range of player counts
- **Individual Player GAgents**: Each player has their own GAgent for isolated state management
- **Real-time Updates**: Game state updates propagate to all players automatically
- **Turn Management**: Automated turn rotation and validation
- **Private Player Views**: Each player sees their own private UI with their tiles and stocks

### User Interface
- **Interactive Game Board**: Visual 9x12 grid with tile placement
- **Player Dashboard**: Shows money, stocks, and available tiles
- **Hotel Chain Status**: Real-time display of chain sizes and stock prices
- **Action Controls**: Intuitive buttons for game actions
- **Game Status**: Clear indication of game state and current player

## How to Play

### Starting a Game
1. Click "Create New Game" to start a new game session
2. Enter 2-6 player names (comma-separated)
3. The game will automatically deal initial tiles and start

### Game Flow
1. **Tile Placement**: On your turn, select a tile from your hand and click on the board to place it
2. **Stock Purchase**: After placing a tile, you can buy up to 3 stocks from available hotel chains
3. **Chain Formation**: Placing tiles can create new hotel chains or extend existing ones
4. **Mergers**: When tiles connect multiple chains, the largest chain absorbs the others
5. **Game End**: Game ends when any chain reaches 41+ tiles; winner is determined by total wealth

### Winning Strategy
- Form and grow hotel chains to increase stock values
- Buy stocks early in promising chains
- Benefit from chain mergers (majority/minority bonuses)
- Balance between growing chains and maintaining diversification

## Technical Implementation

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

## Files Structure

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

## Running the Game

1. **Start the Aevatar Host**: Run the Aevatar workshop application
2. **Access the Game UI**: Navigate to `/demos/acquire-game.html` in your browser
3. **Create or Join**: Either create a new game or join an existing one
4. **Play**: Use the interface to place tiles, buy stocks, and manage your hotel empire

## Future Enhancements

- **WebSocket Integration**: Real-time updates without polling
- **Game History**: Detailed game replay and analysis
- **AI Players**: Computer-controlled opponents
- **Tournament Mode**: Multi-game tournaments with scoring
- **Mobile App**: Native mobile applications
- **Persistent Storage**: Long-term game statistics and leaderboards

## Game Rules Reference

### Initial Setup
- Each player starts with $6000 and 6 tiles
- Players take turns placing one tile and buying up to 3 stocks
- Game continues until any hotel chain reaches 41+ tiles

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

### Scoring
- Final score = cash + value of all stocks held
- Stock value = current stock price × number of shares
- Player with highest total wealth wins