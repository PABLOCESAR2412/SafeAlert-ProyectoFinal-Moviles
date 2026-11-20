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

    public static bool IsValidPhone(string? value)
    {
        var digits = DigitsOnly(value);
        return digits.Length is >= 10 and <= 15;
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
