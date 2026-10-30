using InventoryManager.Web.Data;
using InventoryManager.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace InventoryManager.Web.Services;

public interface IAuthService
{
    Task<AppUser?> AuthenticateAsync(string username, string password);
    Task<AppUser?> GetByUsernameAsync(string username);
}

public class AuthService : IAuthService
{
    private readonly InventoryDbContext _context;
    private readonly IPasswordHasher _passwordHasher;

    public AuthService(InventoryDbContext context, IPasswordHasher passwordHasher)
    {
        _context = context;
        _passwordHasher = passwordHasher;
    }

    public async Task<AppUser?> AuthenticateAsync(string username, string password)
    {
        var normalized = username.Trim().ToLowerInvariant();
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Username.ToLower() == normalized);

        if (user == null)
        {
            return null;
        }

        if (!_passwordHasher.VerifyPassword(password, user.PasswordHash))
        {
            return null;
        }

        return user;
    }

    public async Task<AppUser?> GetByUsernameAsync(string username)
    {
        var normalized = username.Trim().ToLowerInvariant();
        return await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Username.ToLower() == normalized);
    }
}
