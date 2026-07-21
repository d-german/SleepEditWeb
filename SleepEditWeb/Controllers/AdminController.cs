using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using SleepEditWeb.Data;
using SleepEditWeb.Models;
using SleepEditWeb.Web.AdminAccess;

namespace SleepEditWeb.Controllers;

/// <summary>
/// Admin controller for medication database management.
/// Access is protected by the Admin session middleware.
/// </summary>
[Route("Admin/Medications")]
public class AdminController : Controller
{
    private readonly IMedicationRepository _repository;
    private readonly ILogger<AdminController> _logger;

    public AdminController(IMedicationRepository repository, ILogger<AdminController> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    [HttpGet("/Admin/Login")]
    public IActionResult Login(string? returnUrl = null)
    {
        return View("Login", new AdminLoginViewModel { ReturnUrl = returnUrl });
    }

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

    /// <summary>
    /// Admin dashboard with database statistics.
    /// GET: /Admin/Medications
    /// </summary>
    [HttpGet("")]
    public IActionResult Index()
    {
        _logger.LogInformation("Admin index requested.");
        var stats = _repository.GetStats();
        return View("Medications", stats);
    }

    /// <summary>
    /// Export all medications as JSON backup file.
    /// GET: /Admin/Medications/Export
    /// </summary>
    [HttpGet("Export")]
    public IActionResult Export()
    {
        _logger.LogInformation("Admin export requested.");
        var backup = _repository.ExportAll();
        var json = JsonSerializer.Serialize(backup, new JsonSerializerOptions 
        { 
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        var fileName = $"medications_backup_{DateTime.UtcNow:yyyy-MM-dd}.json";
        return File(System.Text.Encoding.UTF8.GetBytes(json), "application/json", fileName);
    }

    /// <summary>
    /// Import medications from JSON backup file.
    /// POST: /Admin/Medications/Import
    /// </summary>
    [HttpPost("Import")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Import(IFormFile file, string mode)
    {
        if (file == null || file.Length == 0)
        {
            _logger.LogWarning("Admin import aborted because uploaded file was missing or empty.");
            TempData["Error"] = "Please select a backup file to import.";
            return RedirectToAction(nameof(Index));
        }

        _logger.LogInformation("Admin import requested. Mode: {Mode}, FileName: {FileName}, Size: {Size}", mode, file.FileName, file.Length);

        try
        {
            using var stream = new StreamReader(file.OpenReadStream());
            var json = await stream.ReadToEndAsync();
            var backup = JsonSerializer.Deserialize<MedicationBackup>(json, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            if (backup?.Medications == null || backup.Medications.Count == 0)
            {
                _logger.LogWarning("Admin import aborted because backup content was invalid or empty.");
                TempData["Error"] = "Invalid backup file or empty medication list.";
                return RedirectToAction(nameof(Index));
            }

            if (mode == "replace")
            {
                _repository.ImportReplace(backup.Medications);
                _logger.LogInformation("Admin import completed with replace mode. Count: {Count}", backup.Medications.Count);
                TempData["Success"] = $"Database replaced with {backup.Medications.Count} medications from backup.";
            }
            else // merge
            {
                _repository.ImportMerge(backup.Medications);
                _logger.LogInformation("Admin import completed with merge mode. Count: {Count}", backup.Medications.Count);
                TempData["Success"] = "Backup merged successfully. New medications added, existing preserved.";
            }
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Admin import failed due to invalid JSON payload.");
            TempData["Error"] = "Invalid JSON format in backup file.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Admin import failed unexpectedly.");
            TempData["Error"] = $"Import failed: {ex.Message}";
        }

        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Reseed database from embedded resource.
    /// POST: /Admin/Medications/Reseed
    /// </summary>
    [HttpPost("Reseed")]
    [ValidateAntiForgeryToken]
    public IActionResult Reseed()
    {
        _logger.LogInformation("Admin reseed requested.");
        try
        {
            _repository.Reseed();
            _logger.LogInformation("Admin reseed completed successfully.");
            TempData["Success"] = "Database reseeded successfully from embedded resource.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Admin reseed failed.");
            TempData["Error"] = $"Reseed failed: {ex.Message}";
        }

        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Clear all user-added medications.
    /// POST: /Admin/Medications/ClearUserMeds
    /// </summary>
    [HttpPost("ClearUserMeds")]
    [ValidateAntiForgeryToken]
    public IActionResult ClearUserMeds()
    {
        _logger.LogInformation("Admin clear-user-meds requested.");
        try
        {
            _repository.ClearUserMedications();
            _logger.LogInformation("Admin clear-user-meds completed successfully.");
            TempData["Success"] = "All user-added medications have been cleared.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Admin clear-user-meds failed.");
            TempData["Error"] = $"Clear failed: {ex.Message}";
        }

        return RedirectToAction(nameof(Index));
    }
}
