using System;
using System.Globalization;
using System.IO;

public static class ConsoleInput
{
    public static string ReadRequired(string prompt)
    {
        while (true)
        {
            Console.Write(prompt);
            string? input = Console.ReadLine();
            if (input is null) throw new EndOfStreamException();
            if (!string.IsNullOrWhiteSpace(input)) return input.Trim();
            Console.WriteLine("A value is required.");
        }
    }

    public static int ReadPositiveInt(string prompt)
    {
        while (true)
        {
            if (int.TryParse(ReadRequired(prompt), out int value) && value > 0)
                return value;
            Console.WriteLine("Enter a positive whole number.");
        }
    }

    public static DateTime ReadDateTime(string label, string format)
    {
        while (true)
        {
            if (DateTime.TryParseExact(ReadRequired($"{label} ({format}): "), format,
                CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime value))
                return value;
            Console.WriteLine($"Enter a valid value in the format {format}.");
        }
    }
}
