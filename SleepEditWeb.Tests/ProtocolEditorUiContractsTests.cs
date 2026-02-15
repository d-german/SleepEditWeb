using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using SleepEditWeb.Controllers;

namespace SleepEditWeb.Tests;

[TestFixture]
public class ProtocolEditorUiContractsTests
{
    [Test]
    public void ProtocolEditorController_RouteTemplates_RemainStable()
    {
        var controllerRoute = typeof(ProtocolEditorController).GetCustomAttribute<RouteAttribute>();
        Assert.That(controllerRoute, Is.Not.Null, "ProtocolEditorController missing [Route] attribute.");
        Assert.That(controllerRoute!.Template, Is.EqualTo("ProtocolEditor"));

        AssertRouteTemplate<HttpGetAttribute>("Index", string.Empty);
        AssertRouteTemplate<HttpGetAttribute>("State", "State");
        AssertRouteTemplate<HttpPostAttribute>("AddSection", "AddSection");
        AssertRouteTemplate<HttpPostAttribute>("AddChild", "AddChild");
        AssertRouteTemplate<HttpPostAttribute>("RemoveNode", "RemoveNode");
        AssertRouteTemplate<HttpPostAttribute>("UpdateNode", "UpdateNode");
        AssertRouteTemplate<HttpPostAttribute>("MoveNode", "MoveNode");
        AssertRouteTemplate<HttpPostAttribute>("AddSubText", "AddSubText");
        AssertRouteTemplate<HttpPostAttribute>("RemoveSubText", "RemoveSubText");
        AssertRouteTemplate<HttpPostAttribute>("Undo", "Undo");
        AssertRouteTemplate<HttpPostAttribute>("Redo", "Redo");
        AssertRouteTemplate<HttpPostAttribute>("Reset", "Reset");
        AssertRouteTemplate<HttpPostAttribute>("SaveXml", "SaveXml");
        AssertRouteTemplate<HttpPostAttribute>("SetDefaultProtocol", "SetDefaultProtocol");
        AssertRouteTemplate<HttpPostAttribute>("ImportXml", "ImportXml");
        AssertRouteTemplate<HttpPostAttribute>("ImportXmlUpload", "ImportXmlUpload");
        AssertRouteTemplate<HttpGetAttribute>("ExportXml", "ExportXml");
    }

    [Test]
    public void AdminView_RendersProtocolTabBeforeMedicationTab_AndProtocolPaneActive()
    {
        var content = File.ReadAllText(ResolveRepoFile("SleepEditWeb/Views/Admin/Medications.cshtml"));

        var protocolTabIndex = content.IndexOf("id=\"admin-protocol-tab\"", StringComparison.Ordinal);
        var medicationTabIndex = content.IndexOf("id=\"admin-medications-tab\"", StringComparison.Ordinal);
        Assert.That(protocolTabIndex, Is.GreaterThanOrEqualTo(0), "Protocol tab id not found.");
        Assert.That(medicationTabIndex, Is.GreaterThanOrEqualTo(0), "Medication tab id not found.");
        Assert.That(protocolTabIndex, Is.LessThan(medicationTabIndex), "Protocol tab should render before medications tab.");

        Assert.That(
            Regex.IsMatch(content, "<button(?=[^>]*id=\"admin-protocol-tab\")(?=[^>]*class=\"[^\"]*nav-link active[^\"]*\")[^>]*>", RegexOptions.Multiline),
            Is.True,
            "Protocol tab should be active.");
        Assert.That(
            Regex.IsMatch(content, "<div(?=[^>]*id=\"admin-protocol-pane\")(?=[^>]*class=\"[^\"]*tab-pane fade show active[^\"]*\")[^>]*>", RegexOptions.Multiline),
            Is.True,
            "Protocol pane should be active.");
        Assert.That(
            Regex.IsMatch(content, "<div(?=[^>]*id=\"admin-medications-pane\")(?=[^>]*class=\"(?![^\"]*active)[^\"]*tab-pane fade[^\"]*\")[^>]*>", RegexOptions.Multiline),
            Is.True,
            "Medication pane should not be active.");
    }

    [Test]
    public void ProtocolEditorView_UsesProtocolWording_WhileRetainingXmlEndpoints()
    {
        var content = File.ReadAllText(ResolveRepoFile("SleepEditWeb/Views/ProtocolEditor/Index.cshtml"));

        Assert.That(content, Does.Contain(">Import Protocol<"));
        Assert.That(content, Does.Contain(">Save Protocol<"));
        Assert.That(content, Does.Contain(">Export Protocol<"));

        Assert.That(content, Does.Contain("/ProtocolEditor/ImportXmlUpload"));
        Assert.That(content, Does.Contain("/ProtocolEditor/SaveXml"));
        Assert.That(content, Does.Contain("/ProtocolEditor/SetDefaultProtocol"));
        Assert.That(content, Does.Contain("/ProtocolEditor/${action}"));
        Assert.That(content, Does.Contain("ExportXml"));
    }

    [Test]
    public void ProtocolEditorAndViewerViews_UseModuleBootstraps()
    {
        var editorContent = File.ReadAllText(ResolveRepoFile("SleepEditWeb/Views/ProtocolEditor/Index.cshtml"));
        var viewerContent = File.ReadAllText(ResolveRepoFile("SleepEditWeb/Views/ProtocolViewer/Index.cshtml"));

        Assert.That(editorContent, Does.Contain("<script type=\"module\">"));
        Assert.That(editorContent, Does.Contain("/js/protocol-editor-ui.js"));
        Assert.That(viewerContent, Does.Contain("<script type=\"module\">"));
        Assert.That(viewerContent, Does.Contain("/js/protocol-viewer-bootstrap.js"));
    }

    private static void AssertRouteTemplate<TAttribute>(string actionName, string expectedTemplate)
        where TAttribute : HttpMethodAttribute
    {
        var action = typeof(ProtocolEditorController).GetMethod(actionName, BindingFlags.Public | BindingFlags.Instance);
        Assert.That(action, Is.Not.Null, $"Action method '{actionName}' not found.");

        var attribute = action!.GetCustomAttribute<TAttribute>();
        Assert.That(attribute, Is.Not.Null, $"Action '{actionName}' missing {typeof(TAttribute).Name}.");
        Assert.That(attribute!.Template, Is.EqualTo(expectedTemplate));
    }

    private static string ResolveRepoFile(string relativePath)
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current != null && !File.Exists(Path.Combine(current.FullName, "SleepEditWeb.sln")))
        {
            current = current.Parent;
        }

        Assert.That(current, Is.Not.Null, "Could not locate repository root.");
        return Path.Combine(current!.FullName, relativePath.Replace('/', Path.DirectorySeparatorChar));
    }
}
