namespace fs_front.Utils;

public static class Pipes
{
    public static string ToDoublePipe(this string input)
    {
        return input.Replace("|", "||");
    }

    public static string ToCurrency(this string input)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;

        var cleanInput = new string(input.Where(c => char.IsDigit(c) || c == '.').ToArray());

        if (decimal.TryParse(cleanInput, out decimal number))
        {
            return number.ToString("C");
        }

        return input;
    }

    public static string ToCurrency(this decimal input)
    {
        return input.ToString("C");
    }

    public static string ToCurrency(this double input)
    {
        return input.ToString("C");
    }

    public static string ToCurrency(this int input)
    {
        return input.ToString("C");
    }
}