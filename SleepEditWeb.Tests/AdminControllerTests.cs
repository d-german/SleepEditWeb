using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SleepEditWeb.Controllers;
using SleepEditWeb.Data;
using SleepEditWeb.Models;
using SleepEditWeb.Tests.TestSupport;
using SleepEditWeb.Web.AdminAccess;

namespace SleepEditWeb.Tests;

[TestFixture]
public sealed class AdminControllerTests
{
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

    [TearDown]
    public void TearDown()
    {
        _controller.Dispose();
    }

    [Test]
    public void LoginGet_PreservesReturnUrlWithoutExposingAPassword()
    {
        var result = _controller.Login("/ProtocolEditor");

        var view = result as ViewResult;
        var model = view?.Model as AdminLoginViewModel;
        Assert.Multiple(() =>
        {
            Assert.That(view?.ViewName, Is.EqualTo("Login"));
            Assert.That(model?.ReturnUrl, Is.EqualTo("/ProtocolEditor"));
            Assert.That(model?.Password, Is.Empty);
        });
    }

    [Test]
    public void Login_WrongPassword_DoesNotUnlockAndClearsSubmittedPassword()
    {
        var model = new AdminLoginViewModel
        {
            Password = "wrong",
            ReturnUrl = "/Admin/Medications"
        };

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
        const string returnUrl = "/Admin/Medications?tab=protocol";
        _url.Setup(x => x.IsLocalUrl(returnUrl)).Returns(true);
        var model = new AdminLoginViewModel
        {
            Password = "sleep123",
            ReturnUrl = returnUrl
        };

        var result = _controller.Login(model);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.TypeOf<LocalRedirectResult>());
            Assert.That(((LocalRedirectResult)result).Url, Is.EqualTo(returnUrl));
            Assert.That(
                _session.GetString(AdminAccessConstants.SessionKey),
                Is.EqualTo(AdminAccessConstants.SessionUnlockedValue));
        });
    }

    [Test]
    public void Login_ExternalReturnUrl_UsesDashboard()
    {
        const string returnUrl = "https://example.com";
        _url.Setup(x => x.IsLocalUrl(returnUrl)).Returns(false);

        var result = _controller.Login(new AdminLoginViewModel
        {
            Password = "sleep123",
            ReturnUrl = returnUrl
        });

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.TypeOf<RedirectToActionResult>());
            Assert.That(
                ((RedirectToActionResult)result).ActionName,
                Is.EqualTo(nameof(AdminController.Index)));
        });
    }

    [Test]
    public void Logout_RemovesUnlockFlagAndRedirectsToLogin()
    {
        _session.SetString(
            AdminAccessConstants.SessionKey,
            AdminAccessConstants.SessionUnlockedValue);

        var result = _controller.Logout();

        Assert.Multiple(() =>
        {
            Assert.That(_session.GetString(AdminAccessConstants.SessionKey), Is.Null);
            Assert.That(result, Is.TypeOf<RedirectToActionResult>());
            Assert.That(
                ((RedirectToActionResult)result).ActionName,
                Is.EqualTo(nameof(AdminController.Login)));
        });
    }

    [TestCase(nameof(AdminController.Index), "")]
    [TestCase(nameof(AdminController.Export), "Export")]
    [TestCase(nameof(AdminController.Import), "Import")]
    [TestCase(nameof(AdminController.Reseed), "Reseed")]
    [TestCase(nameof(AdminController.ClearUserMeds), "ClearUserMeds")]
    public void MedicationActions_UseSecretFreeRoutes(string actionName, string routeTemplate)
    {
        var method = typeof(AdminController).GetMethod(actionName)!;
        var route = method.GetCustomAttributes(inherit: true)
            .OfType<HttpMethodAttribute>()
            .Single();

        Assert.Multiple(() =>
        {
            Assert.That(method.GetParameters().Select(parameter => parameter.Name),
                Does.Not.Contain("secretKey"));
            Assert.That(route.Template, Is.EqualTo(routeTemplate));
        });
    }
}
