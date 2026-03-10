using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Pheidi.Blazor.Components;
using Pheidi.Blazor.Data;
using Pheidi.Blazor.Services;
using Pheidi.Common.Models;
using Pheidi.Common.Engines;
using Pheidi.Common.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Persist data protection keys so ProtectedLocalStorage survives app restarts
var keysDir = Path.Combine(builder.Environment.ContentRootPath, ".keys");
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(keysDir))
    .SetApplicationName("Pheidi");

builder.Services.AddDbContextFactory<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")
        ?? "Data Source=pheidi.db"));

builder.Services.AddScoped<PlanStateService>();
builder.Services.AddScoped<PaceCalculator>();
builder.Services.AddScoped<OtpAuthService>();
builder.Services.AddScoped<AuthStateService>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<PlanRepository>();
builder.Services.AddScoped<WorkoutRepository>();
builder.Services.AddScoped<ICalExportService>();
builder.Services.AddScoped<RacePredictionService>();
builder.Services.AddSingleton<WorkoutLoggingService>();
builder.Services.AddSingleton<PdfExportService>();
builder.Services.AddSingleton<ScheduleFlexibilityEngine>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

// Calendar subscription endpoint (uses non-guessable share token)
app.MapGet("/api/calendar/{token}", async (string token, AppDbContext db, ICalExportService icalService) =>
{
    var user = await db.Users.FirstOrDefaultAsync(u => u.ShareToken == token);
    if (user == null) return Results.NotFound();

    var plan = await db.TrainingPlans
        .Include(p => p.RaceGoal)
        .Include(p => p.Weeks)
            .ThenInclude(w => w.Workouts)
        .Where(p => p.UserId == user.Id && p.Status == PlanStatus.Active)
        .FirstOrDefaultAsync();

    if (plan == null) return Results.NotFound();

    var ics = icalService.GenerateICalendar(plan);
    return Results.Text(ics, "text/calendar");
});

// Shared read-only plan view endpoint (uses non-guessable share token)
app.MapGet("/api/shared/{token}", async (string token, AppDbContext db) =>
{
    var plan = await db.TrainingPlans
        .Include(p => p.RaceGoal)
        .Include(p => p.Weeks)
            .ThenInclude(w => w.Workouts)
        .Where(p => p.ShareToken == token)
        .FirstOrDefaultAsync();

    if (plan == null) return Results.NotFound();

    return Results.Ok(new
    {
        plan.RaceGoal.Distance,
        plan.RaceGoal.RaceDate,
        plan.TotalWeeks,
        TotalMiles = plan.TotalPlannedMiles,
        Weeks = plan.Weeks.Select(w => new
        {
            w.WeekNumber,
            w.Phase,
            TotalDistance = w.TotalPlannedDistance,
            Workouts = w.Workouts.Select(wo => new
            {
                wo.Date,
                wo.Type,
                wo.Description,
                wo.TargetDistanceMiles
            })
        })
    });
});

// --- REST API endpoints ---

// Active plan for a user
app.MapGet("/api/plans/active", async (HttpContext http, AppDbContext db) =>
{
    var userId = http.Request.Headers["X-User-Id"].FirstOrDefault();
    if (!int.TryParse(userId, out var uid)) return Results.Unauthorized();

    var plan = await db.TrainingPlans
        .Include(p => p.RaceGoal)
        .Include(p => p.Weeks)
            .ThenInclude(w => w.Workouts)
        .Where(p => p.UserId == uid && p.Status == PlanStatus.Active)
        .FirstOrDefaultAsync();

    return plan == null ? Results.NotFound() : Results.Ok(plan);
});

// Complete a workout
app.MapPost("/api/workouts/{id:int}/complete", async (int id, AppDbContext db) =>
{
    var workout = await db.ScheduledWorkouts.FindAsync(id);
    if (workout == null) return Results.NotFound();

    workout.Status = WorkoutStatus.Completed;
    await db.SaveChangesAsync();
    return Results.Ok(workout);
});

// Get/update user profile
app.MapGet("/api/profile/{userId:int}", async (int userId, AppDbContext db) =>
{
    var profile = await db.UserProfiles.FirstOrDefaultAsync(p => p.UserId == userId);
    return profile == null ? Results.NotFound() : Results.Ok(profile);
});

app.MapPut("/api/profile/{userId:int}", async (int userId, UserProfile updated, AppDbContext db) =>
{
    var profile = await db.UserProfiles.FirstOrDefaultAsync(p => p.UserId == userId);
    if (profile == null) return Results.NotFound();

    profile.ExperienceLevel = updated.ExperienceLevel;
    profile.PacePreference = updated.PacePreference;
    profile.VdotValue = updated.VdotValue;
    profile.UseMiles = updated.UseMiles;
    profile.AvailableDays = updated.AvailableDays;
    profile.PreferredLongRunDay = updated.PreferredLongRunDay;
    profile.CurrentWeeklyMileage = updated.CurrentWeeklyMileage;
    profile.RunningExperienceMonths = updated.RunningExperienceMonths;

    await db.SaveChangesAsync();
    return Results.Ok(profile);
});

// Report injury
app.MapPost("/api/injuries", async (InjuryReport report, AppDbContext db) =>
{
    db.InjuryReports.Add(report);
    await db.SaveChangesAsync();
    return Results.Created($"/api/injuries/{report.Id}", report);
});

// Get active injury for a user
app.MapGet("/api/injuries/active/{userId:int}", async (int userId, AppDbContext db) =>
{
    var injury = await db.InjuryReports
        .Include(r => r.PainHistory)
        .Where(r => r.UserId == userId && r.Status == InjuryStatus.Active)
        .FirstOrDefaultAsync();

    return injury == null ? Results.NotFound() : Results.Ok(injury);
});

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
