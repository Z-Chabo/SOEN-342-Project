using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

public static class CsvExport
{
    public static void exportToCSV(AvailabilityService service)
    {
        while (true)
        {
            Console.Write("Enter the destination CSV file name (or full path): ");
            string? input = Console.ReadLine();
            if (input is null) return;
            string path = input.Trim().Trim('"');
            if (string.IsNullOrWhiteSpace(path))
            {
                Console.WriteLine("A file name is required.");
                continue;
            }

            if (!path.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
                path += ".csv";

            if (File.Exists(path))
            {
                Console.Write("File already exists. Overwrite? (y/n): ");
                if (!string.Equals(Console.ReadLine()?.Trim(), "y", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine("Export cancelled.");
                    return;
                }
            }

            try
            {
                var availabilities = service.GetAllAvailabilities();
                using (var writer = new StreamWriter(path, false, new UTF8Encoding(false)))
                {
                    writer.WriteLine("First Name,Last Name,Title,Specialty,Date,Start,End");
                    foreach (var (slot, professional) in availabilities)
                    {
                        string[] fields =
                        {
                            professional?.FirstName ?? "",
                            professional?.LastName ?? "",
                            professional?.Title ?? "",
                            professional?.Specialty ?? "",
                            slot.Start.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                            slot.Start.ToString("HH:mm", CultureInfo.InvariantCulture),
                            slot.End.ToString("HH:mm", CultureInfo.InvariantCulture)
                        };
                        writer.WriteLine(string.Join(",", fields.Select(EscapeField)));
                    }
                }
                Console.WriteLine($"Exported {availabilities.Count} availabilities to {Path.GetFullPath(path)}.");
                return;
            }
            catch (Exception error) when (error is IOException || error is UnauthorizedAccessException ||
                error is ArgumentException || error is NotSupportedException)
            {
                Console.WriteLine($"Could not export CSV: {error.Message}");
            }
        }
    }

    private static string EscapeField(string value)
    {
        return value.IndexOfAny(new[] { ',', '"', '\r', '\n' }) >= 0
            ? "\"" + value.Replace("\"", "\"\"") + "\""
            : value;
    }
}
