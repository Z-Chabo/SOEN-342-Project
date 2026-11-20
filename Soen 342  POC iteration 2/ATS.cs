using System;
using System.Globalization;
using System.Linq;

public class Program
{
    public static void Main()
    {
        var availabilityService = new AvailabilityService();
        Console.WriteLine("Welcome to Appointment Management.");

        while (true)
        {
            Console.WriteLine("\nAppointment Management");
            Console.WriteLine("1. Book an appointment");
            Console.WriteLine("2. Search for availability");
            Console.WriteLine("3. Create availability");
            Console.WriteLine("4. Import from CSV");
            Console.WriteLine("5. Export to CSV");
            Console.WriteLine("0. Exit");
            Console.Write("Choose an option: ");

            string? choice = Console.ReadLine()?.Trim();
            switch (choice)
            {
                case "1":
                    Console.WriteLine("you selected book an appointment");
                    break;
                case "2":
                    Console.WriteLine("you selected search for availability");
                    break;
                case "3":
                    Console.WriteLine("you selected create availability");
                    MakeAvailability(availabilityService);
                    break;
                case "4":
                    Console.WriteLine("you selected import from csv");
                    break;
                case "5":
                    Console.WriteLine("you selected export to csv");
                    break;
                case "0":
                    Console.WriteLine("you selected exit");
                    Console.WriteLine("Goodbye!");
                    return;
                case null:
                    Console.WriteLine("Goodbye!");
                    return;
                default:
                    Console.WriteLine("Please choose an option from 0 to 3.");
                    break;
            }
        }
    }

    private static void MakeAvailability(AvailabilityService service)
    {
        try
        {
            Console.Write("Professional ID: ");
            int professionalId = int.Parse(Console.ReadLine()!);

            Console.Write("Start date (yyyy-MM-dd): ");
            DateTime startDate = DateTime.ParseExact(
                Console.ReadLine()!, "yyyy-MM-dd", CultureInfo.InvariantCulture);

            Console.Write("End date (yyyy-MM-dd): ");
            DateTime endDate = DateTime.ParseExact(
                Console.ReadLine()!, "yyyy-MM-dd", CultureInfo.InvariantCulture);

            Console.Write("Weekdays (e.g. Monday,Wednesday): ");
            string? weekdays = Console.ReadLine();
            if (weekdays == null) return;
            DayOfWeek[] days = weekdays.Split(',')
                .Select(day => Enum.Parse<DayOfWeek>(day.Trim(), true))
                .ToArray();

            Console.Write("Daily start time (HH:mm): ");
            TimeSpan startTime = TimeOnly.ParseExact(
                Console.ReadLine()!, "HH:mm", CultureInfo.InvariantCulture).ToTimeSpan();

            Console.Write("Daily end time (HH:mm): ");
            TimeSpan endTime = TimeOnly.ParseExact(
                Console.ReadLine()!, "HH:mm", CultureInfo.InvariantCulture).ToTimeSpan();

            Console.Write("Slot duration in minutes: ");
            int minutes = int.Parse(Console.ReadLine()!);

            // Call the existing availability logic.
            var slots = service.CreateRepeatingAvailability(
                professionalId, startDate, endDate, days,
                startTime, endTime, TimeSpan.FromMinutes(minutes));

            Console.WriteLine($"Created {slots.Count} availability slots.");
            foreach (var slot in slots)
            {
                Console.WriteLine($"{slot.Start:yyyy-MM-dd HH:mm} - {slot.End:HH:mm}");
            }
        }
        catch (Exception error) when (
            error is FormatException || error is OverflowException ||
            error is ArgumentException || error is InvalidOperationException)
        {
            Console.WriteLine($"Could not create availability: {error.Message}");
        }
    }
}
