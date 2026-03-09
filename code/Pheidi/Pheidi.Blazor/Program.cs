using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Pheidi.Blazor.Components;
using Pheidi.Blazor.Data;
using Pheidi.Blazor.Services;
using Pheidi.Common.Models;
using Pheidi.Common.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Persist data protection keys so ProtectedLocalStorage survives app restarts
var keysDir = Path.Combine(builder.Environment.ContentRootPath, ".keys");
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(keysDir))
    .SetApplicationName("Pheidi");

builder.Services.AddDbContext<AppDbContext>(options =>
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

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
