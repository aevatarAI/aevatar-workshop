using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Aevatar.Core.Abstractions.Extensions;
using Aevatar.Workshop.GAgent.Events.Acquire;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aevatar.Workshop.GAgent.GAgents.Acquire;

#region Interface

/// <summary>
/// Acquire player GAgent interface
/// </summary>
public interface IAcquirePlayerGAgent : IStateGAgent<AcquirePlayerGAgentState>
{
    Task<PlayerGameState> GetPlayerGameStateAsync();
    Task<bool> JoinGameAsync(string gameId, string playerName);
    Task<bool> LeaveGameAsync();
    Task<bool> PlaceTileAsync(int row, int column, string tileId);
    Task<bool> BuyStockAsync(string chainName, int quantity);
    Task<bool> HandleMergeStocksAsync(Dictionary<string, int> stockChoices);
    Task<List<string>> GetGameHistoryAsync();
    Task<bool> SetConnectionAsync(string connectionId, bool isConnected);
}

#endregion

#region State and Events

/// <summary>
/// Acquire player GAgent state
/// </summary>
[GenerateSerializer]
public class AcquirePlayerGAgentState : StateBase
{
    [Id(0)] public string PlayerId { get; set; } = string.Empty;
    [Id(1)] public string PlayerName { get; set; } = string.Empty;
    [Id(2)] public string CurrentGameId { get; set; } = string.Empty;
    [Id(3)] public PlayerGameState PlayerGameState { get; set; } = new();
    [Id(4)] public List<string> GameHistory { get; set; } = new();
    [Id(5)] public bool IsConnected { get; set; } = false;
    [Id(6)] public DateTime LastActivityAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Player game state (subset of full game state)
/// </summary>
[GenerateSerializer]
public class PlayerGameState
{
    [Id(0)] public string GameId { get; set; } = string.Empty;
    [Id(1)] public int Money { get; set; } = 6000;
    [Id(2)] public List<string> TileIds { get; set; } = new();
    [Id(3)] public Dictionary<string, int> Stocks { get; set; } = new();
    [Id(4)] public bool IsCurrentTurn { get; set; } = false;
    [Id(5)] public List<string> AvailableActions { get; set; } = new();
    [Id(6)] public Dictionary<string, int> StockPrices { get; set; } = new();
    [Id(7)] public List<string> OtherPlayers { get; set; } = new();
    [Id(8)] public GameStatus GameStatus { get; set; } = GameStatus.WaitingForPlayers;
}

/// <summary>
/// Acquire player state log event (internal to this GAgent)
/// </summary>
[GenerateSerializer]
public class AcquirePlayerGAgentStateLogEvent : StateLogEventBase<AcquirePlayerGAgentStateLogEvent>
{
}

/// <summary>
/// Player joined game state event
/// </summary>
[GenerateSerializer]
public class PlayerJoinedGameStateEvent : AcquirePlayerGAgentStateLogEvent
{
    [Id(0)] public string GameId { get; set; } = string.Empty;
    [Id(1)] public string PlayerName { get; set; } = string.Empty;
    [Id(2)] public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Player left game state event
/// </summary>
[GenerateSerializer]
public class PlayerLeftGameStateEvent : AcquirePlayerGAgentStateLogEvent
{
    [Id(0)] public string GameId { get; set; } = string.Empty;
    [Id(1)] public DateTime LeftAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Player action state event
/// </summary>
[GenerateSerializer]
public class PlayerActionStateEvent : AcquirePlayerGAgentStateLogEvent
{
    [Id(0)] public string ActionType { get; set; } = string.Empty;
    [Id(1)] public Dictionary<string, object> Parameters { get; set; } = new();
    [Id(2)] public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Player connection state event
/// </summary>
[GenerateSerializer]
public class PlayerConnectionStateEvent : AcquirePlayerGAgentStateLogEvent
{
    [Id(0)] public bool IsConnected { get; set; }
    [Id(1)] public string ConnectionId { get; set; } = string.Empty;
    [Id(2)] public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Player turn state changed event
/// </summary>
[GenerateSerializer]
public class PlayerTurnStateChangedEvent : AcquirePlayerGAgentStateLogEvent
{
    [Id(0)] public bool IsCurrentTurn { get; set; }
    [Id(1)] public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Player game state updated event
/// </summary>
[GenerateSerializer]
public class PlayerGameStateChangedEvent : AcquirePlayerGAgentStateLogEvent
{
    [Id(0)] public PlayerGameState GameState { get; set; } = new();
    [Id(1)] public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

#endregion

#region Implementation

/// <summary>
/// Acquire player GAgent implementation
/// </summary>
[GAgent("acquire-player", "acquire")]
public class AcquirePlayerGAgent : GAgentBase<AcquirePlayerGAgentState, AcquirePlayerGAgentStateLogEvent>, IAcquirePlayerGAgent
{
    private IAcquireGameGAgent? _currentGameAgent;
    
    private IGAgentFactory GAgentFactory => ServiceProvider.GetRequiredService<IGAgentFactory>();

    public override Task<string> GetDescriptionAsync()
        => Task.FromResult("Acquire player GAgent that manages individual player state and actions");

    #region Public Methods

    /// <summary>
    /// Get player's current game state
    /// </summary>
    public Task<PlayerGameState> GetPlayerGameStateAsync()
    {
        return Task.FromResult(State.PlayerGameState);
    }

    /// <summary>
    /// Join a game
    /// </summary>
    public async Task<bool> JoinGameAsync(string gameId, string playerName)
    {
        try
        {
            // Get game GAgent
            _currentGameAgent = await GAgentFactory
                .GetGAgentAsync<IAcquireGameGAgent>(Guid.Parse(gameId));

            // Join game through game GAgent
            var success = await _currentGameAgent.JoinGameAsync(gameId, playerName, this.GetGrainId().ToString());
            if (!success)
            {
                Logger.LogWarning("Failed to join game {GameId}", gameId);
                return false;
            }

            // Update state using event sourcing
            RaiseEvent(new PlayerJoinedGameStateEvent
            {
                GameId = gameId,
                PlayerName = playerName,
                JoinedAt = DateTime.UtcNow
            });
            await ConfirmEvents();

            Logger.LogInformation("Player {PlayerName} ({PlayerId}) joined game {GameId}", 
                playerName, State.PlayerId, gameId);
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to join game {GameId} for player {PlayerName}", 
                gameId, playerName);
            return false;
        }
    }

    /// <summary>
    /// Leave current game
    /// </summary>
    public async Task<bool> LeaveGameAsync()
    {
        if (string.IsNullOrEmpty(State.CurrentGameId) || _currentGameAgent == null)
        {
            Logger.LogWarning("Player {PlayerId} is not in a game", State.PlayerId);
            return false;
        }

        try
        {
            var success = await _currentGameAgent.LeaveGameAsync(State.CurrentGameId, State.PlayerId);
            if (!success)
            {
                Logger.LogWarning("Failed to leave game {GameId}", State.CurrentGameId);
                return false;
            }

            // Update state using event sourcing
            RaiseEvent(new PlayerLeftGameStateEvent
            {
                GameId = State.CurrentGameId,
                LeftAt = DateTime.UtcNow
            });
            await ConfirmEvents();

            Logger.LogInformation("Player {PlayerId} left game {GameId}", 
                State.PlayerId, State.CurrentGameId);
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to leave game {GameId} for player {PlayerId}", 
                State.CurrentGameId, State.PlayerId);
            return false;
        }
    }

    /// <summary>
    /// Place a tile
    /// </summary>
    public async Task<bool> PlaceTileAsync(int row, int column, string tileId)
    {
        if (_currentGameAgent == null || string.IsNullOrEmpty(State.CurrentGameId))
        {
            Logger.LogWarning("Player {PlayerId} is not in a game", State.PlayerId);
            return false;
        }

        try
        {
            // Create and publish place tile command
            var command = new PlaceTileCommand
            {
                GameId = State.CurrentGameId,
                PlayerId = State.PlayerId,
                Row = row,
                Column = column,
                TileId = tileId
            };

            await PublishAsync(command);

            // Update state using event sourcing
            RaiseEvent(new PlayerActionStateEvent
            {
                ActionType = "PlaceTile",
                Parameters = new Dictionary<string, object>
                {
                    ["Row"] = row,
                    ["Column"] = column,
                    ["TileId"] = tileId
                },
                Timestamp = DateTime.UtcNow
            });
            await ConfirmEvents();

            Logger.LogInformation("Player {PlayerId} placed tile {TileId} at ({Row}, {Column})", 
                State.PlayerId, tileId, row, column);
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to place tile for player {PlayerId}", State.PlayerId);
            return false;
        }
    }

    /// <summary>
    /// Buy stock
    /// </summary>
    public async Task<bool> BuyStockAsync(string chainName, int quantity)
    {
        if (_currentGameAgent == null || string.IsNullOrEmpty(State.CurrentGameId))
        {
            Logger.LogWarning("Player {PlayerId} is not in a game", State.PlayerId);
            return false;
        }

        try
        {
            // Create and publish buy stock command
            var command = new BuyStockCommand
            {
                GameId = State.CurrentGameId,
                PlayerId = State.PlayerId,
                ChainName = chainName,
                Quantity = quantity
            };

            await PublishAsync(command);

            // Update state using event sourcing
            RaiseEvent(new PlayerActionStateEvent
            {
                ActionType = "BuyStock",
                Parameters = new Dictionary<string, object>
                {
                    ["ChainName"] = chainName,
                    ["Quantity"] = quantity
                },
                Timestamp = DateTime.UtcNow
            });
            await ConfirmEvents();

            Logger.LogInformation("Player {PlayerId} bought {Quantity} shares of {ChainName}", 
                State.PlayerId, quantity, chainName);
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to buy stock for player {PlayerId}", State.PlayerId);
            return false;
        }
    }

    /// <summary>
    /// Handle stock merging
    /// </summary>
    public async Task<bool> HandleMergeStocksAsync(Dictionary<string, int> stockChoices)
    {
        if (_currentGameAgent == null || string.IsNullOrEmpty(State.CurrentGameId))
        {
            Logger.LogWarning("Player {PlayerId} is not in a game", State.PlayerId);
            return false;
        }

        try
        {
            // This would be implemented with a specific merge stocks event
            // For now, we'll log the action

            // Update state using event sourcing
            RaiseEvent(new PlayerActionStateEvent
            {
                ActionType = "MergeStocks",
                Parameters = new Dictionary<string, object>
                {
                    ["StockChoices"] = stockChoices
                },
                Timestamp = DateTime.UtcNow
            });
            await ConfirmEvents();

            Logger.LogInformation("Player {PlayerId} handled stock merge with choices: {Choices}", 
                State.PlayerId, string.Join(", ", stockChoices.Select(kv => $"{kv.Key}:{kv.Value}")));
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to handle stock merge for player {PlayerId}", State.PlayerId);
            return false;
        }
    }

    /// <summary>
    /// Get game history
    /// </summary>
    public Task<List<string>> GetGameHistoryAsync()
    {
        return Task.FromResult(State.GameHistory.ToList());
    }

    /// <summary>
    /// Set connection status
    /// </summary>
    public async Task<bool> SetConnectionAsync(string connectionId, bool isConnected)
    {
        // Update state using event sourcing
        RaiseEvent(new PlayerConnectionStateEvent
        {
            IsConnected = isConnected,
            ConnectionId = connectionId,
            UpdatedAt = DateTime.UtcNow
        });
        await ConfirmEvents();

        Logger.LogInformation("Player {PlayerId} connection status: {Connected}", 
            State.PlayerId, isConnected);
        return true;
    }

    #endregion

    #region Event Handlers

    /// <summary>
    /// Handle game state updates
    /// </summary>
    [EventHandler]
    public async Task HandleBoardStateUpdatedAsync(BoardStateUpdatedEvent @event)
    {
        if (@event.GameId != State.CurrentGameId)
        {
            return;
        }

        // Update player's view of the game
        await UpdatePlayerGameStateAsync();
    }

    /// <summary>
    /// Handle turn started
    /// </summary>
    [EventHandler]
    public async Task HandleTurnStartedAsync(TurnStartedEvent @event)
    {
        if (@event.GameId != State.CurrentGameId)
        {
            return;
        }

        // Update state using event sourcing for turn state change
        RaiseEvent(new PlayerTurnStateChangedEvent
        {
            IsCurrentTurn = @event.CurrentPlayerId == State.PlayerId,
            ChangedAt = DateTime.UtcNow
        });
        await ConfirmEvents();

        if (State.PlayerGameState.IsCurrentTurn)
        {
            Logger.LogInformation("Player {PlayerId}'s turn started in game {GameId}", 
                State.PlayerId, @event.GameId);
        }
    }

    /// <summary>
    /// Handle turn completed
    /// </summary>
    [EventHandler]
    public async Task HandleTurnCompletedAsync(TurnCompletedEvent @event)
    {
        if (@event.GameId != State.CurrentGameId)
        {
            return;
        }

        // Update state using event sourcing for turn state change
        RaiseEvent(new PlayerTurnStateChangedEvent
        {
            IsCurrentTurn = false,
            ChangedAt = DateTime.UtcNow
        });
        await ConfirmEvents();

        Logger.LogDebug("Player {PlayerId}'s turn completed in game {GameId}", 
            State.PlayerId, @event.GameId);
    }

    /// <summary>
    /// Handle stock price updates
    /// </summary>
    [EventHandler]
    public async Task HandleStockPriceUpdatedAsync(StockPriceUpdatedEvent @event)
    {
        if (@event.GameId != State.CurrentGameId)
        {
            return;
        }

        // Update state using event sourcing for stock prices
        var updatedGameState = State.PlayerGameState;
        updatedGameState.StockPrices = new Dictionary<string, int>(@event.StockPrices);
        
        RaiseEvent(new PlayerGameStateChangedEvent
        {
            GameState = updatedGameState,
            UpdatedAt = DateTime.UtcNow
        });
        await ConfirmEvents();

        Logger.LogDebug("Stock prices updated for player {PlayerId} in game {GameId}", 
            State.PlayerId, @event.GameId);
    }

    /// <summary>
    /// Handle game ended
    /// </summary>
    [EventHandler]
    public async Task HandleGameEndedAsync(GameEndedEvent @event)
    {
        if (@event.GameId != State.CurrentGameId)
        {
            return;
        }

        // Update state using event sourcing for game end
        var updatedGameState = State.PlayerGameState;
        updatedGameState.GameStatus = GameStatus.Ended;
        
        RaiseEvent(new PlayerGameStateChangedEvent
        {
            GameState = updatedGameState,
            UpdatedAt = DateTime.UtcNow
        });
        await ConfirmEvents();

        // Update state using event sourcing for leaving game
        RaiseEvent(new PlayerLeftGameStateEvent
        {
            GameId = State.CurrentGameId,
            LeftAt = DateTime.UtcNow
        });
        await ConfirmEvents();

        Logger.LogInformation("Game {GameId} ended for player {PlayerId}. Winner: {Winner}", 
            @event.GameId, State.PlayerId, @event.Winner);
    }

    /// <summary>
    /// Handle player must merge stocks
    /// </summary>
    [EventHandler]
    public async Task HandlePlayerMustMergeStocksAsync(PlayerMustMergeStocksEvent @event)
    {
        if (@event.GameId != State.CurrentGameId || @event.PlayerId != State.PlayerId)
        {
            return;
        }

        // Add merge action to available actions
        var updatedGameState = State.PlayerGameState;
        updatedGameState.AvailableActions.Add("MergeStocks");
        
        RaiseEvent(new PlayerGameStateChangedEvent
        {
            GameState = updatedGameState,
            UpdatedAt = DateTime.UtcNow
        });
        await ConfirmEvents();

        Logger.LogInformation("Player {PlayerId} must merge stocks in game {GameId}", 
            State.PlayerId, @event.GameId);
    }

    #endregion

    #region Private Helper Methods

    private async Task UpdatePlayerGameStateAsync()
    {
        if (_currentGameAgent == null || string.IsNullOrEmpty(State.CurrentGameId))
        {
            return;
        }

        try
        {
            // Get updated game state from game GAgent
            var gameState = await _currentGameAgent.GetGameStateAsync(State.CurrentGameId);
            var playerState = await _currentGameAgent.GetPlayerStateAsync(State.CurrentGameId, State.PlayerId);
            var availableActions = await _currentGameAgent.GetAvailableActionsAsync(State.CurrentGameId, State.PlayerId);

            // Create updated player game state
            var updatedGameState = new PlayerGameState
            {
                GameId = State.CurrentGameId,
                Money = playerState.Money,
                TileIds = playerState.TileIds,
                Stocks = playerState.Stocks,
                IsCurrentTurn = playerState.IsCurrentTurn,
                GameStatus = gameState.Status,
                AvailableActions = new List<string>(),
                StockPrices = gameState.Board.HotelChains
                    .Where(c => c.Value.Tiles.Count > 0)
                    .ToDictionary(c => c.Key, c => c.Value.StockPrice),
                OtherPlayers = gameState.Players
                    .Where(p => p.PlayerId != State.PlayerId)
                    .Select(p => p.PlayerName)
                    .ToList()
            };

            // Update available actions
            if (availableActions.CanPlaceTile)
            {
                updatedGameState.AvailableActions.Add("PlaceTile");
            }
            if (availableActions.CanBuyStocks.Count > 0)
            {
                updatedGameState.AvailableActions.Add("BuyStock");
            }
            if (availableActions.MustMergeStocks)
            {
                updatedGameState.AvailableActions.Add("MergeStocks");
            }

            // Update state using event sourcing
            RaiseEvent(new PlayerGameStateChangedEvent
            {
                GameState = updatedGameState,
                UpdatedAt = DateTime.UtcNow
            });
            await ConfirmEvents();

            Logger.LogDebug("Player {PlayerId} game state updated", State.PlayerId);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to update player {PlayerId} game state", State.PlayerId);
        }
    }

    #endregion

    #region State Transitions

    protected override void GAgentTransitionState(AcquirePlayerGAgentState state, StateLogEventBase<AcquirePlayerGAgentStateLogEvent> @event)
    {
        switch (@event)
        {
            case PlayerJoinedGameStateEvent e:
                state.PlayerId = this.GetGrainId().ToString();
                state.PlayerName = e.PlayerName;
                state.CurrentGameId = e.GameId;
                state.PlayerGameState = new PlayerGameState
                {
                    GameId = e.GameId,
                    Money = 6000,
                    TileIds = new List<string>(),
                    Stocks = new Dictionary<string, int>(),
                    IsCurrentTurn = false,
                    AvailableActions = new List<string>(),
                    StockPrices = new Dictionary<string, int>(),
                    OtherPlayers = new List<string>(),
                    GameStatus = GameStatus.WaitingForPlayers
                };
                state.LastActivityAt = e.JoinedAt;
                break;
                
            case PlayerLeftGameStateEvent e:
                state.GameHistory.Add(state.CurrentGameId);
                state.CurrentGameId = string.Empty;
                state.PlayerGameState = new PlayerGameState();
                state.LastActivityAt = e.LeftAt;
                _currentGameAgent = null;
                break;
                
            case PlayerActionStateEvent e:
                state.LastActivityAt = e.Timestamp;
                break;
                
            case PlayerConnectionStateEvent e:
                state.IsConnected = e.IsConnected;
                state.LastActivityAt = e.UpdatedAt;
                break;
                
            case PlayerTurnStateChangedEvent e:
                state.PlayerGameState.IsCurrentTurn = e.IsCurrentTurn;
                state.LastActivityAt = e.ChangedAt;
                break;
                
            case PlayerGameStateChangedEvent e:
                state.PlayerGameState = e.GameState;
                state.LastActivityAt = e.UpdatedAt;
                break;
        }
    }

    #endregion
}

#endregion