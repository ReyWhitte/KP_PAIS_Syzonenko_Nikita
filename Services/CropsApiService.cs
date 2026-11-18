using System.Text.Json;
namespace AgroGuid.Services;
public class CropsApiService {
 private readonly SQLiteService _db=new();
 public async Task<JsonElement> GetCropsAsync() {
  await _db.InitializeAsync();
  var crops=await _db.Database.Table<AgroGuid.Models.Local.LocalCrop>().OrderBy(x=>x.Name).ToListAsync();
  return JsonSerializer.SerializeToElement(crops.Select(x=>new {x.Id,x.Name,x.Category,x.Description,x.Features,x.CropType}).ToArray(),new JsonSerializerOptions {PropertyNamingPolicy=JsonNamingPolicy.CamelCase});
 }
}
