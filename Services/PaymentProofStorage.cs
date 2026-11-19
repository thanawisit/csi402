using Microsoft.AspNetCore.Http;

namespace IsanDer.Services;

public static class PaymentProofStorage
{
    public const long MaxFileSize = 5 * 1024 * 1024;
    private static readonly byte[] PngSignature = [137, 80, 78, 71, 13, 10, 26, 10];

    public static bool TryGetExtension(IFormFile? file, out string extension)
    {
        extension = string.Empty;
        if (file == null || file.Length == 0 || file.Length > MaxFileSize)
        {
            return false;
        }

        using var stream = file.OpenReadStream();
        var header = new byte[8];
        var bytesRead = 0;
        while (bytesRead < header.Length)
        {
            var read = stream.Read(header, bytesRead, header.Length - bytesRead);
            if (read == 0) break;
            bytesRead += read;
        }
        if (bytesRead == PngSignature.Length && header.SequenceEqual(PngSignature))
        {
            extension = ".png";
            return true;
        }

        if (bytesRead >= 3 && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF)
        {
            extension = ".jpg";
            return true;
        }

        return false;
    }

    public static string GetDirectory(string contentRootPath) =>
        Path.Combine(contentRootPath, "App_Data", "payment-proofs");

    public static string? FindPath(string contentRootPath, int orderId)
    {
        var directory = GetDirectory(contentRootPath);
        var pngPath = Path.Combine(directory, $"{orderId}.png");
        if (File.Exists(pngPath))
        {
            return pngPath;
        }

        var jpgPath = Path.Combine(directory, $"{orderId}.jpg");
        return File.Exists(jpgPath) ? jpgPath : null;
    }

    public static async Task SaveAsync(
        string contentRootPath,
        int orderId,
        IFormFile file,
        string extension,
        CancellationToken cancellationToken)
    {
        var directory = GetDirectory(contentRootPath);
        Directory.CreateDirectory(directory);

        var temporaryPath = Path.Combine(directory, $".{Guid.NewGuid():N}.tmp");
        var destinationPath = Path.Combine(directory, $"{orderId}{extension}");
        try
        {
            await using (var stream = new FileStream(
                temporaryPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                81920,
                useAsync: true))
            {
                await file.CopyToAsync(stream, cancellationToken);
            }

            File.Move(temporaryPath, destinationPath, overwrite: true);

            var otherExtension = extension == ".png" ? ".jpg" : ".png";
            var otherPath = Path.Combine(directory, $"{orderId}{otherExtension}");
            if (File.Exists(otherPath))
            {
                File.Delete(otherPath);
            }
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }
}
