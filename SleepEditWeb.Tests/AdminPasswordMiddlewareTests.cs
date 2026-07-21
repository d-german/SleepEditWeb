using Microsoft.AspNetCore.Http;
using SleepEditWeb.Tests.TestSupport;
using SleepEditWeb.Web.AdminAccess;

namespace SleepEditWeb.Tests;

[TestFixture]
public sealed class AdminPasswordMiddlewareTests
{
    [Test]
    public async Task InvokeAsync_AnonymousAdminGet_RedirectsToLoginWithReturnUrl()
    {
        var nextCalled = false;
        var middleware = new AdminPasswordMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });
        var context = CreateContext("GET", "/Admin/Medications", "?tab=protocol");

        await middleware.InvokeAsync(context);

        Assert.Multiple(() =>
        {
            Assert.That(nextCalled, Is.False);
            Assert.That(context.Response.StatusCode, Is.EqualTo(StatusCodes.Status302Found));
            Assert.That(
                context.Response.Headers.Location.ToString(),
                Is.EqualTo("/Admin/Login?returnUrl=%2FAdmin%2FMedications%3Ftab%3Dprotocol"));
        });
    }

    [Test]
    public async Task InvokeAsync_LoginRequest_AllowsAnonymousRequest()
    {
        var nextCalled = false;
        var middleware = new AdminPasswordMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });
        var context = CreateContext("GET", "/Admin/Login");

        await middleware.InvokeAsync(context);

        Assert.That(nextCalled, Is.True);
    }

    [Test]
    public async Task InvokeAsync_UnlockedAdminRequest_AllowsRequest()
    {
        var context = CreateContext("GET", "/Admin/Medications");
        context.Session.SetString(
            AdminAccessConstants.SessionKey,
            AdminAccessConstants.SessionUnlockedValue);
        var nextCalled = false;
        var middleware = new AdminPasswordMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        await middleware.InvokeAsync(context);

        Assert.That(nextCalled, Is.True);
    }

    [Test]
    public async Task InvokeAsync_AnonymousProtocolEditorRequest_RedirectsToAdminLogin()
    {
        var nextCalled = false;
        var middleware = new AdminPasswordMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });
        var context = CreateContext("GET", "/ProtocolEditor");

        await middleware.InvokeAsync(context);

        Assert.Multiple(() =>
        {
            Assert.That(nextCalled, Is.False);
            Assert.That(context.Response.StatusCode, Is.EqualTo(StatusCodes.Status302Found));
            Assert.That(
                context.Response.Headers.Location.ToString(),
                Is.EqualTo("/Admin/Login?returnUrl=%2FProtocolEditor"));
        });
    }

    [Test]
    public async Task InvokeAsync_AnonymousProtocolViewerRequest_RemainsPublic()
    {
        var nextCalled = false;
        var middleware = new AdminPasswordMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });
        var context = CreateContext("GET", "/ProtocolViewer");

        await middleware.InvokeAsync(context);

        Assert.That(nextCalled, Is.True);
    }

    [Test]
    public async Task InvokeAsync_AnonymousAdminPost_UsesSeeOtherAndDoesNotCallNext()
    {
        var nextCalled = false;
        var middleware = new AdminPasswordMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });
        var context = CreateContext("POST", "/Admin/Medications/ClearUserMeds");

        await middleware.InvokeAsync(context);

        Assert.Multiple(() =>
        {
            Assert.That(nextCalled, Is.False);
            Assert.That(context.Response.StatusCode, Is.EqualTo(StatusCodes.Status303SeeOther));
            Assert.That(context.Response.Headers.Location.ToString(), Is.EqualTo("/Admin/Login"));
        });
    }

    private static DefaultHttpContext CreateContext(string method, string path, string query = "")
    {
        var context = new DefaultHttpContext
        {
            Session = new DictionarySession()
        };
        context.Request.Method = method;
        context.Request.Path = path;
        context.Request.QueryString = new QueryString(query);
        return context;
    }
}
