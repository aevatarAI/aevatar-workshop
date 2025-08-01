using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Aevatar.Core.Abstractions.Extensions;
using Aevatar.Workshop.GAgent.Events.Acquire;
using Aevatar.Workshop.GAgent.GAgents.Acquire;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Aevatar.Workshop.Client.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AcquireController : ControllerBase
{
    private readonly IGAgentFactory _gAgentFactory;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<AcquireController> _logger;

    public AcquireController(
        IGAgentFactory gAgentFactory,
        IHostEnvironment environment,
        ILogger<AcquireController> logger)
    {
        _gAgentFactory = gAgentFactory;
        _environment = environment;
        _logger = logger;
    }

    #region Game Management

    /// <summary>
    /// Create a new Acquire game
    /// </summary>
    [HttpPost("create")]
    public async Task<IActionResult> CreateGame([FromBody] CreateGameRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.CreatorName))
            {
                return BadRequest(new { success = false, error = "Creator name is required" });
            }

            var gameId = request.GameId ?? Guid.NewGuid().ToString("N");
            var creatorPlayerId = Guid.NewGuid().ToString("N");
            
            var gameGAgent = await _gAgentFactory.GetGAgentAsync<IAcquireGameGAgent>(Guid.Parse(gameId));
            
            var gameState = await gameGAgent.CreateGameAsync(gameId, request.CreatorName);

            // Create Player GAgent for the creator
            var playerGAgent = await _gAgentFactory.GetGAgentAsync<IAcquirePlayerGAgent>(Guid.Parse(creatorPlayerId));
            await playerGAgent.JoinGameAsync(gameId, request.CreatorName);
            return Ok(new
            {
                success = true,
                gameId = gameState.GameId,
                playerId = creatorPlayerId,
                playerName = request.CreatorName,
                status = gameState.Status,
                createdAt = gameState.CreatedAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create game");
            return HandleError(ex);
        }
    }

    /// <summary>
    /// Get game state
    /// </summary>
    [HttpGet("game/{gameId}/state")]
    public async Task<IActionResult> GetGameState(string gameId)
    {
        try
        {
            var gameGAgent = await _gAgentFactory.GetGAgentAsync<IAcquireGameGAgent>(Guid.Parse(gameId));
            var gameState = await gameGAgent.GetGameStateAsync(gameId);

            return Ok(new
            {
                success = true,
                game = new
                {
                    gameState.GameId,
                    gameState.Status,
                    gameState.CurrentPlayerId,
                    gameState.CurrentTurn,
                    gameState.CreatedAt,
                    gameState.StartedAt,
                    gameState.EndedAt
                },
                players = gameState.Players.Select(p => new
                {
                    p.PlayerId,
                    p.PlayerName,
                    p.Money,
                    p.TileIds,
                    p.Stocks,
                    p.IsActive
                }),
                board = new
                {
                    hotelChains = gameState.Board.HotelChains.Select(c => new
                    {
                        c.Key,
                        c.Value.Name,
                        TileCount = c.Value.Tiles.Count,
                        c.Value.IsSafe,
                        c.Value.IsTerminal,
                        c.Value.StockPrice,
                        c.Value.AvailableShares
                    }),
                    availableChains = gameState.Board.AvailableChains,
                    remainingTiles = gameState.Board.TileBag.Count
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get game state for {GameId}", gameId);
            return HandleError(ex);
        }
    }

    /// <summary>
    /// Join a game as a player
    /// </summary>
    [HttpPost("game/{gameId}/join")]
    public async Task<IActionResult> JoinGame(string gameId, [FromBody] JoinGameRequest request)
    {
        try
        {
            var playerId = request.PlayerId ?? Guid.NewGuid().ToString("N");
            var playerGAgent = await _gAgentFactory.GetGAgentAsync<IAcquirePlayerGAgent>(Guid.Parse(playerId));
            
            var success = await playerGAgent.JoinGameAsync(gameId, request.PlayerName);
            
            if (!success)
            {
                return BadRequest(new { success = false, error = "Failed to join game" });
            }

            return Ok(new
            {
                success = true,
                playerId = playerId,
                gameId = gameId,
                playerName = request.PlayerName
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to join game {GameId}", gameId);
            return HandleError(ex);
        }
    }

    /// <summary>
    /// Leave a game
    /// </summary>
    [HttpPost("player/{playerId}/leave")]
    public async Task<IActionResult> LeaveGame(string playerId)
    {
        try
        {
            var playerGAgent = await _gAgentFactory.GetGAgentAsync<IAcquirePlayerGAgent>(Guid.Parse(playerId));
            var success = await playerGAgent.LeaveGameAsync();
            
            if (!success)
            {
                return BadRequest(new { success = false, error = "Failed to leave game" });
            }

            return Ok(new { success = true, playerId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to leave game for player {PlayerId}", playerId);
            return HandleError(ex);
        }
    }

    #endregion

    #region Player Actions

    /// <summary>
    /// Place a tile
    /// </summary>
    [HttpPost("player/{playerId}/place-tile")]
    public async Task<IActionResult> PlaceTile(string playerId, [FromBody] PlaceTileRequest request)
    {
        try
        {
            var playerGAgent = await _gAgentFactory.GetGAgentAsync<IAcquirePlayerGAgent>(Guid.Parse(playerId));
            var success = await playerGAgent.PlaceTileAsync(request.Row, request.Column, request.TileId);
            
            if (!success)
            {
                return BadRequest(new { success = false, error = "Failed to place tile" });
            }

            return Ok(new
            {
                success = true,
                playerId,
                tile = new
                {
                    request.TileId,
                    request.Row,
                    request.Column
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to place tile for player {PlayerId}", playerId);
            return HandleError(ex);
        }
    }

    /// <summary>
    /// Buy stock
    /// </summary>
    [HttpPost("player/{playerId}/buy-stock")]
    public async Task<IActionResult> BuyStock(string playerId, [FromBody] BuyStockRequest request)
    {
        try
        {
            var playerGAgent = await _gAgentFactory.GetGAgentAsync<IAcquirePlayerGAgent>(Guid.Parse(playerId));
            var success = await playerGAgent.BuyStockAsync(request.ChainName, request.Quantity);
            
            if (!success)
            {
                return BadRequest(new { success = false, error = "Failed to buy stock" });
            }

            return Ok(new
            {
                success = true,
                playerId,
                purchase = new
                {
                    request.ChainName,
                    request.Quantity
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to buy stock for player {PlayerId}", playerId);
            return HandleError(ex);
        }
    }

    /// <summary>
    /// Handle stock merging
    /// </summary>
    [HttpPost("player/{playerId}/merge-stocks")]
    public async Task<IActionResult> MergeStocks(string playerId, [FromBody] MergeStocksRequest request)
    {
        try
        {
            var playerGAgent = await _gAgentFactory.GetGAgentAsync<IAcquirePlayerGAgent>(Guid.Parse(playerId));
            var success = await playerGAgent.HandleMergeStocksAsync(request.StockChoices);
            
            if (!success)
            {
                return BadRequest(new { success = false, error = "Failed to merge stocks" });
            }

            return Ok(new
            {
                success = true,
                playerId,
                stockChoices = request.StockChoices
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to merge stocks for player {PlayerId}", playerId);
            return HandleError(ex);
        }
    }

    #endregion

    #region Player State

    /// <summary>
    /// Get player state
    /// </summary>
    [HttpGet("player/{playerId}/state")]
    public async Task<IActionResult> GetPlayerState(string playerId)
    {
        try
        {
            var playerGAgent = await _gAgentFactory.GetGAgentAsync<IAcquirePlayerGAgent>(Guid.Parse(playerId));
            var playerState = await playerGAgent.GetPlayerGameStateAsync();

            return Ok(new
            {
                success = true,
                player = new
                {
                    playerState.GameId,
                    playerState.Money,
                    playerState.TileIds,
                    playerState.Stocks,
                    playerState.IsCurrentTurn,
                    playerState.AvailableActions,
                    playerState.StockPrices,
                    playerState.OtherPlayers,
                    playerState.GameStatus
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get player state for {PlayerId}", playerId);
            return HandleError(ex);
        }
    }

    /// <summary>
    /// Get player game history
    /// </summary>
    [HttpGet("player/{playerId}/history")]
    public async Task<IActionResult> GetPlayerHistory(string playerId)
    {
        try
        {
            var playerGAgent = await _gAgentFactory.GetGAgentAsync<IAcquirePlayerGAgent>(Guid.Parse(playerId));
            var history = await playerGAgent.GetGameHistoryAsync();

            return Ok(new
            {
                success = true,
                playerId,
                history
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get player history for {PlayerId}", playerId);
            return HandleError(ex);
        }
    }

    /// <summary>
    /// Set player connection status
    /// </summary>
    [HttpPost("player/{playerId}/connection")]
    public async Task<IActionResult> SetConnection(string playerId, [FromBody] SetConnectionRequest request)
    {
        try
        {
            var playerGAgent = await _gAgentFactory.GetGAgentAsync<IAcquirePlayerGAgent>(Guid.Parse(playerId));
            var success = await playerGAgent.SetConnectionAsync(request.ConnectionId, request.IsConnected);
            
            if (!success)
            {
                return BadRequest(new { success = false, error = "Failed to set connection status" });
            }

            return Ok(new
            {
                success = true,
                playerId,
                isConnected = request.IsConnected
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set connection for player {PlayerId}", playerId);
            return HandleError(ex);
        }
    }

    #endregion

    #region Game List

    /// <summary>
    /// List all active games (simplified implementation)
    /// </summary>
    [HttpGet("games")]
    public async Task<IActionResult> ListGames()
    {
        try
        {
            // This is a simplified implementation - in a real application,
            // you would have a separate GameListGAgent to track all active games
            return Ok(new
            {
                success = true,
                games = new List<object>(),
                message = "Game listing not implemented in this version"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list games");
            return HandleError(ex);
        }
    }

    #endregion

    #region Private Methods

    private IActionResult HandleError(Exception ex)
    {
        if (_environment.IsDevelopment())
        {
            return StatusCode(500, new
            {
                success = false,
                error = ex.Message,
                stackTrace = ex.StackTrace
            });
        }

        return StatusCode(500, new
        {
            success = false,
            error = "An error occurred while processing your request"
        });
    }

    #endregion
}

#region Request Models

public class CreateGameRequest
{
    public string? GameId { get; set; }
    public string CreatorName { get; set; } = string.Empty;
}

public class JoinGameRequest
{
    public string PlayerName { get; set; } = string.Empty;
    public string? PlayerId { get; set; }
}

public class PlaceTileRequest
{
    public int Row { get; set; }
    public int Column { get; set; }
    public string TileId { get; set; } = string.Empty;
}

public class BuyStockRequest
{
    public string ChainName { get; set; } = string.Empty;
    public int Quantity { get; set; }
}

public class MergeStocksRequest
{
    public Dictionary<string, int> StockChoices { get; set; } = new();
}

public class SetConnectionRequest
{
    public string ConnectionId { get; set; } = string.Empty;
    public bool IsConnected { get; set; }
}

#endregion