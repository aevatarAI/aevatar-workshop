using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Aevatar.Workshop.Host.Controllers;

[ApiController]
[Route("[controller]")]
public class HealthController : ControllerBase
{
    private readonly ILogger<HealthController> _logger;

    public HealthController(ILogger<HealthController> logger)
    {
        _logger = logger;
    }

    [HttpGet]
    public IActionResult Get()
    {
        _logger.LogDebug("Health check requested");
        
        return Ok(new
        {
            status = "healthy",
            timestamp = DateTime.UtcNow,
            service = "Aevatar.Workshop.Host",
            version = GetType().Assembly.GetName().Version?.ToString() ?? "unknown"
        });
    }

    [HttpGet("ready")]
    public async Task<IActionResult> Ready()
    {
        _logger.LogDebug("Readiness check requested");
        
        try
        {
            // 这里可以添加更详细的就绪检查逻辑
            // 例如检查数据库连接、外部服务等
            
            return Ok(new
            {
                status = "ready",
                timestamp = DateTime.UtcNow,
                service = "Aevatar.Workshop.Host"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Readiness check failed");
            return StatusCode(503, new
            {
                status = "not ready",
                error = ex.Message,
                timestamp = DateTime.UtcNow
            });
        }
    }

    [HttpGet("live")]
    public IActionResult Live()
    {
        _logger.LogDebug("Liveness check requested");
        
        return Ok(new
        {
            status = "alive",
            timestamp = DateTime.UtcNow,
            service = "Aevatar.Workshop.Host"
        });
    }
}