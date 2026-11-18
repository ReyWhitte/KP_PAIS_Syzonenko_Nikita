using AgroGuid.API.Data;
using Microsoft.EntityFrameworkCore;
using AgroGuid.API.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AgroGuidDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("AgroGuid")
    )
);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddAuthorization();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseHttpsRedirection();

// Вимагати дійсну сесію для операцій із полями та історією.
app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/api/fields") ||
        context.Request.Path.StartsWithSegments("/api/history"))
    {
        var header = context.Request.Headers.Authorization.ToString();
        if (!header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            context.Response.StatusCode = 401;
            return;
        }
        string token = header.Substring(7).Trim();
        using var scope = context.RequestServices.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AgroGuidDbContext>();
        var session = await db.AuthSessions.AsNoTracking().FirstOrDefaultAsync(s =>
            s.TokenHash == SessionTokens.Hash(token) && s.ExpiresAt > DateTime.UtcNow);
        if (session == null)
        {
            context.Response.StatusCode = 401;
            return;
        }
        context.Items["AgroGuidUserId"] = session.UserId;
    }
    await next();
});
app.UseAuthorization();

app.MapControllers();

app.Run();
