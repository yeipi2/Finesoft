using System.Text.RegularExpressions;
using System.Web;

namespace fs_backend.Util;

public static class InputSanitizer
{
    private static readonly Regex DangerousCharsRegex = new(
        @"[<>'"";\\]|(\b(SELECT|INSERT|UPDATE|DELETE|DROP|ALTER|CREATE|TRUNCATE|EXEC|EXECUTE|UNION|JOIN|FROM|WHERE)\b)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex HtmlTagRegex = new(
        @"<[^>]+>",
        RegexOptions.Compiled);

    private static readonly Regex SqlInjectionPattern = new(
        @"(\b(SELECT|INSERT|UPDATE|DELETE|DROP|ALTER|CREATE|TRUNCATE|EXEC|EXECUTE|UNION|AND|OR)\b|['"";\\]|(--|\/\*|\*\/))",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static string Sanitize(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        var sanitized = input.Trim();
        sanitized = HttpUtility.HtmlEncode(sanitized);
        
        return sanitized;
    }

    public static string SanitizeHtml(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        return HtmlTagRegex.Replace(input, string.Empty);
    }

    public static bool ContainsSqlInjection(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return false;

        return SqlInjectionPattern.IsMatch(input);
    }

    public static bool ContainsDangerousChars(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return false;

        return DangerousCharsRegex.IsMatch(input);
    }

    public static string SanitizeForDatabase(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        var sanitized = input.Trim();
        sanitized = sanitized.Replace("'", "''");
        sanitized = sanitized.Replace(";", string.Empty);
        sanitized = sanitized.Replace("--", string.Empty);
        sanitized = sanitized.Replace("/*", string.Empty);
        sanitized = sanitized.Replace("*/", string.Empty);
        
        return sanitized;
    }

    public static string SanitizeFileName(string? fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return "file";

        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = string.Join("_", fileName.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries));
        
        return Path.GetFileNameWithoutExtension(sanitized);
    }
}

public class SanitizedInputAttribute : Attribute
{
}

public static class InputValidationHelper
{
    public static (bool IsValid, string? ErrorMessage) ValidateInput(string? input, string fieldName, int maxLength = 500, bool allowHtml = false)
    {
        if (string.IsNullOrWhiteSpace(input))
            return (true, null);

        if (input.Length > maxLength)
            return (false, $"{fieldName} excede el límite de {maxLength} caracteres");

        if (!allowHtml && InputSanitizer.ContainsDangerousChars(input))
            return (false, $"{fieldName} contiene caracteres no permitidos");

        if (InputSanitizer.ContainsSqlInjection(input))
            return (false, $"{fieldName} contiene patrones potencialmente dangerous");

        return (true, null);
    }
}
