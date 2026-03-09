using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Microsoft.EntityFrameworkCore;
using Pheidi.Blazor.Data;
using Pheidi.Common.Models;

namespace Pheidi.Blazor.Services;

public class AuthStateService
{
    private readonly ProtectedLocalStorage _storage;
    private readonly AppDbContext _db;

    private const string SessionKey = "pheidi_session";
    private const int SessionDays = 30;

    public AppUser? CurrentUser { get; private set; }
    public bool IsAuthenticated => CurrentUser != null;

    public event Action? OnAuthStateChanged;

    public AuthStateService(ProtectedLocalStorage storage, AppDbContext db)
    {
        _storage = storage;
        _db = db;
    }

    public async Task InitializeAsync()
    {
        try
        {
            var result = await _storage.GetAsync<SessionData>(SessionKey);
            if (result.Success && result.Value is { } session)
            {
                if (session.ExpiresAt > DateTime.UtcNow)
                {
                    CurrentUser = await _db.Users.FindAsync(session.UserId);
                    if (CurrentUser != null)
                    {
                        OnAuthStateChanged?.Invoke();
                        return;
                    }
                }

                // Expired or user not found — clean up
                await _storage.DeleteAsync(SessionKey);
            }
        }
        catch
        {
            // Storage not available (prerendering) or decryption failed — ignore
        }
    }

    public async Task SignInAsync(AppUser user)
    {
        CurrentUser = user;
        var session = new SessionData
        {
            UserId = user.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(SessionDays)
        };
        await _storage.SetAsync(SessionKey, session);
        OnAuthStateChanged?.Invoke();
    }

    public async Task SignOutAsync()
    {
        CurrentUser = null;
        await _storage.DeleteAsync(SessionKey);
        OnAuthStateChanged?.Invoke();
    }

    private class SessionData
    {
        public int UserId { get; set; }
        public DateTime ExpiresAt { get; set; }
    }
}
