
using AgroGuid.API.Data;
using AgroGuid.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AgroGuid.API.Controllers;

[ApiController]
[Route("api/fields")]
public class FieldsController : ControllerBase
{
    private readonly AgroGuidDbContext _db;

    public FieldsController(AgroGuidDbContext db)
    {
        _db = db;
    }

    private long CurrentUserId => (long)(HttpContext.Items["AgroGuidUserId"] ?? 0L);

    // Отримання всіх полів конкретного користувача
    [HttpGet("user/{userId:long}")]
    public async Task<IActionResult> GetFields(long userId)
    {
        if (userId != CurrentUserId) return Forbid();
        var fields = await _db.Fields
            .Where(f => f.UserId == userId)
            .OrderBy(f => f.Id)
            .ToListAsync();

        return Ok(fields);
    }

    // Додавання нового поля
    [HttpPost]
    public async Task<IActionResult> CreateField(
        [FromBody] Field field)
    {
        if (field.UserId != CurrentUserId) return Forbid();
        // Перевірка власника поля
        bool userExists = await _db.Users
            .AnyAsync(u => u.Id == field.UserId);

        if (!userExists)
            return BadRequest("Користувача не знайдено.");

        // Перевірка назви поля
        if (string.IsNullOrWhiteSpace(field.Name))
            return BadRequest("Введіть назву поля.");

        // Дата створення
        field.Id = 0;
        field.CreatedAt = DateTime.UtcNow;
        field.UpdatedAt = DateTime.UtcNow;

        _db.Fields.Add(field);
        _db.FieldHistory.Add(new FieldHistory
        {
            UserId = field.UserId,
            FieldName = field.Name.Trim(),
            Action = "Створено поле",
            Description = "Створено нову земельну ділянку.",
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        return Ok(field);
    }


    // Оновлення інформації про земельну ділянку
    [HttpPut("{id:long}")]
    public async Task<IActionResult> UpdateField(
        long id,
        [FromBody] Field updatedField)
    {
        // Знаходимо поле в базі даних
        var field = await _db.Fields.FindAsync(id);

        if (field == null)
            return NotFound("Поле не знайдено.");
        if (field.UserId != CurrentUserId || updatedField.UserId != CurrentUserId) return Forbid();

        // Перевіряємо назву поля
        if (string.IsNullOrWhiteSpace(updatedField.Name))
            return BadRequest("Введіть назву поля.");

        // Перевіряємо площу
        if (updatedField.AreaHa < 0)
            return BadRequest("Площа не може бути від'ємною.");

        // Перевіряємо існування культури, якщо вона вибрана
        if (updatedField.CropId.HasValue)
        {
            bool cropExists = await _db.Crops
                .AnyAsync(c => c.Id == updatedField.CropId.Value);

            if (!cropExists)
                return BadRequest("Культуру не знайдено.");
        }

        // Не записуємо історію, якщо дані фактично не змінилися.
        bool changed = field.Name != updatedField.Name.Trim()
            || field.AreaHa != updatedField.AreaHa
            || field.CropId != updatedField.CropId
            || field.Notes != updatedField.Notes
            || (field.BoundaryGeojson?.RootElement.GetRawText() ?? "")
               != (updatedField.BoundaryGeojson?.RootElement.GetRawText() ?? "");

        // Оновлюємо дані поля
        field.Name = updatedField.Name.Trim();
        field.AreaHa = updatedField.AreaHa;
        field.CropId = updatedField.CropId;
        field.BoundaryGeojson = updatedField.BoundaryGeojson;
        field.Notes = updatedField.Notes;

        // Оновлюємо дату останньої зміни
        field.UpdatedAt = DateTime.UtcNow;

        if (changed)
        {
            _db.FieldHistory.Add(new FieldHistory
            {
                UserId = field.UserId,
                FieldName = field.Name,
                Action = "Оновлено поле",
                Description = "Змінено характеристики або межі земельної ділянки.",
                CreatedAt = DateTime.UtcNow
            });
        }

        // Зберігаємо поле та історію однією операцією
        await _db.SaveChangesAsync();

        return Ok(field);
    }


    // Видалення поля
    [HttpDelete("{id:long}")]
    public async Task<IActionResult> DeleteField(long id)
    {
        var field = await _db.Fields.FindAsync(id);

        if (field == null)
            return NotFound("Поле не знайдено.");
        if (field.UserId != CurrentUserId) return Forbid();

        _db.FieldHistory.Add(new FieldHistory
        {
            UserId = field.UserId,
            FieldName = field.Name,
            Action = "Видалено поле",
            Description = "Земельну ділянку видалено з карти.",
            CreatedAt = DateTime.UtcNow
        });
        _db.Fields.Remove(field);
        await _db.SaveChangesAsync();

        return Ok(new
        {
            message = "Поле успішно видалено."
        });
    }
}
