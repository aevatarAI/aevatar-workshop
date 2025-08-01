using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Aevatar.Workshop.GAgent.Events.Acquire;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;

namespace Aevatar.Workshop.GAgent.GAgents.Acquire;

#region Interface

/// <summary>
/// Acquire game GAgent interface
/// </summary>
public interface IAcquireGameGAgent : IStateGAgent<AcquireGameGAgentState>
{
    Task<GameState> CreateGameAsync(string gameId, string creatorName);
    Task<GameState> GetGameStateAsync(string gameId);
    Task<PlayerState> GetPlayerStateAsync(string gameId, string playerId);
    Task<AvailableActions> GetAvailableActionsAsync(string gameId, string playerId);
    Task<bool> JoinGameAsync(string gameId, string playerName, string playerId);
    Task<bool> LeaveGameAsync(string gameId, string playerId);
}

#endregion

#region State and Events

/// <summary>
/// Acquire game state
/// </summary>
[GenerateSerializer]
public class AcquireGameGAgentState : StateBase
{
    [Id(0)] public Dictionary<string, GameState> Games { get; set; } = new();
    [Id(1)] public Dictionary<string, List<string>> GamePlayers { get; set; } = new();
    [Id(2)] public Dictionary<string, string> PlayerGameMapping { get; set; } = new();
    [Id(3)] public int TotalGamesCreated { get; set; }
}

/// <summary>
/// Acquire game state log event (internal to this GAgent)
/// </summary>
[GenerateSerializer]
public class AcquireGameGAgentStateLogEvent : StateLogEventBase<AcquireGameGAgentStateLogEvent>
{
}

/// <summary>
/// Game created state event
/// </summary>
[GenerateSerializer]
public class GameCreatedStateEvent : AcquireGameGAgentStateLogEvent
{
    [Id(0)] public string GameId { get; set; } = string.Empty;
    [Id(1)] public string CreatorName { get; set; } = string.Empty;
    [Id(2)] public string CreatorPlayerId { get; set; } = string.Empty;
    [Id(3)] public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Player joined state event
/// </summary>
[GenerateSerializer]
public class PlayerJoinedStateEvent : AcquireGameGAgentStateLogEvent
{
    [Id(0)] public string GameId { get; set; } = string.Empty;
    [Id(1)] public string PlayerId { get; set; } = string.Empty;
    [Id(2)] public string PlayerName { get; set; } = string.Empty;
    [Id(3)] public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Player left state event
/// </summary>
[GenerateSerializer]
public class PlayerLeftStateEvent : AcquireGameGAgentStateLogEvent
{
    [Id(0)] public string GameId { get; set; } = string.Empty;
    [Id(1)] public string PlayerId { get; set; } = string.Empty;
    [Id(2)] public DateTime LeftAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Game started state event
/// </summary>
[GenerateSerializer]
public class GameStartedStateEvent : AcquireGameGAgentStateLogEvent
{
    [Id(0)] public string GameId { get; set; } = string.Empty;
    [Id(1)] public DateTime StartedAt { get; set; } = DateTime.UtcNow;
}

#endregion

#region Implementation

/// <summary>
/// Acquire game GAgent implementation
/// </summary>
[GAgent("acquire-game", "acquire")]
public class AcquireGameGAgent : GAgentBase<AcquireGameGAgentState, AcquireGameGAgentStateLogEvent>, IAcquireGameGAgent
{
    private static readonly List<string> HotelChainNames = new()
    {
        "Tower", "Luxor", "American", "Worldwide", "Festival", "Imperial", "Continental"
    };

    private IGAgentFactory GAgentFactory => ServiceProvider.GetRequiredService<IGAgentFactory>();

    public override Task<string> GetDescriptionAsync()
        => Task.FromResult("Acquire board game implementation for 2-6 players using GAgent architecture");

    #region Game Management

    /// <summary>
    /// Create a new game
    /// </summary>
    public async Task<GameState> CreateGameAsync(string gameId, string creatorName)
    {
        if (string.IsNullOrWhiteSpace(creatorName))
        {
            throw new ArgumentException("Creator name is required");
        }

        if (State.Games.ContainsKey(gameId))
        {
            throw new ArgumentException($"Game {gameId} already exists");
        }

        // Generate creator player ID
        var creatorPlayerId = Guid.NewGuid().ToString("N");
        
        // Update state using event sourcing
        RaiseEvent(new GameCreatedStateEvent
        {
            GameId = gameId,
            CreatorName = creatorName,
            CreatorPlayerId = creatorPlayerId,
            CreatedAt = DateTime.UtcNow
        });
        await ConfirmEvents();

        Logger.LogInformation("Created Acquire game {GameId} with creator {CreatorName}", gameId, creatorName);
        
        // Return the created game state
        return State.Games[gameId];
    }

    /// <summary>
    /// Get game state
    /// </summary>
    public Task<GameState> GetGameStateAsync(string gameId)
    {
        if (!State.Games.TryGetValue(gameId, out var game))
        {
            throw new ArgumentException($"Game {gameId} not found");
        }

        return Task.FromResult(game);
    }

    /// <summary>
    /// Get player state
    /// </summary>
    public Task<PlayerState> GetPlayerStateAsync(string gameId, string playerId)
    {
        if (!State.Games.TryGetValue(gameId, out var game))
        {
            throw new ArgumentException($"Game {gameId} not found");
        }

        var player = game.Players.FirstOrDefault(p => p.PlayerId == playerId);
        if (player == null)
        {
            throw new ArgumentException($"Player {playerId} not found in game {gameId}");
        }

        var playerState = new PlayerState
        {
            PlayerId = player.PlayerId,
            PlayerName = player.PlayerName,
            Money = player.Money,
            TileIds = player.TileIds.ToList(),
            Stocks = new Dictionary<string, int>(player.Stocks),
            IsActive = player.IsActive,
            IsCurrentTurn = game.CurrentPlayerId == playerId
        };

        return Task.FromResult(playerState);
    }

    /// <summary>
    /// Get available actions for player
    /// </summary>
    public async Task<AvailableActions> GetAvailableActionsAsync(string gameId, string playerId)
    {
        if (!State.Games.TryGetValue(gameId, out var game))
        {
            throw new ArgumentException($"Game {gameId} not found");
        }

        if (game.Status != GameStatus.InProgress)
        {
            return new AvailableActions();
        }

        var player = game.Players.FirstOrDefault(p => p.PlayerId == playerId);
        if (player == null || !player.IsActive || game.CurrentPlayerId != playerId)
        {
            return new AvailableActions();
        }

        var actions = new AvailableActions
        {
            CanPlaceTile = player.TileIds.Count > 0,
            CanBuyStocks = game.Board.HotelChains.Keys.ToList(),
            MustMergeStocks = false
        };

        // Check if player has tiles to place
        if (player.TileIds.Count == 0)
        {
            actions.CanPlaceTile = false;
        }

        // Check which stocks can be bought (only existing chains)
        if (game.Board.HotelChains.Count == 0)
        {
            actions.CanBuyStocks.Clear();
        }
        else
        {
            // Filter out chains that have no available shares
            actions.CanBuyStocks = game.Board.HotelChains
                .Where(c => c.Value.AvailableShares > 0)
                .Select(c => c.Key)
                .ToList();
        }

        return actions;
    }

    /// <summary>
    /// Join a game
    /// </summary>
    public async Task<bool> JoinGameAsync(string gameId, string playerName, string playerId)
    {
        if (!State.Games.TryGetValue(gameId, out var game))
        {
            return false;
        }

        if (game.Status != GameStatus.WaitingForPlayers)
        {
            return false;
        }

        if (game.Players.Count >= 6)
        {
            return false;
        }

        // Check if player already in game
        if (game.Players.Any(p => p.PlayerId == playerId))
        {
            return false;
        }

        // Update state using event sourcing
        RaiseEvent(new PlayerJoinedStateEvent
        {
            GameId = gameId,
            PlayerId = playerId,
            PlayerName = playerName,
            JoinedAt = DateTime.UtcNow
        });
        await ConfirmEvents();

        // Auto-start game when we have 2+ players
        if (State.Games[gameId].Players.Count >= 2 && State.Games[gameId].Status == GameStatus.WaitingForPlayers)
        {
            await StartGameAsync(gameId);
        }

        Logger.LogInformation("Player {PlayerName} ({PlayerId}) joined game {GameId}", playerName, playerId, gameId);
        return true;
    }

    /// <summary>
    /// Leave a game
    /// </summary>
    public async Task<bool> LeaveGameAsync(string gameId, string playerId)
    {
        if (!State.Games.TryGetValue(gameId, out var game))
        {
            return false;
        }

        var player = game.Players.FirstOrDefault(p => p.PlayerId == playerId);
        if (player == null)
        {
            return false;
        }

        // Update state using event sourcing
        RaiseEvent(new PlayerLeftStateEvent
        {
            GameId = gameId,
            PlayerId = playerId,
            LeftAt = DateTime.UtcNow
        });
        await ConfirmEvents();

        Logger.LogInformation("Player {PlayerName} ({PlayerId}) left game {GameId}", player.PlayerName, playerId, gameId);
        return true;
    }

    #endregion

    #region Event Handlers

    /// <summary>
    /// Handle game initialization
    /// </summary>
    [EventHandler]
    public async Task HandleInitializeGameAsync(InitializeGameEvent @event)
    {
        Logger.LogInformation("Initializing game {GameId} with players: {Players}", 
            @event.GameId, string.Join(", ", @event.PlayerNames));

        try
        {
            // For initialization events, use the first player name as the creator
            var creatorName = @event.PlayerNames.FirstOrDefault() ?? "Player1";
            await CreateGameAsync(@event.GameId, creatorName);
            
            // Publish game started event
            await PublishAsync(new GameStartedEvent
            {
                GameId = @event.GameId,
                Players = State.Games[@event.GameId].Players,
                StartedAt = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to initialize game {GameId}", @event.GameId);
        }
    }

    /// <summary>
    /// Handle player join game
    /// </summary>
    [EventHandler]
    public async Task HandlePlayerJoinGameAsync(PlayerJoinGameEvent @event)
    {
        Logger.LogInformation("Player {PlayerName} ({PlayerId}) joining game {GameId}", 
            @event.PlayerName, @event.PlayerId, @event.GameId);

        try
        {
            await JoinGameAsync(@event.GameId, @event.PlayerName, @event.PlayerId);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to join player {PlayerId} to game {GameId}", @event.PlayerId, @event.GameId);
        }
    }

    /// <summary>
    /// Handle tile placement
    /// </summary>
    [EventHandler]
    public async Task HandlePlaceTileAsync(PlaceTileCommand command)
    {
        Logger.LogInformation("Player {PlayerId} placing tile {TileId} at position ({Row}, {Column}) in game {GameId}", 
            command.PlayerId, command.TileId, command.Row, command.Column, command.GameId);

        try
        {
            if (!State.Games.TryGetValue(command.GameId, out var game))
            {
                Logger.LogWarning("Game {GameId} not found", command.GameId);
                return;
            }

            if (game.Status != GameStatus.InProgress)
            {
                Logger.LogWarning("Game {GameId} is not in progress", command.GameId);
                return;
            }

            if (game.CurrentPlayerId != command.PlayerId)
            {
                Logger.LogWarning("Player {PlayerId} is not the current player", command.PlayerId);
                return;
            }

            // Validate tile placement
            if (!IsValidTilePlacement(game, command.Row, command.Column))
            {
                Logger.LogWarning("Invalid tile placement at ({Row}, {Column})", command.Row, command.Column);
                return;
            }

            // Place the tile
            await PlaceTileAsync(game, command);

            // Publish tile placed event
            await PublishAsync(new TilePlacedEvent
            {
                GameId = command.GameId,
                PlayerId = command.PlayerId,
                Row = command.Row,
                Column = command.Column,
                TileId = command.TileId,
                PlacedAt = DateTime.UtcNow
            });

            // Update board state
            await PublishAsync(new BoardStateUpdatedEvent
            {
                GameId = command.GameId,
                BoardState = game.Board,
                UpdatedAt = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to place tile for player {PlayerId} in game {GameId}", 
                command.PlayerId, command.GameId);
        }
    }

    /// <summary>
    /// Handle stock purchase
    /// </summary>
    [EventHandler]
    public async Task HandleBuyStockAsync(BuyStockCommand command)
    {
        Logger.LogInformation("Player {PlayerId} buying {Quantity} shares of {ChainName} in game {GameId}", 
            command.PlayerId, command.Quantity, command.ChainName, command.GameId);

        try
        {
            if (!State.Games.TryGetValue(command.GameId, out var game))
            {
                Logger.LogWarning("Game {GameId} not found", command.GameId);
                return;
            }

            if (game.Status != GameStatus.InProgress)
            {
                Logger.LogWarning("Game {GameId} is not in progress", command.GameId);
                return;
            }

            if (game.CurrentPlayerId != command.PlayerId)
            {
                Logger.LogWarning("Player {PlayerId} is not the current player", command.PlayerId);
                return;
            }

            // Validate stock purchase
            if (!IsValidStockPurchase(game, command.PlayerId, command.ChainName, command.Quantity))
            {
                Logger.LogWarning("Invalid stock purchase: {Quantity} shares of {ChainName}", command.Quantity, command.ChainName);
                return;
            }

            // Purchase stock
            await PurchaseStockAsync(game, command);

            // Publish stock purchased event
            await PublishAsync(new StockPurchasedEvent
            {
                GameId = command.GameId,
                PlayerId = command.PlayerId,
                ChainName = command.ChainName,
                Quantity = command.Quantity,
                PricePerShare = game.Board.HotelChains[command.ChainName].StockPrice,
                TotalCost = game.Board.HotelChains[command.ChainName].StockPrice * command.Quantity,
                PurchasedAt = DateTime.UtcNow
            });

            // Update stock prices
            await UpdateStockPricesAsync(game);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to buy stock for player {PlayerId} in game {GameId}", 
                command.PlayerId, command.GameId);
        }
    }

    #endregion

    #region Private Helper Methods

    private async Task StartGameAsync(string gameId)
    {
        if (!State.Games.TryGetValue(gameId, out var game))
        {
            return;
        }

        // Initialize board
        await InitializeBoardAsync(game);

        // Deal initial tiles
        await DealInitialTilesAsync(game);

        // Set first player
        game.CurrentPlayerId = game.Players.First().PlayerId;
        game.CurrentTurn = 1;
        game.Status = GameStatus.InProgress;
        game.StartedAt = DateTime.UtcNow;

        // Update state using event sourcing
        RaiseEvent(new GameStartedStateEvent
        {
            GameId = gameId,
            StartedAt = DateTime.UtcNow
        });
        await ConfirmEvents();

        // Publish turn started event
        await PublishAsync(new TurnStartedEvent
        {
            GameId = gameId,
            CurrentPlayerId = game.CurrentPlayerId,
            TurnNumber = game.CurrentTurn,
            StartedAt = DateTime.UtcNow
        });

        Logger.LogInformation("Game {GameId} started with {PlayerCount} players", gameId, game.Players.Count);
    }

    private async Task InitializeBoardAsync(GameState game)
    {
        game.Board = new BoardState
        {
            Grid = new TilePosition[9, 12],
            HotelChains = new Dictionary<string, HotelChainInfo>(),
            AvailableChains = new List<string>(HotelChainNames),
            TileBag = GenerateTileBag()
        };

        // Initialize all hotel chains
        foreach (var chainName in HotelChainNames)
        {
            game.Board.HotelChains[chainName] = new HotelChainInfo
            {
                Name = chainName,
                Tiles = new List<TilePosition>(),
                IsSafe = false,
                IsTerminal = false,
                StockPrice = 0, // Will be set when chain is formed
                AvailableShares = 25
            };
        }

        Logger.LogDebug("Initialized board for game {GameId}", game.GameId);
    }

    private List<string> GenerateTileBag()
    {
        var tileBag = new List<string>();
        for (int row = 0; row < 9; row++)
        {
            for (int col = 0; col < 12; col++)
            {
                tileBag.Add($"{(char)('A' + row)}{col + 1}");
            }
        }

        // Shuffle the tile bag
        var random = new Random();
        for (int i = tileBag.Count - 1; i > 0; i--)
        {
            var j = random.Next(i + 1);
            (tileBag[i], tileBag[j]) = (tileBag[j], tileBag[i]);
        }

        return tileBag;
    }

    private async Task DealInitialTilesAsync(GameState game)
    {
        foreach (var player in game.Players)
        {
            for (int i = 0; i < 6; i++)
            {
                if (game.Board.TileBag.Count > 0)
                {
                    var tile = game.Board.TileBag[0];
                    game.Board.TileBag.RemoveAt(0);
                    player.TileIds.Add(tile);
                }
            }
        }

        Logger.LogDebug("Dealt initial tiles for game {GameId}", game.GameId);
    }

    private bool IsValidTilePlacement(GameState game, int row, int column)
    {
        // Check bounds
        if (row < 0 || row >= 9 || column < 0 || column >= 12)
        {
            return false;
        }

        // Check if tile is already placed
        if (game.Board.Grid[row, column] != null)
        {
            return false;
        }

        // Check if position is adjacent to existing tiles (for first move, allow any position)
        if (game.Board.HotelChains.Values.All(c => c.Tiles.Count == 0))
        {
            return true;
        }

        // For subsequent moves, must be adjacent to existing tiles
        return HasAdjacentTile(game, row, column);
    }

    private bool HasAdjacentTile(GameState game, int row, int column)
    {
        var directions = new[] { (-1, 0), (1, 0), (0, -1), (0, 1) };

        foreach (var (dr, dc) in directions)
        {
            var newRow = row + dr;
            var newCol = column + dc;

            if (newRow >= 0 && newRow < 9 && newCol >= 0 && newCol < 12)
            {
                if (game.Board.Grid[newRow, newCol] != null)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private async Task PlaceTileAsync(GameState game, PlaceTileCommand command)
    {
        var player = game.Players.First(p => p.PlayerId == command.PlayerId);
        
        // Remove tile from player's hand
        player.TileIds.Remove(command.TileId);

        // Place tile on board
        var tilePosition = new TilePosition
        {
            Row = command.Row,
            Column = command.Column,
            TileId = command.TileId
        };

        game.Board.Grid[command.Row, command.Column] = tilePosition;

        // Check for new hotel chain formation or chain extension
        await CheckHotelChainFormationAsync(game, tilePosition);

        // Draw new tile
        if (game.Board.TileBag.Count > 0)
        {
            var newTile = game.Board.TileBag[0];
            game.Board.TileBag.RemoveAt(0);
            player.TileIds.Add(newTile);
        }

        Logger.LogDebug("Placed tile {TileId} at ({Row}, {Column}) for player {PlayerId}", 
            command.TileId, command.Row, command.Column, command.PlayerId);
    }

    private async Task CheckHotelChainFormationAsync(GameState game, TilePosition newTile)
    {
        // Get adjacent tiles
        var adjacentTiles = GetAdjacentTiles(game, newTile);

        if (adjacentTiles.Count == 0)
        {
            return; // Isolated tile, no chain formation
        }

        // Check if adjacent tiles belong to existing chains
        var adjacentChains = adjacentTiles
            .Where(t => t.HotelChain != null)
            .Select(t => t.HotelChain!)
            .Distinct()
            .ToList();

        if (adjacentChains.Count == 0)
        {
            // Create new hotel chain
            await CreateNewHotelChainAsync(game, newTile, adjacentTiles);
        }
        else if (adjacentChains.Count == 1)
        {
            // Extend existing chain
            await ExtendHotelChainAsync(game, newTile, adjacentChains[0]);
        }
        else
        {
            // Merge multiple chains
            await MergeHotelChainsAsync(game, newTile, adjacentChains);
        }
    }

    private List<TilePosition> GetAdjacentTiles(GameState game, TilePosition tile)
    {
        var adjacentTiles = new List<TilePosition>();
        var directions = new[] { (-1, 0), (1, 0), (0, -1), (0, 1) };

        foreach (var (dr, dc) in directions)
        {
            var newRow = tile.Row + dr;
            var newCol = tile.Column + dc;

            if (newRow >= 0 && newRow < 9 && newCol >= 0 && newCol < 12)
            {
                var adjacentTile = game.Board.Grid[newRow, newCol];
                if (adjacentTile != null)
                {
                    adjacentTiles.Add(adjacentTile);
                }
            }
        }

        return adjacentTiles;
    }

    private async Task CreateNewHotelChainAsync(GameState game, TilePosition newTile, List<TilePosition> adjacentTiles)
    {
        if (game.Board.AvailableChains.Count == 0)
        {
            Logger.LogWarning("No available hotel chains for new formation");
            return;
        }

        var chainName = game.Board.AvailableChains[0];
        game.Board.AvailableChains.RemoveAt(0);

        var chain = game.Board.HotelChains[chainName];
        chain.Tiles.Add(newTile);
        chain.Tiles.AddRange(adjacentTiles);
        chain.StockPrice = CalculateStockPrice(chain.Tiles.Count);

        // Update tile chain assignments
        newTile.HotelChain = chainName;
        foreach (var tile in adjacentTiles)
        {
            tile.HotelChain = chainName;
        }

        // Check if chain becomes safe
        if (chain.Tiles.Count >= 11)
        {
            chain.IsSafe = true;
            await PublishAsync(new HotelChainSafeEvent
            {
                GameId = game.GameId,
                ChainName = chainName,
                TileCount = chain.Tiles.Count,
                SafeAt = DateTime.UtcNow
            });
        }

        // Publish chain created event
        await PublishAsync(new HotelChainCreatedEvent
        {
            GameId = game.GameId,
            ChainName = chainName,
            Tiles = chain.Tiles.ToList(),
            CreatedAt = DateTime.UtcNow
        });

        Logger.LogInformation("Created new hotel chain {ChainName} with {TileCount} tiles", chainName, chain.Tiles.Count);
    }

    private async Task ExtendHotelChainAsync(GameState game, TilePosition newTile, string chainName)
    {
        var chain = game.Board.HotelChains[chainName];
        chain.Tiles.Add(newTile);
        chain.StockPrice = CalculateStockPrice(chain.Tiles.Count);
        newTile.HotelChain = chainName;

        // Check if chain becomes safe
        if (chain.Tiles.Count >= 11 && !chain.IsSafe)
        {
            chain.IsSafe = true;
            await PublishAsync(new HotelChainSafeEvent
            {
                GameId = game.GameId,
                ChainName = chainName,
                TileCount = chain.Tiles.Count,
                SafeAt = DateTime.UtcNow
            });
        }

        // Check if chain becomes terminal (game ends)
        if (chain.Tiles.Count >= 41)
        {
            chain.IsTerminal = true;
            await EndGameAsync(game);
        }

        Logger.LogDebug("Extended hotel chain {ChainName} to {TileCount} tiles", chainName, chain.Tiles.Count);
    }

    private async Task MergeHotelChainsAsync(GameState game, TilePosition newTile, List<string> chainNames)
    {
        // Find the largest chain (survivor)
        var survivingChain = chainNames
            .OrderByDescending(c => game.Board.HotelChains[c].Tiles.Count)
            .First();

        var mergedChains = chainNames.Where(c => c != survivingChain).ToList();
        var survivor = game.Board.HotelChains[survivingChain];

        // Merge all tiles into surviving chain
        foreach (var chainName in mergedChains)
        {
            var mergedChain = game.Board.HotelChains[chainName];
            survivor.Tiles.AddRange(mergedChain.Tiles);

            // Update tile chain assignments
            foreach (var tile in mergedChain.Tiles)
            {
                tile.HotelChain = survivingChain;
            }

            // Move available shares back to available pool
            survivor.AvailableShares += mergedChain.AvailableShares;

            // Clear merged chain
            mergedChain.Tiles.Clear();
            mergedChain.AvailableShares = 0;
            game.Board.AvailableChains.Add(chainName);
        }

        // Add the new tile
        survivor.Tiles.Add(newTile);
        newTile.HotelChain = survivingChain;
        survivor.StockPrice = CalculateStockPrice(survivor.Tiles.Count);

        // Check if chain becomes safe
        if (survivor.Tiles.Count >= 11 && !survivor.IsSafe)
        {
            survivor.IsSafe = true;
            await PublishAsync(new HotelChainSafeEvent
            {
                GameId = game.GameId,
                ChainName = survivingChain,
                TileCount = survivor.Tiles.Count,
                SafeAt = DateTime.UtcNow
            });
        }

        // Check if chain becomes terminal (game ends)
        if (survivor.Tiles.Count >= 41)
        {
            survivor.IsTerminal = true;
            await EndGameAsync(game);
        }

        // Calculate merge bonuses
        var bonuses = CalculateMergeBonuses(game, survivingChain, mergedChains);

        // Publish merge event
        await PublishAsync(new HotelChainMergedEvent
        {
            GameId = game.GameId,
            SurvivingChain = survivingChain,
            MergedChains = mergedChains,
            Bonuses = bonuses,
            MergedAt = DateTime.UtcNow
        });

        Logger.LogInformation("Merged hotel chains: {SurvivingChain} absorbed {MergedChains}", 
            survivingChain, string.Join(", ", mergedChains));
    }

    private bool IsValidStockPurchase(GameState game, string playerId, string chainName, int quantity)
    {
        var player = game.Players.First(p => p.PlayerId == playerId);

        // Check if chain exists
        if (!game.Board.HotelChains.ContainsKey(chainName))
        {
            return false;
        }

        var chain = game.Board.HotelChains[chainName];

        // Check if chain has available shares
        if (chain.AvailableShares < quantity)
        {
            return false;
        }

        // Check if player has enough money
        var totalCost = chain.StockPrice * quantity;
        if (player.Money < totalCost)
        {
            return false;
        }

        // Check purchase limits (max 3 shares per turn)
        if (quantity > 3)
        {
            return false;
        }

        return true;
    }

    private async Task PurchaseStockAsync(GameState game, BuyStockCommand command)
    {
        var player = game.Players.First(p => p.PlayerId == command.PlayerId);
        var chain = game.Board.HotelChains[command.ChainName];

        var totalCost = chain.StockPrice * command.Quantity;
        
        // Deduct money
        player.Money -= totalCost;

        // Add stocks
        if (!player.Stocks.ContainsKey(command.ChainName))
        {
            player.Stocks[command.ChainName] = 0;
        }
        player.Stocks[command.ChainName] += command.Quantity;

        // Reduce available shares
        chain.AvailableShares -= command.Quantity;

        Logger.LogDebug("Player {PlayerId} purchased {Quantity} shares of {ChainName} for ${TotalCost}", 
            command.PlayerId, command.Quantity, command.ChainName, totalCost);
    }

    private async Task UpdateStockPricesAsync(GameState game)
    {
        var updatedPrices = new Dictionary<string, int>();

        foreach (var chain in game.Board.HotelChains.Values)
        {
            if (chain.Tiles.Count > 0)
            {
                chain.StockPrice = CalculateStockPrice(chain.Tiles.Count);
                updatedPrices[chain.Name] = chain.StockPrice;
            }
        }

        if (updatedPrices.Count > 0)
        {
            await PublishAsync(new StockPriceUpdatedEvent
            {
                GameId = game.GameId,
                StockPrices = updatedPrices,
                UpdatedAt = DateTime.UtcNow
            });
        }
    }

    private int CalculateStockPrice(int tileCount)
    {
        return tileCount switch
        {
            0 => 0,
            1 => 200,
            2 => 300,
            3 => 400,
            4 => 500,
            5 => 600,
            6 => 700,
            7 => 800,
            8 => 900,
            9 => 1000,
            10 => 1100,
            11 => 1200,
            12 => 1400,
            13 => 1600,
            14 => 1800,
            15 => 2000,
            16 => 2200,
            17 => 2400,
            18 => 2600,
            19 => 2800,
            20 => 3000,
            21 => 3300,
            22 => 3600,
            23 => 3900,
            24 => 4200,
            25 => 4500,
            26 => 5000,
            27 => 5500,
            28 => 6000,
            29 => 6500,
            30 => 7000,
            31 => 7500,
            32 => 8000,
            33 => 8500,
            34 => 9000,
            35 => 9500,
            36 => 10000,
            37 => 11000,
            38 => 12000,
            39 => 13000,
            40 => 14000,
            41 => 15000,
            _ => 15000
        };
    }

    private Dictionary<string, MergeBonus> CalculateMergeBonuses(GameState game, string survivingChain, List<string> mergedChains)
    {
        var bonuses = new Dictionary<string, MergeBonus>();

        foreach (var chainName in mergedChains)
        {
            var chain = game.Board.HotelChains[chainName];
            var stockHolders = game.Players
                .Where(p => p.Stocks.ContainsKey(chainName) && p.Stocks[chainName] > 0)
                .OrderByDescending(p => p.Stocks[chainName])
                .ToList();

            if (stockHolders.Count == 0)
            {
                continue;
            }

            var majorityHolder = stockHolders[0];
            var minorityHolder = stockHolders.Count > 1 ? stockHolders[1] : null;

            var majorityBonus = chain.Tiles.Count * 200;
            var minorityBonus = chain.Tiles.Count * 100;

            if (majorityHolder != null)
            {
                if (!bonuses.ContainsKey(majorityHolder.PlayerId))
                {
                    bonuses[majorityHolder.PlayerId] = new MergeBonus { PlayerId = majorityHolder.PlayerId };
                }
                bonuses[majorityHolder.PlayerId].MajorityBonus += majorityBonus;
                bonuses[majorityHolder.PlayerId].TotalBonus += majorityBonus;
            }

            if (minorityHolder != null)
            {
                if (!bonuses.ContainsKey(minorityHolder.PlayerId))
                {
                    bonuses[minorityHolder.PlayerId] = new MergeBonus { PlayerId = minorityHolder.PlayerId };
                }
                bonuses[minorityHolder.PlayerId].MinorityBonus += minorityBonus;
                bonuses[minorityHolder.PlayerId].TotalBonus += minorityBonus;
            }
        }

        return bonuses;
    }

    private async Task EndGameAsync(GameState game)
    {
        game.Status = GameStatus.Ended;
        game.EndedAt = DateTime.UtcNow;

        // Calculate final scores
        var finalScores = new Dictionary<string, int>();
        foreach (var player in game.Players)
        {
            var score = player.Money;
            
            // Add stock values
            foreach (var (chainName, quantity) in player.Stocks)
            {
                if (game.Board.HotelChains.TryGetValue(chainName, out var chain))
                {
                    score += chain.StockPrice * quantity;
                }
            }

            finalScores[player.PlayerId] = score;
        }

        // Find winner
        var winner = finalScores.OrderByDescending(kv => kv.Value).First();
        var winnerPlayer = game.Players.First(p => p.PlayerId == winner.Key);

        // Publish game ended event
        await PublishAsync(new GameEndedEvent
        {
            GameId = game.GameId,
            Winner = winnerPlayer.PlayerName,
            FinalScores = finalScores,
            EndedAt = DateTime.UtcNow
        });

        Logger.LogInformation("Game {GameId} ended. Winner: {Winner} with score {Score}", 
            game.GameId, winnerPlayer.PlayerName, winner.Value);
    }

    #endregion

    #region State Transitions
    
    protected override void GAgentTransitionState(AcquireGameGAgentState state, StateLogEventBase<AcquireGameGAgentStateLogEvent> @event)
    {
        switch (@event)
        {
            case GameCreatedStateEvent e:
                // Create new game state
                var game = new GameState
                {
                    GameId = e.GameId,
                    Status = GameStatus.WaitingForPlayers,
                    CreatedAt = e.CreatedAt
                };
                
                // Add creator as first player
                var creatorPlayer = new PlayerInfo
                {
                    PlayerId = e.CreatorPlayerId,
                    PlayerName = e.CreatorName,
                    Money = 6000,
                    TileIds = new List<string>(),
                    Stocks = new Dictionary<string, int>(),
                    IsActive = true
                };
                game.Players.Add(creatorPlayer);
                
                // Update state collections
                state.Games[e.GameId] = game;
                state.GamePlayers[e.GameId] = new List<string> { e.CreatorPlayerId };
                state.PlayerGameMapping[e.CreatorPlayerId] = e.GameId;
                state.TotalGamesCreated++;
                break;
                
            case PlayerJoinedStateEvent e:
                if (!state.Games.TryGetValue(e.GameId, out var gameState))
                    return;
                    
                var newPlayer = new PlayerInfo
                {
                    PlayerId = e.PlayerId,
                    PlayerName = e.PlayerName,
                    Money = 6000,
                    TileIds = new List<string>(),
                    Stocks = new Dictionary<string, int>(),
                    IsActive = true
                };
                gameState.Players.Add(newPlayer);
                state.GamePlayers[e.GameId].Add(e.PlayerId);
                state.PlayerGameMapping[e.PlayerId] = e.GameId;
                break;
                
            case PlayerLeftStateEvent e:
                if (!state.Games.TryGetValue(e.GameId, out var leaveGameState))
                    return;
                    
                var player = leaveGameState.Players.FirstOrDefault(p => p.PlayerId == e.PlayerId);
                if (player != null)
                {
                    player.IsActive = false;
                }
                state.GamePlayers[e.GameId].Remove(e.PlayerId);
                state.PlayerGameMapping.Remove(e.PlayerId);
                break;
                
            case GameStartedStateEvent e:
                if (!state.Games.TryGetValue(e.GameId, out var startGameState))
                    return;
                    
                startGameState.Status = GameStatus.InProgress;
                startGameState.StartedAt = e.StartedAt;
                break;
        }
    }
    
    #endregion
}

#endregion