using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using SleepEditWeb.Data;
using SleepEditWeb.Models;
using CSharpFunctionalExtensions;

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

    public AdminController(IMedicationRepository repository)
    {
        _repository = repository;
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
            return NotFound();

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
            return NotFound();

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
            return NotFound();

        if (file == null || file.Length == 0)
        {
            TempData["Error"] = "Please select a backup file to import.";
            return RedirectToAction(nameof(Index), new { secretKey });
        }

        var result = await ProcessImportAsync(file, mode);
        var (_, isFailure, successMessage, error) = result;

        if (isFailure)
        {
            TempData["Error"] = error;
        }
        else
        {
            TempData["Success"] = successMessage;
        }

        return RedirectToAction(nameof(Index), new { secretKey });
    }

    private async Task<Result<string>> ProcessImportAsync(IFormFile file, string mode)
    {
        return await Result.Try(async () => 
        {
            using var stream = new StreamReader(file.OpenReadStream());
            var json = await stream.ReadToEndAsync();
            var backup = JsonSerializer.Deserialize<MedicationBackup>(json, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            if (backup?.Medications == null || backup.Medications.Count == 0)
            {
                return Result.Failure<string>("Invalid backup file or empty medication list.");
            }

            if (mode == "replace")
            {
                var replaceResult = _repository.ImportReplace(backup.Medications);
                return replaceResult.IsFailure 
                    ? Result.Failure<string>(replaceResult.Error) 
                    : Result.Success($"Database replaced with {backup.Medications.Count} medications from backup.");
            }
            else // merge
            {
                var mergeResult = _repository.ImportMerge(backup.Medications);
                return mergeResult.IsFailure 
                    ? Result.Failure<string>(mergeResult.Error) 
                    : Result.Success("Backup merged successfully. New medications added, existing preserved.");
            }
        }, ex => 
        {
            return ex switch
            {
                JsonException => "Invalid JSON format in backup file.",
                _ => $"Import failed: {ex.Message}"
            };
        }).Bind(r => r);
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
            return NotFound();

        var result = _repository.Reseed();
        var (_, isFailure, error) = result;

        if (isFailure)
        {
            TempData["Error"] = error;
        }
        else
        {
            TempData["Success"] = "Database reseeded successfully from embedded resource.";
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
            return NotFound();

        var result = _repository.ClearUserMedications();
        var (_, isFailure, error) = result;

        if (isFailure)
        {
            TempData["Error"] = error;
        }
        else
        {
            TempData["Success"] = "All user-added medications have been cleared.";
        }

        return RedirectToAction(nameof(Index), new { secretKey });
    }
}
