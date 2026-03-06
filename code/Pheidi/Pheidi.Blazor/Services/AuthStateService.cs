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
            var result = await _storage.GetAsync<int>(SessionKey);
            if (result.Success)
            {
                CurrentUser = await _db.Users.FindAsync(result.Value);
            }
        }
        catch
        {
            // Storage not available (prerendering) — ignore
        }
    }

    public async Task SignInAsync(AppUser user)
    {
        CurrentUser = user;
        await _storage.SetAsync(SessionKey, user.Id);
        OnAuthStateChanged?.Invoke();
    }

    public async Task SignOutAsync()
    {
        CurrentUser = null;
        await _storage.DeleteAsync(SessionKey);
        OnAuthStateChanged?.Invoke();
    }
}
