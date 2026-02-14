using System.Text.Json;
using System.Text.Json.Serialization;

namespace SleepEditWeb.Services;

/// <summary>
/// Service to lookup drug information from OpenFDA API.
/// </summary>
public interface IDrugInfoService
{
    Task<DrugInfo?> GetDrugInfoAsync(string drugName);
}

/// <summary>
/// Drug information from OpenFDA.
/// </summary>
public sealed class DrugInfo
{
    public string Name { get; init; } = "";
    public string? GenericName { get; init; }
    public string? Purpose { get; init; }
    public string? Uses { get; init; }
    public string? Warnings { get; init; }
    public string? Dosage { get; init; }
    public string? Manufacturer { get; init; }
    public string Source { get; init; } = "OpenFDA";
    public bool Found { get; init; }
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// Implementation using OpenFDA API (free, no API key required).
/// https://open.fda.gov/apis/drug/label/
/// </summary>
public sealed class OpenFdaDrugInfoService : IDrugInfoService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OpenFdaDrugInfoService> _logger;
    private const string BaseUrl = "https://api.fda.gov/drug/label.json";

    public OpenFdaDrugInfoService(HttpClient httpClient, ILogger<OpenFdaDrugInfoService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<DrugInfo?> GetDrugInfoAsync(string drugName)
    {
        if (string.IsNullOrWhiteSpace(drugName))
        {
            _logger.LogWarning("Drug info lookup rejected because drug name was empty.");
            return null;
        }

        _logger.LogInformation("Drug info lookup requested for {DrugName}", drugName);

        try
        {
            // Try brand name first, then generic name
            var result = await SearchByBrandNameAsync(drugName) 
                         ?? await SearchByGenericNameAsync(drugName);

            if (result != null)
            {
                _logger.LogInformation("Drug info lookup completed successfully for {DrugName}", drugName);
                return result;
            }

            _logger.LogInformation("Drug info lookup completed with no FDA label match for {DrugName}", drugName);
            return new DrugInfo
            {
                Name = drugName,
                Found = false,
                ErrorMessage = "No FDA drug label information found for this medication."
            };
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Failed to fetch drug info for {DrugName}", drugName);
            return new DrugInfo
            {
                Name = drugName,
                Found = false,
                ErrorMessage = "Unable to connect to FDA database. Please try again later."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching drug info for {DrugName}", drugName);
            return new DrugInfo
            {
                Name = drugName,
                Found = false,
                ErrorMessage = "An error occurred while looking up drug information."
            };
        }
    }

    private async Task<DrugInfo?> SearchByBrandNameAsync(string drugName)
    {
        var encodedName = Uri.EscapeDataString(drugName);
        var url = $"{BaseUrl}?search=openfda.brand_name:\"{encodedName}\"&limit=1";
        return await FetchAndParseAsync(drugName, url);
    }

    private async Task<DrugInfo?> SearchByGenericNameAsync(string drugName)
    {
        var encodedName = Uri.EscapeDataString(drugName);
        var url = $"{BaseUrl}?search=openfda.generic_name:\"{encodedName}\"&limit=1";
        return await FetchAndParseAsync(drugName, url);
    }

    private async Task<DrugInfo?> FetchAndParseAsync(string drugName, string url)
    {
        var response = await _httpClient.GetAsync(url);
        
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogDebug("OpenFDA request returned non-success status {StatusCode} for {DrugName}", response.StatusCode, drugName);
            return null;
        }

        var json = await response.Content.ReadAsStringAsync();
        var fdaResponse = JsonSerializer.Deserialize<OpenFdaResponse>(json);

        if (fdaResponse?.Results == null || fdaResponse.Results.Count == 0)
            return null;

        var result = fdaResponse.Results[0];
        var openfda = result.OpenFda;

        return new DrugInfo
        {
            Name = openfda?.BrandName?.FirstOrDefault() ?? drugName,
            GenericName = openfda?.GenericName?.FirstOrDefault(),
            Purpose = CleanText(result.Purpose?.FirstOrDefault()),
            Uses = CleanText(result.IndicationsAndUsage?.FirstOrDefault()),
            Warnings = CleanText(result.Warnings?.FirstOrDefault()),
            Dosage = CleanText(result.DosageAndAdministration?.FirstOrDefault()),
            Manufacturer = openfda?.ManufacturerName?.FirstOrDefault(),
            Found = true,
            Source = "OpenFDA"
        };
    }

    private static string? CleanText(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;

        // Remove HTML tags and clean up whitespace
        var cleaned = System.Text.RegularExpressions.Regex.Replace(text, "<[^>]+>", " ");
        cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"\s+", " ");
        return cleaned.Trim();
    }
}

#region OpenFDA JSON Models

internal sealed class OpenFdaResponse
{
    [JsonPropertyName("results")]
    public List<OpenFdaResult>? Results { get; set; }
}

internal sealed class OpenFdaResult
{
    [JsonPropertyName("purpose")]
    public List<string>? Purpose { get; set; }

    [JsonPropertyName("indications_and_usage")]
    public List<string>? IndicationsAndUsage { get; set; }

    [JsonPropertyName("warnings")]
    public List<string>? Warnings { get; set; }

    [JsonPropertyName("dosage_and_administration")]
    public List<string>? DosageAndAdministration { get; set; }

    [JsonPropertyName("openfda")]
    public OpenFdaDetails? OpenFda { get; set; }
}

internal sealed class OpenFdaDetails
{
    [JsonPropertyName("brand_name")]
    public List<string>? BrandName { get; set; }

    [JsonPropertyName("generic_name")]
    public List<string>? GenericName { get; set; }

    [JsonPropertyName("manufacturer_name")]
    public List<string>? ManufacturerName { get; set; }
}

#endregion
