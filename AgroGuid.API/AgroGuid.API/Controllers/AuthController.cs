
using AgroGuid.API.Data;
using AgroGuid.API.DTO;
using AgroGuid.API.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AgroGuid.API.Services;

namespace AgroGuid.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AgroGuidDbContext _context;
    private readonly PasswordHasher<User> _passwordHasher = new();

    public AuthController(AgroGuidDbContext context)
    {
        _context = context;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        string email = request.Email.Trim().ToLowerInvariant();
        string login = request.Login.Trim();

        if (await _context.Users.AnyAsync(u =>
            u.Email.ToLower() == email || u.Login == login))
        {
            return Conflict(new
            {
                message = "Такий логін або email вже використовується."
            });
        }

        var user = new User
        {
            Name = request.Name.Trim(),
            Email = email,
            Login = login,
            CreatedAt = DateTime.UtcNow
        };

        user.PasswordHash =
            _passwordHasher.HashPassword(user, request.Password);

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return Ok(new AuthResponse
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            Login = user.Login,
            Message = "Реєстрація успішна."
        });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        string login = request.Login.Trim();

        var user = await _context.Users.FirstOrDefaultAsync(u =>
            u.Login == login ||
            u.Email.ToLower() == login.ToLower());

        if (user == null)
        {
            return Unauthorized(new
            {
                message = "Неправильний логін або пароль."
            });
        }

        var result = _passwordHasher.VerifyHashedPassword(
            user,
            user.PasswordHash,
            request.Password
        );

        if (result == PasswordVerificationResult.Failed)
        {
            return Unauthorized(new
            {
                message = "Неправильний логін або пароль."
            });
        }

        string token = SessionTokens.Create();
        _context.AuthSessions.Add(new AuthSession
        {
            UserId = user.Id,
            TokenHash = SessionTokens.Hash(token),
            ExpiresAt = DateTime.UtcNow.AddDays(30),
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        return Ok(new AuthResponse
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            Login = user.Login,
            Token = token,
            Message = "Вхід успішний."
        });
    }
    // Перевірка збереженої сесії під час запуску програми.
    [HttpGet("session")]
    public async Task<IActionResult> GetSession()
    {
        string? token = Request.Headers.Authorization.ToString();
        if (!token.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return Unauthorized();
        string raw = token.Substring(7).Trim();
        var session = await _context.AuthSessions.AsNoTracking()
            .FirstOrDefaultAsync(s => s.TokenHash == SessionTokens.Hash(raw)
                && s.ExpiresAt > DateTime.UtcNow);
        if (session == null) return Unauthorized();
        var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == session.UserId);
        if (user == null) return Unauthorized();
        return Ok(new AuthResponse { Id = user.Id, Name = user.Name,
            Email = user.Email, Login = user.Login, Message = "Сесія активна." });
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        string? header = Request.Headers.Authorization.ToString();
        if (header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            string hash = SessionTokens.Hash(header.Substring(7).Trim());
            await _context.AuthSessions.Where(s => s.TokenHash == hash).ExecuteDeleteAsync();
        }
        return NoContent();
    }
}
