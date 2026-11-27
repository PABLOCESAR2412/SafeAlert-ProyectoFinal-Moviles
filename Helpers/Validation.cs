using System.Text.RegularExpressions;

namespace SafeAlert.Helpers;

public static class Validation
{
    private static readonly Regex EmailRegex = new(
        @"^[^@\s]+@[^@\s]+\.[^@\s]{2,}$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public static string DigitsOnly(string? value) =>
        new((value ?? string.Empty).Where(char.IsDigit).ToArray());

    public static bool IsValidEmail(string? value) =>
        !string.IsNullOrWhiteSpace(value) && EmailRegex.IsMatch(value.Trim());

    public static bool IsValidPhone(string? value) => NormalizePhone(value) is not null;

    public static string? NormalizePhone(string? value)
    {
        var text = value?.Trim() ?? string.Empty;
        var international = text.StartsWith('+');
        var number = international ? text[1..] : text;
        if (number.Any(c => !char.IsAsciiDigit(c) && !char.IsWhiteSpace(c) && c is not ('-' or '(' or ')')))
            return null;

        var digits = DigitsOnly(number);
        if (international && !digits.StartsWith("593", StringComparison.Ordinal))
            return digits.Length is >= 8 and <= 15 && digits[0] != '0' ? "+" + digits : null;

        if (international)
            digits = digits[3..];

        if (!digits.StartsWith('0'))
            digits = "0" + digits;

        var mobile = digits.Length == 10 && digits.StartsWith("09", StringComparison.Ordinal);
        var landline = digits.Length == 9 && digits[1] is >= '2' and <= '7';
        return mobile || landline ? digits : null;
    }

    public static bool IsValidFullName(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        var words = value.Trim()
            .Split(' ', StringSplitOptions.RemoveEmptyEntries);

        return words.Length >= 2 &&
               words.All(word => word.All(c => char.IsLetter(c) || c is '\'' or '’'));
    }

    public static bool IsLetters(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        return value.Trim().All(c => char.IsLetter(c) || c is ' ' or '\'' or '’' or '-' or '.');
    }

    public static bool IsStrongEnough(string? password) =>
        !string.IsNullOrWhiteSpace(password) && password.Length >= 6;
}
