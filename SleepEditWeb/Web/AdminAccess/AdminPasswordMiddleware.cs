namespace SleepEditWeb.Web.AdminAccess;

public sealed class AdminPasswordMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var isProtectedPath = context.Request.Path.StartsWithSegments("/Admin") ||
                              context.Request.Path.StartsWithSegments("/ProtocolEditor");
        var isLoginPath = context.Request.Path.Equals(new PathString("/Admin/Login"));
        var isUnlocked = context.Session.GetString(AdminAccessConstants.SessionKey) ==
                         AdminAccessConstants.SessionUnlockedValue;

        if (!isProtectedPath || isLoginPath || isUnlocked)
        {
            await next(context);
            return;
        }

        const string loginUrl = "/Admin/Login";
        if (HttpMethods.IsGet(context.Request.Method))
        {
            var returnUrl = context.Request.PathBase + context.Request.Path + context.Request.QueryString;
            context.Response.Redirect(loginUrl + "?returnUrl=" + Uri.EscapeDataString(returnUrl));
            return;
        }

        context.Response.StatusCode = StatusCodes.Status303SeeOther;
        context.Response.Headers.Location = loginUrl;
    }
}
