using System;
using System.IO;
using System.Globalization;
using System.Linq;
using Microsoft.VisualBasic.FileIO;

public static class CsvFile
{
    public static void importFromCSV(AvailabilityService service)
    {
        using StreamReader? sourceFile = Open();
        if (sourceFile is null) return;
        int imported = 0, skipped = 0;
        try
        {
            using var parser = new TextFieldParser(sourceFile);
            parser.SetDelimiters(",");
            parser.HasFieldsEnclosedInQuotes = true;
            string[] expected = { "First Name", "Last Name", "Title", "Specialty", "Date", "Start", "End" };
            var header = parser.ReadFields();
            if (header is null || !header.SequenceEqual(expected, StringComparer.OrdinalIgnoreCase))
            {
                Console.WriteLine("Expected CSV columns: " + string.Join(",", expected));
                return;
            }
            while (!parser.EndOfData)
            {
                long line = parser.LineNumber;
                try
                {
                    var fields = parser.ReadFields();
                    if (fields is null || fields.Length != 7 || fields.Any(string.IsNullOrWhiteSpace))
                        throw new FormatException("Expected seven non-empty fields.");
                    DateTime start = DateTime.ParseExact(fields[4] + " " + fields[5],
                        "yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
                    DateTime end = DateTime.ParseExact(fields[4] + " " + fields[6],
                        "yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
                    service.ImportAvailability(fields[0], fields[1], fields[2], fields[3], start, end);
                    imported++;
                }
                catch (Exception error) when (error is FormatException || error is ArgumentException ||
                    error is InvalidOperationException || error is MalformedLineException)
                {
                    skipped++;
                    Console.WriteLine($"Skipped line {line}: {error.Message}");
                }
            }
        }
        catch (Exception error) when (error is IOException || error is MalformedLineException)
        {
            Console.WriteLine($"Could not finish reading CSV: {error.Message}");
        }
        Console.WriteLine($"Imported {imported} availabilities; skipped {skipped} rows.");
    }

    public static StreamReader? Open()
    {
        while (true)
        {
            Console.Write("Enter the source CSV file path: ");
            string? input = Console.ReadLine();
            if (input is null) return null;

            if (string.IsNullOrWhiteSpace(input))
            {
                Console.WriteLine("A file path is required.");
                continue;
            }

            string path = input.Trim().Trim('"');
            if (!File.Exists(path))
            {
                Console.WriteLine($"File not found: {path}");
                continue;
            }

            try
            {
                return new StreamReader(path);
            }
            catch (Exception exception) when (
                exception is UnauthorizedAccessException || exception is IOException)
            {
                Console.WriteLine($"Could not open file: {exception.Message}");
            }
        }
    }
}
