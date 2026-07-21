# Admin Password Gate Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Protect every `/Admin` route with the hard-coded password `sleep123`, a 30-minute session unlock, and explicit logout.

**Architecture:** A focused middleware guards the complete `/Admin` and `/ProtocolEditor` path boundaries and allows only `/Admin/Login` through anonymously. `AdminController` owns password validation and session changes; existing Admin actions become secret-free because authorization is enforced before MVC executes.

**Tech Stack:** ASP.NET Core 8 MVC, ASP.NET Core Session, Razor, NUnit/Moq, Playwright Test 1.61.

## Global Constraints

- The password is exactly `sleep123` and is hard-coded server-side.
- The password must not appear in links, HTML, query strings, redirects, or logs.
- A successful login unlocks Admin for the existing 30-minute idle session.
- Logout removes access immediately.
- Every current and future `/Admin` and `/ProtocolEditor` endpoint is protected except `GET` and `POST /Admin/Login`.
- Login, logout, import, reseed, and clear operations validate anti-forgery tokens.
- Protocol Editor implementation is unchanged; its page and API endpoints use the same Admin session gate. The public `/ProtocolViewer` remains accessible.

---

### Task 1: Admin path middleware

**Files:**
- Create: `SleepEditWeb/Web/AdminAccess/AdminAccessConstants.cs`
- Create: `SleepEditWeb/Web/AdminAccess/AdminPasswordMiddleware.cs`
- Create: `SleepEditWeb.Tests/AdminPasswordMiddlewareTests.cs`
- Create: `SleepEditWeb.Tests/TestSupport/DictionarySession.cs`
- Modify: `SleepEditWeb/Program.cs`

**Interfaces:**
- Produces: `AdminAccessConstants.Password`, `AdminAccessConstants.SessionKey`, `AdminAccessConstants.SessionUnlockedValue`.
- Produces: `AdminPasswordMiddleware.InvokeAsync(HttpContext context)`.
- Consumes: ASP.NET Core `ISession`, registered before the middleware through the existing `UseSession()` call.

- [ ] **Step 1: Write the failing middleware tests**

Create `AdminPasswordMiddlewareTests.cs` with a dictionary-backed `ISession` test double and tests equivalent to:

```csharp
[Test]
public async Task InvokeAsync_AnonymousAdminGet_RedirectsToLoginWithReturnUrl()
{
    var nextCalled = false;
    var middleware = new AdminPasswordMiddleware(_ => { nextCalled = true; return Task.CompletedTask; });
    var context = CreateContext("GET", "/Admin/Medications", "?tab=protocol");

    await middleware.InvokeAsync(context);

    Assert.Multiple(() =>
    {
        Assert.That(nextCalled, Is.False);
        Assert.That(context.Response.StatusCode, Is.EqualTo(StatusCodes.Status302Found));
        Assert.That(context.Response.Headers.Location.ToString(),
            Is.EqualTo("/Admin/Login?returnUrl=%2FAdmin%2FMedications%3Ftab%3Dprotocol"));
    });
}

[Test]
public async Task InvokeAsync_LoginRequest_AllowsAnonymousRequest()
{
    var nextCalled = false;
    var middleware = new AdminPasswordMiddleware(_ => { nextCalled = true; return Task.CompletedTask; });
    var context = CreateContext("GET", "/Admin/Login");

    await middleware.InvokeAsync(context);

    Assert.That(nextCalled, Is.True);
}

[Test]
public async Task InvokeAsync_UnlockedAdminRequest_AllowsRequest()
{
    var context = CreateContext("GET", "/Admin/Medications");
    context.Session.SetString(AdminAccessConstants.SessionKey, AdminAccessConstants.SessionUnlockedValue);
    var nextCalled = false;
    var middleware = new AdminPasswordMiddleware(_ => { nextCalled = true; return Task.CompletedTask; });

    await middleware.InvokeAsync(context);

    Assert.That(nextCalled, Is.True);
}

[Test]
public async Task InvokeAsync_AnonymousProtocolEditorRequest_RedirectsToAdminLogin()
{
    var nextCalled = false;
    var middleware = new AdminPasswordMiddleware(_ => { nextCalled = true; return Task.CompletedTask; });
    var context = CreateContext("GET", "/ProtocolEditor");

    await middleware.InvokeAsync(context);

    Assert.Multiple(() =>
    {
        Assert.That(nextCalled, Is.False);
        Assert.That(context.Response.StatusCode, Is.EqualTo(StatusCodes.Status302Found));
        Assert.That(context.Response.Headers.Location.ToString(),
            Is.EqualTo("/Admin/Login?returnUrl=%2FProtocolEditor"));
    });
}

[Test]
public async Task InvokeAsync_AnonymousProtocolViewerRequest_RemainsPublic()
{
    var nextCalled = false;
    var middleware = new AdminPasswordMiddleware(_ => { nextCalled = true; return Task.CompletedTask; });
    var context = CreateContext("GET", "/ProtocolViewer");

    await middleware.InvokeAsync(context);

    Assert.That(nextCalled, Is.True);
}

[Test]
public async Task InvokeAsync_AnonymousAdminPost_UsesSeeOtherAndDoesNotCallNext()
{
    var nextCalled = false;
    var middleware = new AdminPasswordMiddleware(_ => { nextCalled = true; return Task.CompletedTask; });
    var context = CreateContext("POST", "/Admin/Medications/ClearUserMeds");

    await middleware.InvokeAsync(context);

    Assert.Multiple(() =>
    {
        Assert.That(nextCalled, Is.False);
        Assert.That(context.Response.StatusCode, Is.EqualTo(StatusCodes.Status303SeeOther));
        Assert.That(context.Response.Headers.Location.ToString(), Is.EqualTo("/Admin/Login"));
    });
}
```

Create the shared test session:

```csharp
using Microsoft.AspNetCore.Http;

namespace SleepEditWeb.Tests.TestSupport;

internal sealed class DictionarySession : ISession
{
    private readonly Dictionary<string, byte[]> _values = [];

    public bool IsAvailable => true;
    public string Id { get; } = Guid.NewGuid().ToString("N");
    public IEnumerable<string> Keys => _values.Keys;

    public void Clear() => _values.Clear();
    public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task LoadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    public void Remove(string key) => _values.Remove(key);
    public void Set(string key, byte[] value) => _values[key] = value;
    public bool TryGetValue(string key, out byte[]? value) => _values.TryGetValue(key, out value);
}
```

Use this helper in `AdminPasswordMiddlewareTests`:

```csharp
private static DefaultHttpContext CreateContext(string method, string path, string query = "")
{
    var context = new DefaultHttpContext { Session = new DictionarySession() };
    context.Request.Method = method;
    context.Request.Path = path;
    context.Request.QueryString = new QueryString(query);
    return context;
}
```

- [ ] **Step 2: Run the middleware tests and verify RED**

Run:

```powershell
dotnet test SleepEditWeb.Tests/SleepEditWeb.Tests.csproj --no-restore --filter AdminPasswordMiddlewareTests
```

Expected: compilation fails because `AdminPasswordMiddleware` and `AdminAccessConstants` do not exist.

- [ ] **Step 3: Add the constants and minimal middleware**

Implement:

```csharp
namespace SleepEditWeb.Web.AdminAccess;

public static class AdminAccessConstants
{
    public const string Password = "sleep123";
    public const string SessionKey = "AdminAccess.Unlocked";
    public const string SessionUnlockedValue = "true";
}
```

```csharp
namespace SleepEditWeb.Web.AdminAccess;

public sealed class AdminPasswordMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var isProtectedPath = context.Request.Path.StartsWithSegments("/Admin") ||
                              context.Request.Path.StartsWithSegments("/ProtocolEditor");
        if (!isProtectedPath ||
            context.Request.Path.Equals(new PathString("/Admin/Login")) ||
            context.Session.GetString(AdminAccessConstants.SessionKey) == AdminAccessConstants.SessionUnlockedValue)
        {
            await next(context);
            return;
        }

        var loginUrl = "/Admin/Login";
        if (HttpMethods.IsGet(context.Request.Method))
        {
            var returnUrl = context.Request.PathBase + context.Request.Path + context.Request.QueryString;
            loginUrl += "?returnUrl=" + Uri.EscapeDataString(returnUrl);
            context.Response.Redirect(loginUrl);
            return;
        }

        context.Response.StatusCode = StatusCodes.Status303SeeOther;
        context.Response.Headers.Location = loginUrl;
    }
}
```

Register `app.UseMiddleware<AdminPasswordMiddleware>();` immediately after `app.UseSession();` in `Program.cs`, and add `using SleepEditWeb.Web.AdminAccess;`.

- [ ] **Step 4: Run the middleware tests and verify GREEN**

Run the filtered command from Step 2.

Expected: all six middleware tests pass.

- [ ] **Step 5: Commit the middleware boundary**

```powershell
git add SleepEditWeb/Web/AdminAccess SleepEditWeb.Tests/AdminPasswordMiddlewareTests.cs SleepEditWeb.Tests/TestSupport/DictionarySession.cs SleepEditWeb/Program.cs
git commit -m "Protect Admin routes with session middleware"
```

---

### Task 2: Login, logout, and secret-free Admin actions

**Files:**
- Create: `SleepEditWeb/Models/AdminLoginViewModel.cs`
- Create: `SleepEditWeb.Tests/AdminControllerTests.cs`
- Modify: `SleepEditWeb/Controllers/AdminController.cs`

**Interfaces:**
- Consumes: `AdminAccessConstants` from Task 1.
- Produces: `AdminController.Login(string? returnUrl)`, `AdminController.Login(AdminLoginViewModel model)`, and `AdminController.Logout()`.
- Produces: secret-free routes for `Index`, `Export`, `Import`, `Reseed`, and `ClearUserMeds`.

- [ ] **Step 1: Write failing controller tests**

Create tests with a mocked `IMedicationRepository`, `NullLogger<AdminController>`, a `DefaultHttpContext` using `DictionarySession`, and a mocked `IUrlHelper`. The setup is:

```csharp
private Mock<IMedicationRepository> _repository = null!;
private Mock<IUrlHelper> _url = null!;
private DictionarySession _session = null!;
private AdminController _controller = null!;

[SetUp]
public void SetUp()
{
    _repository = new Mock<IMedicationRepository>();
    _url = new Mock<IUrlHelper>();
    _session = new DictionarySession();
    _controller = new AdminController(
        _repository.Object,
        NullLogger<AdminController>.Instance)
    {
        ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { Session = _session }
        },
        Url = _url.Object
    };
}
```

```csharp
[Test]
public void Login_WrongPassword_DoesNotUnlockAndClearsSubmittedPassword()
{
    var model = new AdminLoginViewModel { Password = "wrong", ReturnUrl = "/Admin/Medications" };

    var result = _controller.Login(model);

    var view = result as ViewResult;
    var returned = view?.Model as AdminLoginViewModel;
    Assert.Multiple(() =>
    {
        Assert.That(returned?.ErrorMessage, Is.EqualTo("Incorrect password."));
        Assert.That(returned?.Password, Is.Empty);
        Assert.That(_session.GetString(AdminAccessConstants.SessionKey), Is.Null);
    });
}

[Test]
public void Login_CorrectPassword_UnlocksAndUsesLocalReturnUrl()
{
    _url.Setup(x => x.IsLocalUrl("/Admin/Medications?tab=protocol")).Returns(true);
    var model = new AdminLoginViewModel
    {
        Password = "sleep123",
        ReturnUrl = "/Admin/Medications?tab=protocol"
    };

    var result = _controller.Login(model);

    Assert.Multiple(() =>
    {
        Assert.That(result, Is.TypeOf<LocalRedirectResult>());
        Assert.That(((LocalRedirectResult)result).Url, Is.EqualTo(model.ReturnUrl));
        Assert.That(_session.GetString(AdminAccessConstants.SessionKey),
            Is.EqualTo(AdminAccessConstants.SessionUnlockedValue));
    });
}

[Test]
public void Login_ExternalReturnUrl_UsesDashboard()
{
    _url.Setup(x => x.IsLocalUrl("https://example.com")).Returns(false);

    var result = _controller.Login(new AdminLoginViewModel
    {
        Password = "sleep123",
        ReturnUrl = "https://example.com"
    });

    Assert.Multiple(() =>
    {
        Assert.That(result, Is.TypeOf<RedirectToActionResult>());
        Assert.That(((RedirectToActionResult)result).ActionName,
            Is.EqualTo(nameof(AdminController.Index)));
    });
}

[Test]
public void Logout_RemovesUnlockFlagAndRedirectsToLogin()
{
    _session.SetString(AdminAccessConstants.SessionKey, AdminAccessConstants.SessionUnlockedValue);

    var result = _controller.Logout();

    Assert.Multiple(() =>
    {
        Assert.That(_session.GetString(AdminAccessConstants.SessionKey), Is.Null);
        Assert.That(result, Is.TypeOf<RedirectToActionResult>());
        Assert.That(((RedirectToActionResult)result).ActionName, Is.EqualTo(nameof(AdminController.Login)));
    });
}
```

Also add reflection assertions that existing action parameters no longer contain `secretKey`, and their route templates are `""`, `"Export"`, `"Import"`, `"Reseed"`, and `"ClearUserMeds"`.

- [ ] **Step 2: Run controller tests and verify RED**

```powershell
dotnet test SleepEditWeb.Tests/SleepEditWeb.Tests.csproj --no-restore --filter AdminControllerTests
```

Expected: compilation failures for `AdminLoginViewModel`, login/logout actions, and secret-free signatures.

- [ ] **Step 3: Implement the login model and controller behavior**

Create:

```csharp
using System.ComponentModel.DataAnnotations;

namespace SleepEditWeb.Models;

public sealed class AdminLoginViewModel
{
    [Required]
    public string Password { get; set; } = string.Empty;
    public string? ReturnUrl { get; set; }
    public string? ErrorMessage { get; set; }
}
```

In `AdminController`, delete `SecretKey`, `IsValidKey`, all key checks, all `ViewBag.SecretKey` assignments, and all `secretKey` parameters. Add:

```csharp
[HttpGet("/Admin/Login")]
public IActionResult Login(string? returnUrl = null) =>
    View("Login", new AdminLoginViewModel { ReturnUrl = returnUrl });

[HttpPost("/Admin/Login")]
[ValidateAntiForgeryToken]
public IActionResult Login(AdminLoginViewModel model)
{
    if (!string.Equals(model.Password, AdminAccessConstants.Password, StringComparison.Ordinal))
    {
        _logger.LogWarning("Admin login denied due to an incorrect password.");
        model.Password = string.Empty;
        model.ErrorMessage = "Incorrect password.";
        return View("Login", model);
    }

    HttpContext.Session.SetString(
        AdminAccessConstants.SessionKey,
        AdminAccessConstants.SessionUnlockedValue);
    _logger.LogInformation("Admin session unlocked.");

    if (Url.IsLocalUrl(model.ReturnUrl))
    {
        return LocalRedirect(model.ReturnUrl!);
    }

    return RedirectToAction(nameof(Index));
}

[HttpPost("/Admin/Logout")]
[ValidateAntiForgeryToken]
public IActionResult Logout()
{
    HttpContext.Session.Remove(AdminAccessConstants.SessionKey);
    _logger.LogInformation("Admin session locked.");
    return RedirectToAction(nameof(Login));
}
```

Use `[HttpGet("")]`, `[HttpGet("Export")]`, `[HttpPost("Import")]`, `[HttpPost("Reseed")]`, and `[HttpPost("ClearUserMeds")]`. Add `[ValidateAntiForgeryToken]` to `Import`. All redirects return `RedirectToAction(nameof(Index))` without route values.

- [ ] **Step 4: Run controller and middleware tests and verify GREEN**

```powershell
dotnet test SleepEditWeb.Tests/SleepEditWeb.Tests.csproj --no-restore --filter "AdminControllerTests|AdminPasswordMiddlewareTests"
```

Expected: all new Admin tests pass.

- [ ] **Step 5: Commit controller behavior**

```powershell
git add SleepEditWeb/Models/AdminLoginViewModel.cs SleepEditWeb/Controllers/AdminController.cs SleepEditWeb.Tests/AdminControllerTests.cs
git commit -m "Add Admin password login session"
```

---

### Task 3: Password UI, navigation, and browser regression

**Files:**
- Create: `SleepEditWeb/Views/Admin/Login.cshtml`
- Create: `SleepEditWeb/e2e/tests/admin-password-gate.spec.ts`
- Modify: `SleepEditWeb/Views/Admin/Medications.cshtml`
- Modify: `SleepEditWeb/Views/Shared/_Layout.cshtml`
- Modify: `SleepEditWeb.Tests/ProtocolEditorUiContractsTests.cs`

**Interfaces:**
- Consumes: login/logout routes and `AdminLoginViewModel` from Task 2.
- Produces: password form labelled `Admin password`, error text `Incorrect password.`, and `Log out` button.

- [ ] **Step 1: Write the failing Playwright and markup contract tests**

Add `admin-password-gate.spec.ts`:

```typescript
import { test, expect } from '@playwright/test';

test('Admin requires password, persists the session, and locks again on logout', async ({ page }) => {
  await page.goto('/Admin/Medications');
  await expect(page).toHaveURL(/\/Admin\/Login\?returnUrl=/);
  await expect(page.getByRole('heading', { name: 'Admin Access' })).toBeVisible();

  await page.getByLabel('Admin password').fill('wrong');
  await page.getByRole('button', { name: 'Unlock Admin' }).click();
  await expect(page.getByRole('alert')).toHaveText('Incorrect password.');
  await expect(page.getByLabel('Admin password')).toHaveValue('');

  await page.getByLabel('Admin password').fill('sleep123');
  await page.getByRole('button', { name: 'Unlock Admin' }).click();
  await expect(page).toHaveURL(/\/Admin\/Medications/);
  await expect(page.getByRole('heading', { name: 'Admin Dashboard' })).toBeVisible();

  await page.goto('/ProtocolEditor');
  await expect(page).not.toHaveURL(/\/Admin\/Login/);

  const exportResponse = await page.request.get('/Admin/Medications/Export');
  expect(exportResponse.status()).toBe(200);
  expect(exportResponse.headers()['content-type']).toContain('application/json');

  await page.goto('/Admin/Medications');
  await page.getByRole('button', { name: 'Log out' }).click();
  await expect(page).toHaveURL(/\/Admin\/Login/);
  await page.goto('/Admin/Medications');
  await expect(page).toHaveURL(/\/Admin\/Login\?returnUrl=/);
  await page.goto('/ProtocolEditor');
  await expect(page).toHaveURL(/\/Admin\/Login\?returnUrl=/);
});

test('Admin password never appears in navigation or page markup', async ({ page }) => {
  await page.goto('/SleepNoteEditor');
  const adminLink = page.getByRole('link', { name: 'Admin' });
  await expect(adminLink).toHaveAttribute('href', '/Admin/Medications');
  await expect(page.locator('html')).not.toContainText('sleep123');
});
```

Extend the existing UI contract test to assert `_Layout.cshtml` and `Medications.cshtml` do not contain `medAdmin2025xK9!`, `asp-route-secretKey`, or `new { secretKey`, and that `Login.cshtml` contains `type="password"` and anti-forgery form markup.

- [ ] **Step 2: Run the new tests and verify RED**

```powershell
dotnet test SleepEditWeb.Tests/SleepEditWeb.Tests.csproj --no-restore --filter ProtocolEditorUiContractsTests
cd SleepEditWeb
npx playwright test e2e/tests/admin-password-gate.spec.ts
```

Expected: contract assertions and browser navigation fail because the old secret URLs and no login view are present.

- [ ] **Step 3: Implement the login and secret-free Admin views**

`Login.cshtml` must use the normal layout and contain:

```cshtml
@model SleepEditWeb.Models.AdminLoginViewModel
@{
    ViewData["Title"] = "Admin Access";
}

<div class="row justify-content-center">
    <div class="col-12 col-md-6 col-lg-4">
        <div class="dashboard-card">
            <div class="dashboard-card-header">Admin Access</div>
            <div class="dashboard-card-body">
                @if (!string.IsNullOrEmpty(Model.ErrorMessage))
                {
                    <div class="alert alert-danger" role="alert">@Model.ErrorMessage</div>
                }
                <form asp-action="Login" asp-controller="Admin" method="post">
                    @Html.AntiForgeryToken()
                    <input asp-for="ReturnUrl" type="hidden" />
                    <label asp-for="Password" class="form-label">Admin password</label>
                    <input asp-for="Password" type="password" class="form-control" autocomplete="current-password" autofocus />
                    <button type="submit" class="btn btn-primary mt-3 w-100">Unlock Admin</button>
                </form>
            </div>
        </div>
    </div>
</div>
```

In `_Layout.cshtml`, generate the Admin href with `Url.Action("Index", "Admin")` and no route values. In `Medications.cshtml`, remove `secretKey`, remove every `asp-route-secretKey`, generate Export without route values, and add this form beside the page title:

```cshtml
<form asp-action="Logout" asp-controller="Admin" method="post">
    @Html.AntiForgeryToken()
    <button type="submit" class="btn btn-outline-secondary">Log out</button>
</form>
```

- [ ] **Step 4: Run the focused browser and contract tests and verify GREEN**

Run both commands from Step 2.

Expected: the UI contract tests and both Admin Playwright scenarios pass.

- [ ] **Step 5: Commit the user-facing gate**

```powershell
git add SleepEditWeb/Views/Admin SleepEditWeb/Views/Shared/_Layout.cshtml SleepEditWeb/e2e/tests/admin-password-gate.spec.ts SleepEditWeb.Tests/ProtocolEditorUiContractsTests.cs
git commit -m "Add Admin password gate UI"
```

---

### Task 4: Full verification

**Files:**
- Verify all files changed in Tasks 1-3.

**Interfaces:**
- Consumes: the complete Admin gate.
- Produces: evidence that server, frontend, and browser behavior remain compatible.

- [ ] **Step 1: Search for leaked legacy and new passwords**

```powershell
rg -n "medAdmin2025xK9|sleep123" SleepEditWeb -g "!Web/AdminAccess/AdminAccessConstants.cs" -g "!e2e/**"
```

Expected: no matches. The only production occurrence of `sleep123` is `AdminAccessConstants.cs`; test and design files may contain the expected test credential.

- [ ] **Step 2: Run all .NET tests**

```powershell
dotnet test SleepEditWeb.sln --no-restore
```

Expected: all tests pass with zero failures.

- [ ] **Step 3: Run frontend checks**

```powershell
cd SleepEditWeb
npm run check:frontend
```

Expected: frontend guardrails and all JavaScript tests pass.

- [ ] **Step 4: Run the complete Playwright suite**

```powershell
cd SleepEditWeb
npx playwright test
```

Expected: all existing 12 scenarios plus the 2 Admin scenarios pass.

- [ ] **Step 5: Verify repository state**

```powershell
git diff --check
git status --short --branch
git log -4 --oneline
```

Expected: no whitespace errors, no uncommitted implementation files, and the three implementation commits appear after the approved design and plan commits.
