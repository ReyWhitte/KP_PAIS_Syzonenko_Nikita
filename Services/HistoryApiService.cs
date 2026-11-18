using System.Text.Json;
using AgroGuid.Models.Local;
namespace AgroGuid.Services;
public class HistoryApiService {
 private readonly SQLiteService _db=new();
 public HistoryApiService(string apiUrl) { }
 public async Task<JsonElement> GetHistoryAsync(long userId) {
  await _db.InitializeAsync();
  var rows=await _db.Database.Table<LocalFieldHistory>().Where(x=>x.UserId==(int)userId).OrderByDescending(x=>x.CreatedAt).ToListAsync();
  return JsonSerializer.SerializeToElement(rows.Select(x=>new {x.Id,x.UserId,x.FieldId,x.FieldName,x.Action,x.Description,x.CreatedAt}).ToArray(),new JsonSerializerOptions {PropertyNamingPolicy=JsonNamingPolicy.CamelCase});
 }
}
