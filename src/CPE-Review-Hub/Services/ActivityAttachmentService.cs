using System.IO.Compression;
using System.Text;
using CPE_Review_Hub.Models;

namespace CPE_Review_Hub.Services;

public sealed class ActivityAttachmentService
{
    public const int MaximumFiles = 3;
    public const long MaximumFileSize = 5 * 1024 * 1024;
    public const long MaximumTotalSize = 10 * 1024 * 1024;

    private static readonly Dictionary<string, string> AllowedTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        [".png"] = "image/png", [".jpg"] = "image/jpeg", [".jpeg"] = "image/jpeg",
        [".webp"] = "image/webp", [".pdf"] = "application/pdf",
        [".docx"] = "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        [".txt"] = "text/plain"
    };

    public async Task<(IReadOnlyList<SubmissionAttachment> Files, string? Error)> ReadAsync(
        IReadOnlyList<IFormFile> uploads, CancellationToken cancellationToken)
    {
        var files = uploads.Where(x => x.Length > 0).ToList();
        if (files.Count > MaximumFiles) return ([], $"Attach no more than {MaximumFiles} files.");
        if (files.Any(x => x.Length > MaximumFileSize)) return ([], "Each attachment must be 5 MB or smaller.");
        if (files.Sum(x => x.Length) > MaximumTotalSize) return ([], "Attachments must total 10 MB or less.");

        var result = new List<SubmissionAttachment>();
        foreach (var upload in files)
        {
            var safeName = Path.GetFileName(upload.FileName).Trim();
            var extension = Path.GetExtension(safeName);
            if (safeName.Length is 0 or > 255 || !AllowedTypes.TryGetValue(extension, out var contentType))
                return ([], "Allowed attachments: PNG, JPG, WEBP, PDF, DOCX, and TXT.");
            await using var input = upload.OpenReadStream();
            await using var memory = new MemoryStream((int)upload.Length);
            await input.CopyToAsync(memory, cancellationToken);
            var data = memory.ToArray();
            if (!HasValidSignature(extension, data))
                return ([], $"{safeName} does not match its file type or is damaged.");
            result.Add(new SubmissionAttachment { FileName = safeName, ContentType = contentType,
                FileSize = data.LongLength, Data = data });
        }
        return (result, null);
    }

    private static bool HasValidSignature(string extension, byte[] data) => extension.ToLowerInvariant() switch
    {
        ".png" => Starts(data, [0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a]),
        ".jpg" or ".jpeg" => Starts(data, [0xff, 0xd8, 0xff]),
        ".pdf" => Starts(data, [0x25, 0x50, 0x44, 0x46]),
        ".webp" => data.Length >= 12 && Encoding.ASCII.GetString(data, 0, 4) == "RIFF" && Encoding.ASCII.GetString(data, 8, 4) == "WEBP",
        ".docx" => IsDocx(data),
        ".txt" => data.Length == 0 || !data.Contains((byte)0),
        _ => false
    };

    private static bool Starts(byte[] data, byte[] signature) => data.AsSpan().StartsWith(signature);
    private static bool IsDocx(byte[] data)
    {
        try
        {
            using var stream = new MemoryStream(data);
            using var archive = new ZipArchive(stream, ZipArchiveMode.Read);
            return archive.GetEntry("[Content_Types].xml") is not null && archive.GetEntry("word/document.xml") is not null;
        }
        catch (InvalidDataException) { return false; }
    }
}
