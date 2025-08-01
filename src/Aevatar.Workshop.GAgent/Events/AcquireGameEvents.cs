using Aevatar.Core.Abstractions;
using Orleans;
using System.ComponentModel;

namespace Aevatar.Workshop.GAgent.Events.Acquire;

#region Game Core Events

/// <summary>
/// Game initialization event
/// </summary>
[GenerateSerializer]
public class InitializeGameEvent : EventBase
{
    [Id(0)] 
    [Description("Unique identifier for the game session")]
    public string GameId { get; set; } = string.Empty;
    
    [Id(1)] 
    [Description("List of player names joining the game")]
    public List<string> PlayerNames { get; set; } = new();
}

/// <summary>
/// Game started event
/// </summary>
[GenerateSerializer]
public class GameStartedEvent : EventBase
{
    [Id(0)] public string GameId { get; set; } = string.Empty;
    [Id(1)] public List<PlayerInfo> Players { get; set; } = new();
    [Id(2)] public DateTime StartedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Game ended event
/// </summary>
[GenerateSerializer]
public class GameEndedEvent : EventBase
{
    [Id(0)] public string GameId { get; set; } = string.Empty;
    [Id(1)] public string Winner { get; set; } = string.Empty;
    [Id(2)] public Dictionary<string, int> FinalScores { get; set; } = new();
    [Id(3)] public DateTime EndedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Player join game event
/// </summary>
[GenerateSerializer]
public class PlayerJoinGameEvent : EventBase
{
    [Id(0)] public string GameId { get; set; } = string.Empty;
    [Id(1)] public string PlayerName { get; set; } = string.Empty;
    [Id(2)] public string PlayerId { get; set; } = string.Empty;
}

/// <summary>
/// Player leave game event
/// </summary>
[GenerateSerializer]
public class PlayerLeaveGameEvent : EventBase
{
    [Id(0)] public string GameId { get; set; } = string.Empty;
    [Id(1)] public string PlayerId { get; set; } = string.Empty;
}

#endregion

#region Tile and Board Events

/// <summary>
/// Place tile command
/// </summary>
[GenerateSerializer]
public class PlaceTileCommand : EventBase
{
    [Id(0)] public string GameId { get; set; } = string.Empty;
    [Id(1)] public string PlayerId { get; set; } = string.Empty;
    [Id(2)] public int Row { get; set; }
    [Id(3)] public int Column { get; set; }
    [Id(4)] public string TileId { get; set; } = string.Empty;
}

/// <summary>
/// Tile placed event
/// </summary>
[GenerateSerializer]
public class TilePlacedEvent : EventBase
{
    [Id(0)] public string GameId { get; set; } = string.Empty;
    [Id(1)] public string PlayerId { get; set; } = string.Empty;
    [Id(2)] public int Row { get; set; }
    [Id(3)] public int Column { get; set; }
    [Id(4)] public string TileId { get; set; } = string.Empty;
    [Id(5)] public DateTime PlacedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Board state updated event
/// </summary>
[GenerateSerializer]
public class BoardStateUpdatedEvent : EventBase
{
    [Id(0)] public string GameId { get; set; } = string.Empty;
    [Id(1)] public BoardState BoardState { get; set; } = new();
    [Id(2)] public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

#endregion

#region Hotel Chain Events

/// <summary>
/// Hotel chain created event
/// </summary>
[GenerateSerializer]
public class HotelChainCreatedEvent : EventBase
{
    [Id(0)] public string GameId { get; set; } = string.Empty;
    [Id(1)] public string ChainName { get; set; } = string.Empty;
    [Id(2)] public List<TilePosition> Tiles { get; set; } = new();
    [Id(3)] public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Hotel chain merged event
/// </summary>
[GenerateSerializer]
public class HotelChainMergedEvent : EventBase
{
    [Id(0)] public string GameId { get; set; } = string.Empty;
    [Id(1)] public string SurvivingChain { get; set; } = string.Empty;
    [Id(2)] public List<string> MergedChains { get; set; } = new();
    [Id(3)] public Dictionary<string, MergeBonus> Bonuses { get; set; } = new();
    [Id(4)] public DateTime MergedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Hotel chain safe event (reaches 11+ tiles)
/// </summary>
[GenerateSerializer]
public class HotelChainSafeEvent : EventBase
{
    [Id(0)] public string GameId { get; set; } = string.Empty;
    [Id(1)] public string ChainName { get; set; } = string.Empty;
    [Id(2)] public int TileCount { get; set; }
    [Id(3)] public DateTime SafeAt { get; set; } = DateTime.UtcNow;
}

#endregion

#region Stock Events

/// <summary>
/// Buy stock command
/// </summary>
[GenerateSerializer]
public class BuyStockCommand : EventBase
{
    [Id(0)] public string GameId { get; set; } = string.Empty;
    [Id(1)] public string PlayerId { get; set; } = string.Empty;
    [Id(2)] public string ChainName { get; set; } = string.Empty;
    [Id(3)] public int Quantity { get; set; }
}

/// <summary>
/// Stock purchased event
/// </summary>
[GenerateSerializer]
public class StockPurchasedEvent : EventBase
{
    [Id(0)] public string GameId { get; set; } = string.Empty;
    [Id(1)] public string PlayerId { get; set; } = string.Empty;
    [Id(2)] public string ChainName { get; set; } = string.Empty;
    [Id(3)] public int Quantity { get; set; }
    [Id(4)] public int PricePerShare { get; set; }
    [Id(5)] public int TotalCost { get; set; }
    [Id(6)] public DateTime PurchasedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Stock price updated event
/// </summary>
[GenerateSerializer]
public class StockPriceUpdatedEvent : EventBase
{
    [Id(0)] public string GameId { get; set; } = string.Empty;
    [Id(1)] public Dictionary<string, int> StockPrices { get; set; } = new();
    [Id(2)] public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

#endregion

#region Turn Management Events

/// <summary>
/// Turn started event
/// </summary>
[GenerateSerializer]
public class TurnStartedEvent : EventBase
{
    [Id(0)] public string GameId { get; set; } = string.Empty;
    [Id(1)] public string CurrentPlayerId { get; set; } = string.Empty;
    [Id(2)] public int TurnNumber { get; set; }
    [Id(3)] public DateTime StartedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Turn completed event
/// </summary>
[GenerateSerializer]
public class TurnCompletedEvent : EventBase
{
    [Id(0)] public string GameId { get; set; } = string.Empty;
    [Id(1)] public string PlayerId { get; set; } = string.Empty;
    [Id(2)] public int TurnNumber { get; set; }
    [Id(3)] public List<TurnAction> Actions { get; set; } = new();
    [Id(4)] public DateTime CompletedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Player must merge stocks event
/// </summary>
[GenerateSerializer]
public class PlayerMustMergeStocksEvent : EventBase
{
    [Id(0)] public string GameId { get; set; } = string.Empty;
    [Id(1)] public string PlayerId { get; set; } = string.Empty;
    [Id(2)] public string SurvivingChain { get; set; } = string.Empty;
    [Id(3)] public List<string> MergedChains { get; set; } = new();
    [Id(4)] public Dictionary<string, int> OwnedStocks { get; set; } = new();
    [Id(5)] public DateTime Deadline { get; set; } = DateTime.UtcNow.AddMinutes(1);
}

#endregion

#region Query Events

/// <summary>
/// Get game state query
/// </summary>
[GenerateSerializer]
public class GetGameStateQuery : EventBase
{
    [Id(0)] public string GameId { get; set; } = string.Empty;
}

/// <summary>
/// Get player state query
/// </summary>
[GenerateSerializer]
public class GetPlayerStateQuery : EventBase
{
    [Id(0)] public string GameId { get; set; } = string.Empty;
    [Id(1)] public string PlayerId { get; set; } = string.Empty;
}

/// <summary>
/// Get available actions query
/// </summary>
[GenerateSerializer]
public class GetAvailableActionsQuery : EventBase
{
    [Id(0)] public string GameId { get; set; } = string.Empty;
    [Id(1)] public string PlayerId { get; set; } = string.Empty;
}

#endregion

#region Data Models

/// <summary>
/// Player information
/// </summary>
[GenerateSerializer]
public class PlayerInfo
{
    [Id(0)] public string PlayerId { get; set; } = string.Empty;
    [Id(1)] public string PlayerName { get; set; } = string.Empty;
    [Id(2)] public int Money { get; set; } = 6000; // Starting money
    [Id(3)] public List<string> TileIds { get; set; } = new();
    [Id(4)] public Dictionary<string, int> Stocks { get; set; } = new();
    [Id(5)] public bool IsActive { get; set; } = true;
}

/// <summary>
/// Tile position
/// </summary>
[GenerateSerializer]
public class TilePosition
{
    [Id(0)] public int Row { get; set; }
    [Id(1)] public int Column { get; set; }
    [Id(2)] public string TileId { get; set; } = string.Empty;
    [Id(3)] public string? HotelChain { get; set; }
}

/// <summary>
/// Board state
/// </summary>
[GenerateSerializer]
public class BoardState
{
    [Id(0)] public TilePosition[,] Grid { get; set; } = new TilePosition[9, 12]; // 9x12 grid
    [Id(1)] public Dictionary<string, HotelChainInfo> HotelChains { get; set; } = new();
    [Id(2)] public List<string> AvailableChains { get; set; } = new();
    [Id(3)] public List<string> TileBag { get; set; } = new();
}

/// <summary>
/// Hotel chain information
/// </summary>
[GenerateSerializer]
public class HotelChainInfo
{
    [Id(0)] public string Name { get; set; } = string.Empty;
    [Id(1)] public List<TilePosition> Tiles { get; set; } = new();
    [Id(2)] public bool IsSafe { get; set; } = false; // Safe at 11+ tiles
    [Id(3)] public bool IsTerminal { get; set; } = false; // Terminal at 41+ tiles
    [Id(4)] public int StockPrice { get; set; }
    [Id(5)] public int AvailableShares { get; set; } = 25; // Max 25 shares per chain
}

/// <summary>
/// Merge bonus information
/// </summary>
[GenerateSerializer]
public class MergeBonus
{
    [Id(0)] public string PlayerId { get; set; } = string.Empty;
    [Id(1)] public int MajorityBonus { get; set; }
    [Id(2)] public int MinorityBonus { get; set; }
    [Id(3)] public int TotalBonus { get; set; }
}

/// <summary>
/// Turn action
/// </summary>
[GenerateSerializer]
public class TurnAction
{
    [Id(0)] public string ActionType { get; set; } = string.Empty; // "PlaceTile", "BuyStock", "MergeStocks"
    [Id(1)] public Dictionary<string, object> Parameters { get; set; } = new();
    [Id(2)] public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Game state
/// </summary>
[GenerateSerializer]
public class GameState
{
    [Id(0)] public string GameId { get; set; } = string.Empty;
    [Id(1)] public GameStatus Status { get; set; } = GameStatus.WaitingForPlayers;
    [Id(2)] public List<PlayerInfo> Players { get; set; } = new();
    [Id(3)] public BoardState Board { get; set; } = new();
    [Id(4)] public string CurrentPlayerId { get; set; } = string.Empty;
    [Id(5)] public int CurrentTurn { get; set; } = 0;
    [Id(6)] public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    [Id(7)] public DateTime? StartedAt { get; set; }
    [Id(8)] public DateTime? EndedAt { get; set; }
}

/// <summary>
/// Player state
/// </summary>
[GenerateSerializer]
public class PlayerState
{
    [Id(0)] public string PlayerId { get; set; } = string.Empty;
    [Id(1)] public string PlayerName { get; set; } = string.Empty;
    [Id(2)] public int Money { get; set; }
    [Id(3)] public List<string> TileIds { get; set; } = new();
    [Id(4)] public Dictionary<string, int> Stocks { get; set; } = new();
    [Id(5)] public bool IsActive { get; set; }
    [Id(6)] public bool IsCurrentTurn { get; set; }
    [Id(7)] public List<string> AvailableActions { get; set; } = new();
}

/// <summary>
/// Available actions for player
/// </summary>
[GenerateSerializer]
public class AvailableActions
{
    [Id(0)] public bool CanPlaceTile { get; set; }
    [Id(1)] public List<string> CanBuyStocks { get; set; } = new();
    [Id(2)] public bool MustMergeStocks { get; set; }
    [Id(3)] public string? MergeSurvivingChain { get; set; }
    [Id(4)] public List<string> MergeMergedChains { get; set; } = new();
}

[GenerateSerializer]
public enum GameStatus
{
    WaitingForPlayers,
    InProgress,
    Ended
}

#endregion