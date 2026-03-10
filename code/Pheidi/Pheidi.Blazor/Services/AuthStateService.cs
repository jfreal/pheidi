using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Microsoft.EntityFrameworkCore;
using Pheidi.Blazor.Data;
using Pheidi.Common.Models;

namespace Pheidi.Blazor.Services;

public class AuthStateService
{
    private readonly ProtectedLocalStorage _storage;
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    private const string SessionKey = "pheidi_session";
    private const int SessionDays = 30;

    public AppUser? CurrentUser { get; private set; }
    public bool IsAuthenticated => CurrentUser != null;

    public event Func<Task>? OnAuthStateChanged;

    public AuthStateService(ProtectedLocalStorage storage, IDbContextFactory<AppDbContext> dbFactory)
    {
        _storage = storage;
        _dbFactory = dbFactory;
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
                    using var db = await _dbFactory.CreateDbContextAsync();
                    CurrentUser = await db.Users.FindAsync(session.UserId);
                    if (CurrentUser != null)
                    {
                        await NotifyAuthStateChanged();
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
        await NotifyAuthStateChanged();
    }

    public async Task SignOutAsync()
    {
        CurrentUser = null;
        await _storage.DeleteAsync(SessionKey);
        await NotifyAuthStateChanged();
    }

    private async Task NotifyAuthStateChanged()
    {
        if (OnAuthStateChanged != null)
        {
            foreach (var handler in OnAuthStateChanged.GetInvocationList().Cast<Func<Task>>())
                await handler();
        }
    }

    private class SessionData
    {
        public int UserId { get; set; }
        public DateTime ExpiresAt { get; set; }
    }
}
