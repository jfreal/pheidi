using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Pheidi.Blazor.Data;
using Pheidi.Common.Models;

namespace Pheidi.Blazor.Services;

public partial class OtpAuthService
{
    private readonly AppDbContext _db;
    private readonly ILogger<OtpAuthService> _logger;
    private readonly IHostEnvironment _env;

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

    public async Task<bool> SendCodeAsync(string identifier)
    {
        identifier = NormalizeIdentifier(identifier);
        if (string.IsNullOrEmpty(identifier))
            return false;

        // Debug bypass: skip OTP generation for test phone
        if (_env.IsDevelopment() && identifier == DebugPhone)
        {
            _logger.LogInformation("Debug bypass: skipping OTP for {Phone}", DebugPhone);
            return true;
        }

        // Clean up expired OTP codes opportunistically
        var cutoff = DateTime.UtcNow.AddMinutes(-ExpiryMinutes * 2);
        var expired = await _db.OtpCodes
            .Where(c => c.ExpiresAt < cutoff)
            .ToListAsync();
        if (expired.Count > 0)
        {
            _db.OtpCodes.RemoveRange(expired);
        }

        // Rate limiting: max 5 codes per identifier per hour
        var recentCount = await _db.OtpCodes
            .CountAsync(c => c.Email == identifier && c.CreatedAt > DateTime.UtcNow.AddHours(-1));

        if (recentCount >= MaxAttemptsPerHour)
        {
            _logger.LogWarning("Rate limit exceeded for {Identifier}", identifier);
            return false;
        }

        var code = GenerateCode();
        var otpCode = new OtpCode
        {
            Email = identifier,
            Code = code,
            ExpiresAt = DateTime.UtcNow.AddMinutes(ExpiryMinutes)
        };

        _db.OtpCodes.Add(otpCode);
        await _db.SaveChangesAsync();

        // Dev mode: log code to console instead of sending email/SMS
        _logger.LogInformation("OTP code for {Identifier}: {Code}", identifier, code);

        return true;
    }

    public async Task<AppUser?> VerifyCodeAsync(string identifier, string code)
    {
        identifier = NormalizeIdentifier(identifier);
        code = code.Trim();

        // Debug bypass: code 999999 with test phone skips OTP validation
        if (_env.IsDevelopment() && identifier == DebugPhone && code == DebugCode)
        {
            _logger.LogInformation("Debug bypass: auto-login for {Phone}", DebugPhone);
            var debugUser = await _db.Users.FirstOrDefaultAsync(u => u.Email == identifier);
            if (debugUser == null)
            {
                debugUser = new AppUser { Email = identifier };
                _db.Users.Add(debugUser);
                await _db.SaveChangesAsync();
            }
            return debugUser;
        }

        var otpCode = await _db.OtpCodes
            .Where(c => c.Email == identifier && c.Code == code && !c.IsUsed && c.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(c => c.CreatedAt)
            .FirstOrDefaultAsync();

        if (otpCode == null)
            return null;

        otpCode.IsUsed = true;

        // Find or create user
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == identifier);
        if (user == null)
        {
            user = new AppUser { Email = identifier };
            _db.Users.Add(user);
        }

        await _db.SaveChangesAsync();
        return user;
    }

    /// <summary>
    /// Validates that input looks like an email or phone number.
    /// </summary>
    public static bool IsValidIdentifier(string input)
    {
        input = input.Trim();
        return IsPhoneNumber(input) || EmailRegex().IsMatch(input);
    }

    private static string NormalizeIdentifier(string input)
    {
        input = input.Trim();
        if (IsPhoneNumber(input))
            return DigitsOnly().Replace(input, "");
        return input.ToLowerInvariant();
    }

    private static bool IsPhoneNumber(string input)
    {
        var digits = DigitsOnly().Replace(input, "");
        return digits.Length >= 10 && digits.Length <= 15 && digits.All(char.IsDigit);
    }

    private static string GenerateCode()
    {
        return Random.Shared.Next(100000, 999999).ToString("D6");
    }

    [GeneratedRegex(@"[^\d]")]
    private static partial Regex DigitsOnly();

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex EmailRegex();
}
