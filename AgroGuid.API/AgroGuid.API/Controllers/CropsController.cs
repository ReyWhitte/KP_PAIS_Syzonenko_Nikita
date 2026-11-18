using AgroGuid.API.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AgroGuid.API.Controllers;

[ApiController]
[Route("api/crops")]
public class CropsController : ControllerBase
{
    private readonly AgroGuidDbContext _db;

    public CropsController(AgroGuidDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetCrops()
    {
        var crops = await _db.Crops.AsNoTracking()
            .OrderBy(c => c.Name)
            .ToListAsync();
        return Ok(crops);
    }
}
