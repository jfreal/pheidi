using Microsoft.EntityFrameworkCore;
using Pheidi.Blazor.Data;
using Pheidi.Common.Models;

namespace Pheidi.Blazor.Services;

public class UserService
{
    private readonly AppDbContext _db;

    public UserService(AppDbContext db) => _db = db;

    public async Task<UserProfile?> GetProfileAsync(int userId)
    {
        return await _db.UserProfiles.FirstOrDefaultAsync(p => p.UserId == userId);
    }

    public async Task<UserProfile> GetOrCreateProfileAsync(int userId)
    {
        var profile = await GetProfileAsync(userId);
        if (profile != null) return profile;

        profile = new UserProfile { UserId = userId };
        _db.UserProfiles.Add(profile);
        await _db.SaveChangesAsync();
        return profile;
    }

    public async Task SaveProfileAsync(UserProfile profile)
    {
        var existing = await _db.UserProfiles.FindAsync(profile.Id);
        if (existing != null)
        {
            _db.Entry(existing).CurrentValues.SetValues(profile);
        }
        else
        {
            _db.UserProfiles.Add(profile);
        }
        await _db.SaveChangesAsync();
    }

    public async Task<bool> IsPaidUserAsync(int userId)
    {
        var user = await _db.Users.FindAsync(userId);
        return user?.IsPaidUser ?? false;
    }
}
