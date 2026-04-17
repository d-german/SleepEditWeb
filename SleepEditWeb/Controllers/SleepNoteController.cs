using Microsoft.AspNetCore.Mvc;
using SleepEditWeb.Models;
using SleepEditWeb.Services;

namespace SleepEditWeb.Controllers;

[Route("SleepNote")]
public sealed class SleepNoteController(
    ISleepNoteService sleepNoteService,
    ILogger<SleepNoteController> logger) : Controller
{
    [HttpGet]
    public IActionResult Index() => View();

    [HttpPost("api/generate")]
    [ValidateAntiForgeryToken]
    public IActionResult GenerateNote([FromBody] SleepNoteFormData? formData)
    {
        if (formData is null)
            return BadRequest("Form data is required.");

        var result = sleepNoteService.GenerateNote(formData);
        logger.LogInformation("Sleep note generated ({Length} chars)", result.NarrativeText.Length);
        return Ok(result);
    }

    [HttpGet("api/config")]
    public IActionResult GetConfiguration()
    {
        var config = sleepNoteService.GetConfiguration();
        return Ok(config);
    }

    [HttpPost("api/config/mask-types")]
    [ValidateAntiForgeryToken]
    public IActionResult AddMaskType([FromBody] ValueRequest? request)
    {
        if (string.IsNullOrWhiteSpace(request?.Value))
            return BadRequest("Mask type value is required.");

        sleepNoteService.AddMaskType(request.Value);
        return Created();
    }

    [HttpDelete("api/config/mask-types/{maskType}")]
    [ValidateAntiForgeryToken]
    public IActionResult RemoveMaskType(string maskType)
    {
        if (string.IsNullOrWhiteSpace(maskType))
            return BadRequest("Mask type is required.");

        sleepNoteService.RemoveMaskType(maskType);
        return NoContent();
    }

    [HttpPost("api/config/mask-sizes")]
    [ValidateAntiForgeryToken]
    public IActionResult AddMaskSize([FromBody] ValueRequest? request)
    {
        if (string.IsNullOrWhiteSpace(request?.Value))
            return BadRequest("Mask size value is required.");

        sleepNoteService.AddMaskSize(request.Value);
        return Created();
    }

    [HttpDelete("api/config/mask-sizes/{maskSize}")]
    [ValidateAntiForgeryToken]
    public IActionResult RemoveMaskSize(string maskSize)
    {
        if (string.IsNullOrWhiteSpace(maskSize))
            return BadRequest("Mask size is required.");

        sleepNoteService.RemoveMaskSize(maskSize);
        return NoContent();
    }

    public sealed class ValueRequest
    {
        public string Value { get; init; } = string.Empty;
    }
}
