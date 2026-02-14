using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using SleepEditWeb.Data;
using SleepEditWeb.Models;

namespace SleepEditWeb.Controllers;

/// <summary>
/// Admin controller for medication database management.
/// Protected by secret URL - wrong key returns 404.
/// </summary>
[Route("Admin/Medications")]
public class AdminController : Controller
{
    // TODO: Consider moving to appsettings.json for production
    private const string SecretKey = "medAdmin2025xK9!";

    private readonly IMedicationRepository _repository;
    private readonly ILogger<AdminController> _logger;

    public AdminController(IMedicationRepository repository, ILogger<AdminController> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    /// <summary>
    /// Validates the secret key and returns NotFound if invalid.
    /// </summary>
    private static bool IsValidKey(string secretKey)
    {
        return !string.IsNullOrEmpty(secretKey) && 
               secretKey.Equals(SecretKey, StringComparison.Ordinal);
    }

    /// <summary>
    /// Admin dashboard with database statistics.
    /// GET: /Admin/Medications/{secretKey}
    /// </summary>
    [HttpGet("{secretKey}")]
    public IActionResult Index(string secretKey)
    {
        if (!IsValidKey(secretKey))
        {
            _logger.LogWarning("Admin index denied due to invalid key.");
            return NotFound();
        }

        _logger.LogInformation("Admin index requested.");
        var stats = _repository.GetStats();
        ViewBag.SecretKey = secretKey;
        return View("Medications", stats);
    }

    /// <summary>
    /// Export all medications as JSON backup file.
    /// GET: /Admin/Medications/{secretKey}/Export
    /// </summary>
    [HttpGet("{secretKey}/Export")]
    public IActionResult Export(string secretKey)
    {
        if (!IsValidKey(secretKey))
        {
            _logger.LogWarning("Admin export denied due to invalid key.");
            return NotFound();
        }

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
    /// POST: /Admin/Medications/{secretKey}/Import
    /// </summary>
    [HttpPost("{secretKey}/Import")]
    public async Task<IActionResult> Import(string secretKey, IFormFile file, string mode)
    {
        if (!IsValidKey(secretKey))
        {
            _logger.LogWarning("Admin import denied due to invalid key.");
            return NotFound();
        }

        if (file == null || file.Length == 0)
        {
            _logger.LogWarning("Admin import aborted because uploaded file was missing or empty.");
            TempData["Error"] = "Please select a backup file to import.";
            return RedirectToAction(nameof(Index), new { secretKey });
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
                return RedirectToAction(nameof(Index), new { secretKey });
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

        return RedirectToAction(nameof(Index), new { secretKey });
    }

    /// <summary>
    /// Reseed database from embedded resource.
    /// POST: /Admin/Medications/{secretKey}/Reseed
    /// </summary>
    [HttpPost("{secretKey}/Reseed")]
    [ValidateAntiForgeryToken]
    public IActionResult Reseed(string secretKey)
    {
        if (!IsValidKey(secretKey))
        {
            _logger.LogWarning("Admin reseed denied due to invalid key.");
            return NotFound();
        }

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

        return RedirectToAction(nameof(Index), new { secretKey });
    }

    /// <summary>
    /// Clear all user-added medications.
    /// POST: /Admin/Medications/{secretKey}/ClearUserMeds
    /// </summary>
    [HttpPost("{secretKey}/ClearUserMeds")]
    [ValidateAntiForgeryToken]
    public IActionResult ClearUserMeds(string secretKey)
    {
        if (!IsValidKey(secretKey))
        {
            _logger.LogWarning("Admin clear-user-meds denied due to invalid key.");
            return NotFound();
        }

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

        return RedirectToAction(nameof(Index), new { secretKey });
    }
}
