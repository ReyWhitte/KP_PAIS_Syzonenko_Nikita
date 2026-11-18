using AgroGuid.API.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AgroGuid.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DatabaseController : ControllerBase
{
    private readonly AgroGuidDbContext _context;

    public DatabaseController(AgroGuidDbContext context)
    {
        _context = context;
    }

    [HttpGet("status")]
    public async Task<IActionResult> GetStatus()
    {
        bool connected = await _context.Database.CanConnectAsync();

        if (!connected)
        {
            return StatusCode(503, new
            {
                status = "error",
                message = "Не вдалося підключитися до PostgreSQL."
            });
        }

        var usersCount = await _context.Users.CountAsync();
        var cropsCount = await _context.Crops.CountAsync();
        var fieldsCount = await _context.Fields.CountAsync();

        return Ok(new
        {
            status = "connected",
            database = "AgroGuid",
            tables = new
            {
                users = usersCount,
                crops = cropsCount,
                fields = fieldsCount
            }
        });
    }
}