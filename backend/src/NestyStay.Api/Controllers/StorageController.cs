using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NestyStay.Application.Abstractions;

namespace NestyStay.Api.Controllers;

/// <summary>
/// Streams private server-local objects only when the provider-issued,
/// short-lived URL signature is valid. Application endpoints perform the
/// resource authorization before issuing the URL.
/// </summary>
[ApiController]
[Route("api/storage")]
public sealed class StorageController(IStorageProvider storageProvider) : ControllerBase
{
    [AllowAnonymous]
    [HttpGet("objects")]
    public async Task<IActionResult> Download(
        [FromQuery] string key,
        [FromQuery] long expires,
        [FromQuery] string token,
        CancellationToken cancellationToken)
    {
        if (!TryDecodeKey(key, out var objectKey) ||
            !storageProvider.ValidateAccessToken("GET", objectKey, expires, token))
        {
            return NotFound();
        }

        try
        {
            var stream = await storageProvider.OpenReadAsync(objectKey, cancellationToken);
            return File(stream, ContentTypeFor(objectKey), enableRangeProcessing: false);
        }
        catch (FileNotFoundException)
        {
            return NotFound();
        }
        catch (DirectoryNotFoundException)
        {
            return NotFound();
        }
    }

    private static bool TryDecodeKey(string encodedKey, out string objectKey)
    {
        objectKey = string.Empty;
        if (string.IsNullOrWhiteSpace(encodedKey)) return false;

        try
        {
            var padded = encodedKey.Replace('-', '+').Replace('_', '/');
            padded += new string('=', (4 - padded.Length % 4) % 4);
            objectKey = Encoding.UTF8.GetString(Convert.FromBase64String(padded));
            return !string.IsNullOrWhiteSpace(objectKey);
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static string ContentTypeFor(string objectKey) =>
        Path.GetExtension(objectKey).ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".webp" => "image/webp",
            ".pdf" => "application/pdf",
            ".zip" => "application/zip",
            ".csv" => "text/csv",
            _ => "application/octet-stream"
        };
}
