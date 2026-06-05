// Services/Automation/HangfireAdminAuthorizationFilter.cs
using Hangfire.Dashboard;

namespace FootballPredictionGame.Services.Automation;

public class HangfireAdminAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();

        return httpContext.User.Identity?.IsAuthenticated == true &&
               httpContext.User.IsInRole("Admin");
    }
}