using FootballPredictionGame.Data;
using FootballPredictionGame.Models;
using FootballPredictionGame.Services;
using FootballPredictionGame.Services.Automation;
using Hangfire;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Hangfire.SqlServer;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

// Database
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Services
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IFileUploadService, FileUploadService>();
builder.Services.AddScoped<IAutomationGuardService, AutomationGuardService>();
builder.Services.AddScoped<ILoginTrackingService, LoginTrackingService>();
builder.Services.AddScoped<AutomationHealthCheckJob>();
builder.Services.AddScoped<SessionCleanupJob>();
builder.Services.AddScoped<FixtureStatusAutomationJob>();
builder.Services.AddScoped<IResultProcessingService, ResultProcessingService>();
builder.Services.AddScoped<ResultProcessingAutomationJob>();
builder.Services.AddScoped<IPredictionReminderEmailService, PredictionReminderEmailService>();
builder.Services.AddScoped<PredictionReminderAutomationJob>();
builder.Services.AddScoped<IWeeklyPerformanceEmailService, WeeklyPerformanceEmailService>();
builder.Services.AddScoped<WeeklyPerformanceEmailAutomationJob>();

// Identity - ONLY ONE Identity registration
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.User.RequireUniqueEmail = true;

    options.Password.RequiredLength = 8;
    options.Password.RequireDigit = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireNonAlphanumeric = true;

    options.SignIn.RequireConfirmedAccount = false;
    options.SignIn.RequireConfirmedEmail = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// Cookie settings
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
});

builder.Services.AddHangfire(configuration =>
{
    configuration
        .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
        .UseSimpleAssemblyNameTypeSerializer()
        .UseRecommendedSerializerSettings()
        .UseSqlServerStorage(
            builder.Configuration.GetConnectionString("DefaultConnection"),
            new SqlServerStorageOptions
            {
                CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
                SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
                QueuePollInterval = TimeSpan.FromSeconds(15),
                UseRecommendedIsolationLevel = true,
                DisableGlobalLocks = true
            });
});

builder.Services.AddHangfireServer(options =>
{
    options.ServerName = "FootballPredictionGame-Automation";
    options.WorkerCount = 1;
});
builder.Services.AddDistributedMemoryCache();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.Name = ".Transtec360Football.Session";
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

// Seed roles/admin/data
using (var scope = app.Services.CreateScope())
{
    await DbInitializer.InitializeAsync(scope.ServiceProvider);
}

app.UseHangfireDashboard("/admin/jobs", new DashboardOptions
{
    Authorization = new[] { new HangfireAdminAuthorizationFilter() },
    DashboardTitle = "Transtec 360° Football Prediction Automation Jobs"
});
RecurringJob.AddOrUpdate<AutomationHealthCheckJob>(
    recurringJobId: "automation-health-check",
    methodCall: job => job.RunAsync(),
    cronExpression: "*/10 * * * *"
);
RecurringJob.AddOrUpdate<SessionCleanupJob>(
    recurringJobId: "session-cleanup-job",
    methodCall: job => job.RunAsync(),
    cronExpression: "*/5 * * * *"
);
RecurringJob.AddOrUpdate<FixtureStatusAutomationJob>(
    recurringJobId: "fixture-status-automation-job",
    methodCall: job => job.RunAsync(),
    cronExpression: "*/5 * * * *"
);
RecurringJob.AddOrUpdate<ResultProcessingAutomationJob>(
    recurringJobId: "result-processing-automation-job",
    methodCall: job => job.RunAsync(),
    cronExpression: "*/5 * * * *"
);
RecurringJob.AddOrUpdate<PredictionReminderAutomationJob>(
    recurringJobId: "prediction-reminder-automation-job",
    methodCall: job => job.RunAsync(),
    cronExpression: "*/15 * * * *"
);
RecurringJob.AddOrUpdate<WeeklyPerformanceEmailAutomationJob>(
    recurringJobId: "weekly-performance-email-automation-job",
    methodCall: job => job.RunAsync(),
    cronExpression: "*/30 * * * *"
);

// Area route
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");

//// This makes https://fifa26.transtec360.com open Football/Index
//app.MapControllerRoute(
//    name: "root",
//    pattern: "",
//    defaults: new { controller = "Football", action = "Index" });

//// This makes https://fifa26.transtec360.com/t360football also open Football/Index
//app.MapControllerRoute(
//    name: "t360football",
//    pattern: "t360football",
//    defaults: new { controller = "Football", action = "Index" });

// Default route for all other pages
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();