using Microsoft.EntityFrameworkCore;
using Pheidi.Blazor.Data;
using Pheidi.Common.Models;

namespace Pheidi.Blazor.Services;

public class UserService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public UserService(IDbContextFactory<AppDbContext> dbFactory) => _dbFactory = dbFactory;

    public async Task<UserProfile?> GetProfileAsync(int userId)
    {
        using var db = await _dbFactory.CreateDbContextAsync();
        return await db.UserProfiles.FirstOrDefaultAsync(p => p.UserId == userId);
    }

    public async Task<UserProfile> GetOrCreateProfileAsync(int userId)
    {
        using var db = await _dbFactory.CreateDbContextAsync();
        var profile = await db.UserProfiles.FirstOrDefaultAsync(p => p.UserId == userId);
        if (profile != null) return profile;

        profile = new UserProfile { UserId = userId };
        db.UserProfiles.Add(profile);
        await db.SaveChangesAsync();
        return profile;
    }

    public async Task SaveProfileAsync(UserProfile profile)
    {
        using var db = await _dbFactory.CreateDbContextAsync();
        var existing = await db.UserProfiles.FindAsync(profile.Id);
        if (existing != null)
        {
            db.Entry(existing).CurrentValues.SetValues(profile);
        }
        else
        {
            db.UserProfiles.Add(profile);
        }
        await db.SaveChangesAsync();
    }

    public async Task<bool> IsPaidUserAsync(int userId)
    {
        using var db = await _dbFactory.CreateDbContextAsync();
        var user = await db.Users.FindAsync(userId);
        return user?.IsPaidUser ?? false;
    }
}
