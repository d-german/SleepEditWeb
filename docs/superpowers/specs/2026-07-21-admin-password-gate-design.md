# Admin Password Gate Design

## Goal

Require the hard-coded password `sleep123` before any Admin page or operation can be used. This includes the Protocol Editor and all of its mutation endpoints because it is hosted inside the Admin dashboard. A successful login unlocks Admin for the existing 30-minute idle session. Logout revokes access immediately.

This is intentionally a lightweight deterrent, not full user authentication. The password will remain in server-side code and must never appear in generated links, HTML, query strings, or logs.

## Routes and user experience

- `GET /Admin/Login` displays a password form.
- `POST /Admin/Login` validates the anti-forgery token and submitted password.
- A correct password records an Admin-unlocked flag in the current ASP.NET Core session, then redirects to the requested local Admin URL or the medication dashboard.
- An incorrect password redisplays the login form with `Incorrect password.` and does not unlock the session.
- `POST /Admin/Logout` validates the anti-forgery token, removes the Admin session flag, and redirects to the login page.
- The sidebar Admin link points to the medication dashboard without including a secret.
- Existing medication dashboard, export, import, reseed, and clear-user-medication routes no longer contain or accept a secret key.

## Protection architecture

An Admin password middleware runs after `UseSession` and before route execution. It checks requests whose path begins with `/Admin` or `/ProtocolEditor`, while allowing the login endpoints and static resources to proceed. The public `/ProtocolViewer` remains unaffected.

For a protected Admin request:

1. If the session contains the unlocked flag, the request proceeds.
2. Otherwise, safe `GET` requests redirect to `/Admin/Login` with a local return URL.
3. Other HTTP methods are rejected before their controller action can mutate data. The response redirects to login using HTTP 303 so a browser follows with `GET` rather than replaying the original request body.

The middleware protects current and future Admin and Protocol Editor controller routes without relying on each action author to remember a check.

## Components

- `AdminAccessConstants` owns the session key and hard-coded password so controller and middleware use one definition.
- `AdminPasswordMiddleware` owns route-wide enforcement and safe login redirection.
- `AdminController` adds login/logout actions and removes secret-key parameters from medication actions.
- `AdminLoginViewModel` carries the password, return URL, and validation error state.
- `Views/Admin/Login.cshtml` provides the password form.
- `Views/Admin/Medications.cshtml` generates secret-free Admin operation URLs and includes Logout.
- `Views/Shared/_Layout.cshtml` generates a secret-free Admin navigation link.

## Security and error handling

- The password input uses `type="password"` and is never echoed back.
- Login and logout posts use anti-forgery validation.
- Return URLs are accepted only when `Url.IsLocalUrl` succeeds; otherwise the dashboard is used.
- Invalid passwords are logged only as denied attempts, never with the submitted value.
- Export, Protocol Editor, and all mutation endpoints are protected by the same session gate.
- Existing session cookies remain HTTP-only and essential, with a 30-minute idle timeout.
- No rate limiting, user identity, password rotation, or cryptographic credential storage is added because the requested scope is a simple hard-coded password.

## Testing

Test-first coverage will establish the behavior before production changes:

- Controller/unit tests for correct and incorrect password handling, local return URL validation, logout, and secret-free route generation.
- Middleware tests proving anonymous Admin and Protocol Editor requests are blocked, login remains reachable, unlocked sessions pass, and protected non-GET requests cannot reach their action.
- Playwright end-to-end tests proving direct Admin navigation shows login, a wrong password fails, `sleep123` unlocks the dashboard for later Admin navigation, and Logout restores the gate.
- Existing .NET, frontend, and Playwright suites remain green.
