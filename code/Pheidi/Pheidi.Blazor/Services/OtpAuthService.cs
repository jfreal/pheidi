using Microsoft.EntityFrameworkCore;
using Pheidi.Blazor.Data;
using Pheidi.Common.Models;

namespace Pheidi.Blazor.Services;

public class OtpAuthService
{
    private readonly AppDbContext _db;
    private readonly ILogger<OtpAuthService> _logger;
    private readonly IHostEnvironment _env;

    private const int CodeLength = 6;
    private const int ExpiryMinutes = 10;
    private const int MaxAttemptsPerHour = 5;
    private const string DebugPhone = "8607782522";
    private const string DebugCode = "999999";

    public OtpAuthService(AppDbContext db, ILogger<OtpAuthService> logger, IHostEnvironment env)
    {
        _db = db;
        _logger = logger;
        _env = env;
    }

    public async Task<bool> SendCodeAsync(string email)
    {
        email = email.Trim().ToLowerInvariant();

        // Debug bypass: skip OTP generation for test phone
        if (_env.IsDevelopment() && email == DebugPhone)
        {
            _logger.LogInformation("Debug bypass: skipping OTP for {Phone}", DebugPhone);
            return true;
        }

        // Rate limiting: max 5 codes per email per hour
        var recentCount = await _db.OtpCodes
            .CountAsync(c => c.Email == email && c.CreatedAt > DateTime.UtcNow.AddHours(-1));

        if (recentCount >= MaxAttemptsPerHour)
        {
            _logger.LogWarning("Rate limit exceeded for {Email}", email);
            return false;
        }

        var code = GenerateCode();
        var otpCode = new OtpCode
        {
            Email = email,
            Code = code,
            ExpiresAt = DateTime.UtcNow.AddMinutes(ExpiryMinutes)
        };

        _db.OtpCodes.Add(otpCode);
        await _db.SaveChangesAsync();

        // Dev mode: log code to console instead of sending email
        _logger.LogInformation("OTP code for {Email}: {Code}", email, code);

        return true;
    }

    public async Task<AppUser?> VerifyCodeAsync(string email, string code)
    {
        email = email.Trim().ToLowerInvariant();
        code = code.Trim();

        // Debug bypass: code 999999 with test phone skips OTP validation
        if (_env.IsDevelopment() && email == DebugPhone && code == DebugCode)
        {
            _logger.LogInformation("Debug bypass: auto-login for {Phone}", DebugPhone);
            var debugUser = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (debugUser == null)
            {
                debugUser = new AppUser { Email = email };
                _db.Users.Add(debugUser);
                await _db.SaveChangesAsync();
            }
            return debugUser;
        }

        var otpCode = await _db.OtpCodes
            .Where(c => c.Email == email && c.Code == code && !c.IsUsed && c.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(c => c.CreatedAt)
            .FirstOrDefaultAsync();

        if (otpCode == null)
            return null;

        otpCode.IsUsed = true;

        // Find or create user
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user == null)
        {
            user = new AppUser { Email = email };
            _db.Users.Add(user);
        }

        await _db.SaveChangesAsync();
        return user;
    }

    private static string GenerateCode()
    {
        return Random.Shared.Next(0, 999999).ToString("D6");
    }
}
