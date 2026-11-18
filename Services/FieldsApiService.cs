using System.Text.Json;
using AgroGuid.Models.Local;
namespace AgroGuid.Services;
public class FieldsApiService {
 private readonly SQLiteService _db=new();
 public FieldsApiService(string apiUrl) { }
 private static JsonElement Json(object data)=>JsonSerializer.SerializeToElement(data,new JsonSerializerOptions { PropertyNamingPolicy=JsonNamingPolicy.CamelCase });
 public async Task<JsonElement> GetFieldsAsync(long userId) {
  await _db.InitializeAsync();
  var fields=await _db.Database.Table<LocalField>().Where(x=>x.UserId==(int)userId).ToListAsync();
  return Json(fields.Select(x=>new { x.Id,x.UserId,x.Name,AreaHa=(decimal?)Convert.ToDecimal(x.AreaHa), BoundaryGeojson=JsonDocument.Parse(x.GeometryJson).RootElement.Clone(),x.Notes }).ToArray());
 }
 public async Task<JsonElement> CreateFieldAsync(long userId,string name,decimal? areaHa,string boundaryGeojson,string? notes=null) {
  await _db.InitializeAsync();
  using var check=JsonDocument.Parse(boundaryGeojson);
  var field=new LocalField { UserId=checked((int)userId),Name=name,AreaHa=(double)(areaHa??0),GeometryJson=check.RootElement.GetRawText(),Notes=notes??"",CropName=(notes??"").StartsWith("Культура: ")?(notes??"")["Культура: ".Length..]:"" };
  await _db.Database.InsertAsync(field);
  await LogAsync(field,"Створення поля",$"Створено поле «{name}».");
  return Json(new {field.Id,field.UserId,field.Name,AreaHa=areaHa,BoundaryGeojson=check.RootElement.Clone(),field.Notes});
 }
 public async Task UpdateFieldAsync(long id,long userId,string name,decimal? areaHa,string boundaryGeojson,string? notes=null) {
  await _db.InitializeAsync();
  var field=await _db.Database.Table<LocalField>().Where(x=>x.Id==(int)id&&x.UserId==(int)userId).FirstOrDefaultAsync();
  if(field==null) throw new InvalidOperationException("Поле не знайдено.");
  using var check=JsonDocument.Parse(boundaryGeojson);
  field.Name=name;field.AreaHa=(double)(areaHa??0);field.GeometryJson=check.RootElement.GetRawText();field.Notes=notes??"";
  field.CropName=field.Notes.StartsWith("Культура: ")?field.Notes["Культура: ".Length..]:"";
  await _db.Database.UpdateAsync(field);await LogAsync(field,"Редагування поля",$"Змінено поле «{name}».");
 }
 public async Task DeleteFieldAsync(long id) {
  await _db.InitializeAsync();
  var field=await _db.Database.Table<LocalField>().Where(x=>x.Id==(int)id).FirstOrDefaultAsync();
  if(field==null) return;
  await _db.Database.DeleteAsync(field);await LogAsync(field,"Видалення поля",$"Видалено поле «{field.Name}».");
 }
 private async Task LogAsync(LocalField field,string action,string description) => await _db.Database.InsertAsync(new LocalFieldHistory { UserId=field.UserId,FieldId=field.Id,FieldName=field.Name,Action=action,Description=description,CreatedAt=DateTime.UtcNow });
}
