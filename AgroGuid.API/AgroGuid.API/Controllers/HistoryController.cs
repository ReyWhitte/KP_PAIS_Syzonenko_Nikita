using AgroGuid.API.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AgroGuid.API.Controllers;

[ApiController]
[Route("api/history")]
public class HistoryController : ControllerBase
{
    private readonly AgroGuidDbContext _db;
    public HistoryController(AgroGuidDbContext db) => _db = db;

    [HttpGet("user/{userId:long}")]
    public async Task<IActionResult> GetHistory(long userId)
    {
        if (userId != (long)(HttpContext.Items["AgroGuidUserId"] ?? 0L)) return Forbid();
        if (userId <= 0) return BadRequest("Некоректний ID користувача.");
        var items = await _db.FieldHistory.AsNoTracking()
            .Where(h => h.UserId == userId)
            .OrderByDescending(h => h.CreatedAt)
            .ThenByDescending(h => h.Id)
            .Take(500)
            .ToListAsync();
        return Ok(items);
    }
}
