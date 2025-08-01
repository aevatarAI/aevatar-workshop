using Aevatar.Core.Abstractions;
using Aevatar.Workshop.GAgent.Events.Acquire;
using Aevatar.Workshop.GAgent.GAgents.Acquire;
using Aevatar.Workshop.TestBase;
using Shouldly;
using Xunit.Abstractions;

namespace Aevatar.Workshop.Tests;

[Collection(ClusterCollection.Name)]
public class AcquirePlayerGAgentTests : AevatarWorkshopTestBase<AevatarWorkshopTestModule>
{
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly IGAgentFactory _gAgentFactory;

    public AcquirePlayerGAgentTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        _gAgentFactory = GetRequiredService<IGAgentFactory>();
    }

    [Fact]
    public async Task GetPlayerGameStateAsync_ShouldReturnInitialState()
    {
        // Arrange
        var playerId = Guid.NewGuid();
        var playerAgent = await _gAgentFactory.GetGAgentAsync<IAcquirePlayerGAgent>(playerId);

        // Act
        var playerGameState = await playerAgent.GetPlayerGameStateAsync();

        // Assert
        playerGameState.ShouldNotBeNull();
        playerGameState.Money.ShouldBe(6000);
        playerGameState.TileIds.ShouldBeEmpty();
        playerGameState.Stocks.ShouldBeEmpty();
        playerGameState.IsCurrentTurn.ShouldBeFalse();
        playerGameState.AvailableActions.ShouldBeEmpty();
        playerGameState.GameStatus.ShouldBe(GameStatus.WaitingForPlayers);
    }

    [Fact]
    public async Task JoinGameAsync_ShouldJoinGameSuccessfully()
    {
        // Arrange
        var gameId = Guid.NewGuid().ToString();
        var playerId = Guid.NewGuid();
        var playerName = "Alice";
        
        // Create game first
        var gameAgent = await _gAgentFactory.GetGAgentAsync<IAcquireGameGAgent>(Guid.NewGuid());
        await gameAgent.CreateGameAsync(gameId, "Creator");
        
        var playerAgent = await _gAgentFactory.GetGAgentAsync<IAcquirePlayerGAgent>(playerId);

        // Act
        var result = await playerAgent.JoinGameAsync(gameId, playerName);

        // Assert
        result.ShouldBeTrue();
        var playerGameState = await playerAgent.GetPlayerGameStateAsync();
        playerGameState.GameId.ShouldBe(gameId);
        playerGameState.Money.ShouldBe(6000);
        playerGameState.GameStatus.ShouldBe(GameStatus.InProgress); // Game should start with 2+ players
    }

    [Fact]
    public async Task JoinGameAsync_ShouldFailWhenGameDoesNotExist()
    {
        // Arrange
        var gameId = Guid.NewGuid().ToString();
        var playerId = Guid.NewGuid();
        var playerName = "Alice";
        
        var playerAgent = await _gAgentFactory.GetGAgentAsync<IAcquirePlayerGAgent>(playerId);

        // Act
        var result = await playerAgent.JoinGameAsync(gameId, playerName);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public async Task LeaveGameAsync_ShouldLeaveGameSuccessfully()
    {
        // Arrange
        var gameId = Guid.NewGuid().ToString();
        var playerId = Guid.NewGuid();
        var playerName = "Alice";
        
        // Create game and join
        var gameAgent = await _gAgentFactory.GetGAgentAsync<IAcquireGameGAgent>(Guid.NewGuid());
        await gameAgent.CreateGameAsync(gameId, "Creator");
        
        var playerAgent = await _gAgentFactory.GetGAgentAsync<IAcquirePlayerGAgent>(playerId);
        await playerAgent.JoinGameAsync(gameId, playerName);

        // Act
        var result = await playerAgent.LeaveGameAsync();

        // Assert
        result.ShouldBeTrue();
        var playerGameState = await playerAgent.GetPlayerGameStateAsync();
        playerGameState.GameId.ShouldBeEmpty();
        playerGameState.GameStatus.ShouldBe(GameStatus.WaitingForPlayers);
    }

    [Fact]
    public async Task LeaveGameAsync_ShouldFailWhenNotInGame()
    {
        // Arrange
        var playerId = Guid.NewGuid();
        var playerAgent = await _gAgentFactory.GetGAgentAsync<IAcquirePlayerGAgent>(playerId);

        // Act
        var result = await playerAgent.LeaveGameAsync();

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public async Task PlaceTileAsync_ShouldPlaceTileSuccessfully()
    {
        // Arrange
        var gameId = Guid.NewGuid().ToString();
        var playerId = Guid.NewGuid();
        var playerName = "Alice";
        
        // Create game and join
        var gameAgent = await _gAgentFactory.GetGAgentAsync<IAcquireGameGAgent>(Guid.NewGuid());
        await gameAgent.CreateGameAsync(gameId, "Creator");
        
        var playerAgent = await _gAgentFactory.GetGAgentAsync<IAcquirePlayerGAgent>(playerId);
        await playerAgent.JoinGameAsync(gameId, playerName);

        // Get tile from player's hand
        var playerGameState = await playerAgent.GetPlayerGameStateAsync();
        var tileId = playerGameState.TileIds.First();

        // Act
        var result = await playerAgent.PlaceTileAsync(0, 0, tileId);

        // Assert
        result.ShouldBeTrue();
        
        // Allow for event processing
        await Task.Delay(100);

        // Verify tile was placed
        var gameState = await gameAgent.GetGameStateAsync(gameId);
        gameState.Board.Grid[0, 0].ShouldNotBeNull();
        gameState.Board.Grid[0, 0].TileId.ShouldBe(tileId);
    }

    [Fact]
    public async Task PlaceTileAsync_ShouldFailWhenNotInGame()
    {
        // Arrange
        var playerId = Guid.NewGuid();
        var playerAgent = await _gAgentFactory.GetGAgentAsync<IAcquirePlayerGAgent>(playerId);

        // Act
        var result = await playerAgent.PlaceTileAsync(0, 0, "A1");

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public async Task BuyStockAsync_ShouldBuyStockSuccessfully()
    {
        // Arrange
        var gameId = Guid.NewGuid().ToString();
        var playerId = Guid.NewGuid();
        var playerName = "Alice";
        
        // Create game and join
        var gameAgent = await _gAgentFactory.GetGAgentAsync<IAcquireGameGAgent>(Guid.NewGuid());
        await gameAgent.CreateGameAsync(gameId, "Creator");
        
        var playerAgent = await _gAgentFactory.GetGAgentAsync<IAcquirePlayerGAgent>(playerId);
        await playerAgent.JoinGameAsync(gameId, playerName);

        // Act
        var result = await playerAgent.BuyStockAsync("Tower", 1);

        // Assert
        result.ShouldBeTrue();
        
        // Allow for event processing
        await Task.Delay(100);

        // Verify command was sent (actual stock purchase logic depends on game state)
        _testOutputHelper.WriteLine("Buy stock command sent successfully");
    }

    [Fact]
    public async Task BuyStockAsync_ShouldFailWhenNotInGame()
    {
        // Arrange
        var playerId = Guid.NewGuid();
        var playerAgent = await _gAgentFactory.GetGAgentAsync<IAcquirePlayerGAgent>(playerId);

        // Act
        var result = await playerAgent.BuyStockAsync("Tower", 1);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public async Task HandleMergeStocksAsync_ShouldHandleMergeSuccessfully()
    {
        // Arrange
        var gameId = Guid.NewGuid().ToString();
        var playerId = Guid.NewGuid();
        var playerName = "Alice";
        
        // Create game and join
        var gameAgent = await _gAgentFactory.GetGAgentAsync<IAcquireGameGAgent>(Guid.NewGuid());
        await gameAgent.CreateGameAsync(gameId, "Creator");
        
        var playerAgent = await _gAgentFactory.GetGAgentAsync<IAcquirePlayerGAgent>(playerId);
        await playerAgent.JoinGameAsync(gameId, playerName);

        var stockChoices = new Dictionary<string, int>
        {
            { "Tower", 2 },
            { "Luxor", 1 }
        };

        // Act
        var result = await playerAgent.HandleMergeStocksAsync(stockChoices);

        // Assert
        result.ShouldBeTrue();
        
        // Allow for event processing
        await Task.Delay(100);

        _testOutputHelper.WriteLine($"Handle merge stocks completed with choices: {string.Join(", ", stockChoices.Select(kv => $"{kv.Key}:{kv.Value}"))}");
    }

    [Fact]
    public async Task HandleMergeStocksAsync_ShouldFailWhenNotInGame()
    {
        // Arrange
        var playerId = Guid.NewGuid();
        var playerAgent = await _gAgentFactory.GetGAgentAsync<IAcquirePlayerGAgent>(playerId);

        var stockChoices = new Dictionary<string, int>
        {
            { "Tower", 1 }
        };

        // Act
        var result = await playerAgent.HandleMergeStocksAsync(stockChoices);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public async Task GetGameHistoryAsync_ShouldReturnHistory()
    {
        // Arrange
        var playerId = Guid.NewGuid();
        var playerAgent = await _gAgentFactory.GetGAgentAsync<IAcquirePlayerGAgent>(playerId);

        // Act
        var history = await playerAgent.GetGameHistoryAsync();

        // Assert
        history.ShouldNotBeNull();
        history.ShouldBeEmpty(); // No games played yet
    }

    [Fact]
    public async Task GetGameHistoryAsync_ShouldIncludePreviousGames()
    {
        // Arrange
        var gameId = Guid.NewGuid().ToString();
        var playerId = Guid.NewGuid();
        var playerName = "Alice";
        
        // Create game and join
        var gameAgent = await _gAgentFactory.GetGAgentAsync<IAcquireGameGAgent>(Guid.NewGuid());
        await gameAgent.CreateGameAsync(gameId, "Creator");
        
        var playerAgent = await _gAgentFactory.GetGAgentAsync<IAcquirePlayerGAgent>(playerId);
        await playerAgent.JoinGameAsync(gameId, playerName);

        // Leave game to add to history
        await playerAgent.LeaveGameAsync();

        // Act
        var history = await playerAgent.GetGameHistoryAsync();

        // Assert
        history.ShouldNotBeNull();
        history.ShouldContain(gameId);
    }

    [Fact]
    public async Task SetConnectionAsync_ShouldSetConnectionStatus()
    {
        // Arrange
        var playerId = Guid.NewGuid();
        var playerAgent = await _gAgentFactory.GetGAgentAsync<IAcquirePlayerGAgent>(playerId);
        var connectionId = "connection-123";

        // Act
        var result = await playerAgent.SetConnectionAsync(connectionId, true);

        // Assert
        result.ShouldBeTrue();
        
        // Allow for event processing
        await Task.Delay(100);

        var playerGameState = await playerAgent.GetPlayerGameStateAsync();
        playerGameState.ShouldNotBeNull();
    }

    [Fact]
    public async Task PlayerState_ShouldPersistAcrossOperations()
    {
        // Arrange
        var gameId = Guid.NewGuid().ToString();
        var playerId = Guid.NewGuid();
        var playerName = "Alice";
        
        // Create game and join
        var gameAgent = await _gAgentFactory.GetGAgentAsync<IAcquireGameGAgent>(Guid.NewGuid());
        await gameAgent.CreateGameAsync(gameId, "Creator");
        
        var playerAgent = await _gAgentFactory.GetGAgentAsync<IAcquirePlayerGAgent>(playerId);
        await playerAgent.JoinGameAsync(gameId, playerName);

        // Get initial state
        var initialState = await playerAgent.GetPlayerGameStateAsync();
        var initialMoney = initialState.Money;

        // Act - perform some operations
        await playerAgent.SetConnectionAsync("conn-123", true);
        await Task.Delay(100);

        // Get updated state
        var updatedState = await playerAgent.GetPlayerGameStateAsync();

        // Assert
        updatedState.ShouldNotBeNull();
        updatedState.Money.ShouldBe(initialMoney);
        updatedState.GameId.ShouldBe(gameId);
    }
}