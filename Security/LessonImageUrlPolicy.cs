namespace CPE_Review_Hub.Security;

public static class LessonImageUrlPolicy
{
    private static readonly HashSet<string> DirectImageHosts = new(StringComparer.OrdinalIgnoreCase)
    {
        "upload.wikimedia.org"
    };

    private static readonly HashSet<string> SourcePageHosts = new(StringComparer.OrdinalIgnoreCase)
    {
        "commons.wikimedia.org"
    };

    public static bool IsAllowedDirectImage(string? value) => IsAllowed(value, DirectImageHosts) &&
        Uri.TryCreate(value, UriKind.Absolute, out var uri) &&
        (uri.AbsolutePath.EndsWith(".svg", StringComparison.OrdinalIgnoreCase) ||
         uri.AbsolutePath.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ||
         uri.AbsolutePath.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
         uri.AbsolutePath.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase) ||
         uri.AbsolutePath.EndsWith(".webp", StringComparison.OrdinalIgnoreCase));

    public static bool IsAllowedSourcePage(string? value) => IsAllowed(value, SourcePageHosts) &&
        Uri.TryCreate(value, UriKind.Absolute, out var uri) &&
        uri.AbsolutePath.StartsWith("/wiki/File", StringComparison.OrdinalIgnoreCase);

    private static bool IsAllowed(string? value, HashSet<string> hosts)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri)) return false;
        return uri.Scheme == Uri.UriSchemeHttps && uri.IsDefaultPort &&
            string.IsNullOrEmpty(uri.UserInfo) && hosts.Contains(uri.IdnHost);
    }
}
