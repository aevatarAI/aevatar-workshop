using Aevatar.Core.Abstractions;
using Aevatar.Workshop.GAgent.Events.Acquire;
using Aevatar.Workshop.GAgent.GAgents.Acquire;
using Aevatar.Workshop.TestBase;
using Shouldly;
using Xunit.Abstractions;

namespace Aevatar.Workshop.Tests;

[Collection(ClusterCollection.Name)]
public sealed class AcquireGameGAgentTests : AevatarWorkshopTestBase<AevatarWorkshopTestModule>
{
    private readonly ITestOutputHelper _testOutputHelper;

    public AcquireGameGAgentTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    [Fact(DisplayName = "Can create game with creator.")]
    public async Task CreateGameAsync_ShouldCreateGameWithCreator()
    {
        // Arrange
        var gameId = Guid.NewGuid().ToString();
        var creatorName = "Alice";
        var gameAgent = await GAgentFactory.GetGAgentAsync<IAcquireGameGAgent>(Guid.NewGuid());

        // Act
        var gameState = await gameAgent.CreateGameAsync(gameId, creatorName);

        // Assert
        gameState.ShouldNotBeNull();
        gameState.GameId.ShouldBe(gameId);
        gameState.Status.ShouldBe(GameStatus.WaitingForPlayers);
        gameState.Players.Count.ShouldBe(1);
        gameState.Players[0].PlayerName.ShouldBe(creatorName);
        gameState.Players[0].Money.ShouldBe(6000);
        gameState.CreatedAt.ShouldNotBe(default);
    }

    [Fact(DisplayName = "Cannot create game with existed game id.")]
    public async Task CreateGameAsync_ShouldThrowWhenGameExists()
    {
        // Arrange
        var gameId = Guid.NewGuid().ToString();
        var creatorName = "Alice";
        var gameAgent = await GAgentFactory.GetGAgentAsync<IAcquireGameGAgent>(Guid.NewGuid());

        // Act & Assert
        await gameAgent.CreateGameAsync(gameId, creatorName);
        await Should.ThrowAsync<ArgumentException>(() => gameAgent.CreateGameAsync(gameId, "Bob"));
    }

    [Fact(DisplayName = "Can add player to game when join game.")]
    public async Task JoinGameAsync_ShouldAddPlayerToGame()
    {
        // Arrange
        var gameId = Guid.NewGuid().ToString();
        var gameAgent = await GAgentFactory.GetGAgentAsync<IAcquireGameGAgent>(Guid.NewGuid());
        await gameAgent.CreateGameAsync(gameId, "Alice");
        
        var playerId = Guid.NewGuid().ToString();

        // Act
        var result = await gameAgent.JoinGameAsync(gameId, "Bob", playerId);

        // Assert
        result.ShouldBeTrue();
        var gameState = await gameAgent.GetGameStateAsync(gameId);
        gameState.Players.Count.ShouldBe(2);
        gameState.Players.ShouldContain(p => p.PlayerName == "Bob");
        gameState.Players.ShouldContain(p => p.PlayerId == playerId);
    }

    [Fact]
    public async Task JoinGameAsync_ShouldStartGameWithTwoPlayers()
    {
        // Arrange
        var gameId = Guid.NewGuid().ToString();
        var gameAgent = await GAgentFactory.GetGAgentAsync<IAcquireGameGAgent>(Guid.NewGuid());
        await gameAgent.CreateGameAsync(gameId, "Alice");
        
        var playerId = Guid.NewGuid().ToString();

        // Act
        await gameAgent.JoinGameAsync(gameId, "Bob", playerId);

        // Assert
        var gameState = await gameAgent.GetGameStateAsync(gameId);
        gameState.Status.ShouldBe(GameStatus.InProgress);
        gameState.StartedAt.ShouldNotBeNull();
        gameState.Board.ShouldNotBeNull();
        gameState.Board.TileBag.ShouldNotBeEmpty();
        gameState.Players.All(p => p.TileIds.Count == 6).ShouldBeTrue();
    }

    [Fact]
    public async Task GetAvailableActionsAsync_ShouldReturnActionsForCurrentPlayer()
    {
        // Arrange
        var gameId = Guid.NewGuid().ToString();
        var gameAgent = await GAgentFactory.GetGAgentAsync<IAcquireGameGAgent>(Guid.NewGuid());
        await gameAgent.CreateGameAsync(gameId, "Alice");
        
        var playerId = Guid.NewGuid().ToString();
        await gameAgent.JoinGameAsync(gameId, "Bob", playerId);

        var gameState = await gameAgent.GetGameStateAsync(gameId);
        var currentPlayerId = gameState.CurrentPlayerId;

        // Act
        var actions = await gameAgent.GetAvailableActionsAsync(gameId, currentPlayerId);

        // Assert
        actions.ShouldNotBeNull();
        actions.CanPlaceTile.ShouldBeTrue();
        actions.CanBuyStocks.ShouldBeEmpty(); // No chains formed yet
        actions.MustMergeStocks.ShouldBeFalse();
    }

    [Fact]
    public async Task GetAvailableActionsAsync_ShouldReturnEmptyForNonCurrentPlayer()
    {
        // Arrange
        var gameId = Guid.NewGuid().ToString();
        var gameAgent = await GAgentFactory.GetGAgentAsync<IAcquireGameGAgent>(Guid.NewGuid());
        await gameAgent.CreateGameAsync(gameId, "Alice");
        
        var playerId = Guid.NewGuid().ToString();
        await gameAgent.JoinGameAsync(gameId, "Bob", playerId);

        var gameState = await gameAgent.GetGameStateAsync(gameId);
        var nonCurrentPlayerId = gameState.Players.First(p => p.PlayerId != gameState.CurrentPlayerId).PlayerId;

        // Act
        var actions = await gameAgent.GetAvailableActionsAsync(gameId, nonCurrentPlayerId);

        // Assert
        actions.ShouldNotBeNull();
        actions.CanPlaceTile.ShouldBeFalse();
        actions.CanBuyStocks.ShouldBeEmpty();
        actions.MustMergeStocks.ShouldBeFalse();
    }

    [Fact]
    public async Task LeaveGameAsync_ShouldMarkPlayerAsInactive()
    {
        // Arrange
        var gameId = Guid.NewGuid().ToString();
        var gameAgent = await GAgentFactory.GetGAgentAsync<IAcquireGameGAgent>(Guid.NewGuid());
        await gameAgent.CreateGameAsync(gameId, "Alice");
        
        var playerId = Guid.NewGuid().ToString();
        await gameAgent.JoinGameAsync(gameId, "Bob", playerId);

        var gameState = await gameAgent.GetGameStateAsync(gameId);
        var playerToLeave = gameState.Players.First(p => p.PlayerName == "Bob");

        // Act
        var result = await gameAgent.LeaveGameAsync(gameId, playerToLeave.PlayerId);

        // Assert
        result.ShouldBeTrue();
        var updatedGameState = await gameAgent.GetGameStateAsync(gameId);
        var player = updatedGameState.Players.First(p => p.PlayerId == playerToLeave.PlayerId);
        player.IsActive.ShouldBeFalse();
    }

    [Fact]
    public async Task GameFlow_ShouldHandleMultipleTurns()
    {
        // Arrange
        var gameId = Guid.NewGuid().ToString();
        var gameAgent = await GAgentFactory.GetGAgentAsync<IAcquireGameGAgent>(Guid.NewGuid());
        await gameAgent.CreateGameAsync(gameId, "Alice");
        
        var playerId = Guid.NewGuid().ToString();
        await gameAgent.JoinGameAsync(gameId, "Bob", playerId);

        var gameState = await gameAgent.GetGameStateAsync(gameId);
        var firstPlayerId = gameState.CurrentPlayerId;
        var secondPlayerId = gameState.Players.First(p => p.PlayerId != firstPlayerId).PlayerId;

        // Act - Check game state and available actions
        var firstPlayerActions = await gameAgent.GetAvailableActionsAsync(gameId, firstPlayerId);
        var secondPlayerActions = await gameAgent.GetAvailableActionsAsync(gameId, secondPlayerId);

        // Assert - Verify turn-based gameplay
        firstPlayerActions.CanPlaceTile.ShouldBeTrue();
        secondPlayerActions.CanPlaceTile.ShouldBeFalse(); // Not their turn
        gameState.CurrentTurn.ShouldBe(1);

        _testOutputHelper.WriteLine($"Game flow test completed. First player: {firstPlayerId}, Second player: {secondPlayerId}");
    }

    [Fact]
    public async Task BoardState_ShouldBeCorrectlyInitialized()
    {
        // Arrange
        var gameId = Guid.NewGuid().ToString();
        var gameAgent = await GAgentFactory.GetGAgentAsync<IAcquireGameGAgent>(Guid.NewGuid());
        await gameAgent.CreateGameAsync(gameId, "Alice");
        
        var playerId = Guid.NewGuid().ToString();
        await gameAgent.JoinGameAsync(gameId, "Bob", playerId);

        // Act
        var gameState = await gameAgent.GetGameStateAsync(gameId);

        // Assert
        gameState.Board.ShouldNotBeNull();
        gameState.Board.Grid.GetLength(0).ShouldBe(9); // 9 rows
        gameState.Board.Grid.GetLength(1).ShouldBe(12); // 12 columns
        gameState.Board.TileBag.Count.ShouldBe(108 - 12); // 108 total tiles - 12 dealt to 2 players
        gameState.Board.HotelChains.Count.ShouldBe(7); // 7 hotel chains
        gameState.Board.AvailableChains.Count.ShouldBe(7); // All chains available

        // Verify tile bag contains valid tile IDs
        gameState.Board.TileBag.ShouldAllBe(t => t.Length == 2 && char.IsLetter(t[0]) && char.IsDigit(t[1]));
    }

    [Fact]
    public async Task PlayerState_ShouldBeCorrectlyManaged()
    {
        // Arrange
        var gameId = Guid.NewGuid().ToString();
        var gameAgent = await GAgentFactory.GetGAgentAsync<IAcquireGameGAgent>(Guid.NewGuid());
        await gameAgent.CreateGameAsync(gameId, "Alice");
        
        var playerId = Guid.NewGuid().ToString();
        await gameAgent.JoinGameAsync(gameId, "Bob", playerId);

        var gameState = await gameAgent.GetGameStateAsync(gameId);
        var testPlayerId = gameState.Players.First(p => p.PlayerName == "Alice").PlayerId;

        // Act
        var playerState = await gameAgent.GetPlayerStateAsync(gameId, testPlayerId);

        // Assert
        playerState.ShouldNotBeNull();
        playerState.PlayerId.ShouldBe(testPlayerId);
        playerState.PlayerName.ShouldBe("Alice");
        playerState.Money.ShouldBe(6000);
        playerState.TileIds.Count.ShouldBe(6);
        playerState.Stocks.ShouldBeEmpty();
        playerState.IsActive.ShouldBeTrue();
        playerState.IsCurrentTurn.ShouldBe(gameState.CurrentPlayerId == testPlayerId);
    }
}