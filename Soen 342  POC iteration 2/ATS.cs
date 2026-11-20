using System;
using System.Globalization;
using System.Linq;

public class Program
{
    public static void Main()
    {
        AvailabilityService availabilityService = new();
        AppointmentService appointmentService = new(availabilityService);
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
                    RunAction(() => SearchOrBook(availabilityService, appointmentService, true));
                    break;
                case "2":
                    RunAction(() => SearchOrBook(availabilityService, appointmentService, false));
                    break;
                case "3":
                    RunAction(() => ChooseAvailabilityType(availabilityService));
                    break;
                case "4":
                    CsvImport.importFromCSV(availabilityService);
                    break;
                case "5":
                    CsvExport.exportToCSV(availabilityService);
                    break;
                case "0":
                    Console.WriteLine("you selected exit");
                    Console.WriteLine("Goodbye!");
                    return;
                case null:
                    Console.WriteLine("Goodbye!");
                    return;
                default:
                    Console.WriteLine("Please choose an option from 0 to 5.");
                    break;
            }
        }
    }

    private static bool HasAvailability(AvailabilityService service)
    {
        if (service.State.Availabilities.Count > 0) return true;
        Console.WriteLine("No availabilities exist. Create availability using option 3 or import from CSV using option 4.");
        return false;
    }

    private static string ReadRequired(string prompt)
    {
        Console.Write(prompt);
        string? value = Console.ReadLine();
        if (value is null) throw new OperationCanceledException();
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("A value is required. Please try again.");
        return value.Trim();
    }

    private static DateTime ReadDate() => DateTime.ParseExact(
        ReadRequired("Date (yyyy-MM-dd): "), "yyyy-MM-dd", CultureInfo.InvariantCulture);

    private static TimeSpan ReadTime(string prompt) => DateTime.ParseExact(
        ReadRequired(prompt + " (HH:mm): "), "HH:mm", CultureInfo.InvariantCulture).TimeOfDay;

    private static void RunAction(Action action)
    {
        try { action(); }
        catch (OperationCanceledException) { Console.WriteLine("Cancelled."); }
        catch (Exception error) when (error is ArgumentException || error is FormatException ||
            error is InvalidOperationException || error is OverflowException)
        {
            Console.WriteLine(error.Message);
        }
    }

    private static void ChooseAvailabilityType(AvailabilityService service)
    {
        string choice = ReadRequired("Availability type (1 = individual slot, 2 = repeating schedule): ");
        switch (choice)
        {
            case "1":
                CreateAvailability(service);
                break;
            case "2":
                MakeAvailability(service);
                break;
            default:
                throw new ArgumentException("Select availability type 1 or 2.");
        }
    }

    private static void MakeAvailability(AvailabilityService service)
    {
        int professionalId = int.Parse(ReadRequired("Professional ID: "));
        DateTime startDate = DateTime.ParseExact(ReadRequired("Start date (yyyy-MM-dd): "),
            "yyyy-MM-dd", CultureInfo.InvariantCulture);
        DateTime endDate = DateTime.ParseExact(ReadRequired("End date (yyyy-MM-dd): "),
            "yyyy-MM-dd", CultureInfo.InvariantCulture);
        DayOfWeek[] days = ReadRequired("Weekdays (e.g. Monday,Wednesday): ").Split(',')
            .Select(day => Enum.Parse<DayOfWeek>(day.Trim(), true)).ToArray();
        TimeSpan startTime = ReadTime("Daily start time");
        TimeSpan endTime = ReadTime("Daily end time");
        int minutes = int.Parse(ReadRequired("Slot duration in minutes: "));
        var slots = service.CreateRepeatingAvailability(professionalId, startDate, endDate,
            days, startTime, endTime, TimeSpan.FromMinutes(minutes));
        Console.WriteLine($"Created {slots.Count} availability slots.");
        foreach (var slot in slots)
            Console.WriteLine($"{slot.Start:yyyy-MM-dd HH:mm} - {slot.End:HH:mm}");
    }

    private static void CreateAvailability(AvailabilityService service)
    {
        string firstName = ReadRequired("Professional first name: ");
        string lastName = ReadRequired("Professional last name: ");
        string title = ReadRequired("Title: ");
        string specialty = ReadRequired("Specialty: ");
        DateTime date = ReadDate();
        TimeSpan start = ReadTime("Start time");
        TimeSpan end = ReadTime("End time");
        service.ImportAvailability(firstName, lastName, title, specialty, date.Add(start), date.Add(end));
        Console.WriteLine("Availability created.");
    }

    private static void SearchOrBook(AvailabilityService availability,
        AppointmentService appointments, bool book)
    {
        if (!HasAvailability(availability)) return;

        var professionals = availability.State.Professionals.ToList();
        for (int i = 0; i < professionals.Count; i++)
        {
            var professional = professionals[i];
            Console.WriteLine($"{i + 1}. {professional.Title} {professional.FirstName} {professional.LastName} ({professional.Specialty})");
        }
        int professionalOption = int.Parse(ReadRequired("Choose a professional (option number): "));
        if (professionalOption < 1 || professionalOption > professionals.Count)
            throw new ArgumentException("Select a valid option from the professional list.");
        int professionalId = professionals[professionalOption - 1].Id;
        DateTime date = ReadDate();
        var slots = availability.SearchAvailability(professionalId, date);
        if (slots.Count == 0)
        {
            Console.WriteLine("No available slots for this professional on this date. Try another date, create availability using option 3, or import from CSV using option 4.");
            return;
        }
        for (int i = 0; i < slots.Count; i++)
        {
            var slot = availability.State.Availabilities.Single(a =>
                a.ProfessionalId == professionalId && a.Start == slots[i]);
            Console.WriteLine($"{i + 1}. {slot.Start:HH:mm} - {slot.End:HH:mm}");
        }
        if (!book) return;

        int selection = int.Parse(ReadRequired("Slot number: "));
        if (selection < 1 || selection > slots.Count)
            throw new ArgumentException("Select a slot from the list.");
        int patientId = int.Parse(ReadRequired("Patient ID: "));
        string patientName = ReadRequired("Patient name: ");
        string method = ReadRequired("Booking method (1 = in clinic, 2 = by phone): ");
        if (method != "1" && method != "2")
            throw new ArgumentException("Select booking method 1 or 2.");
        var appointment = appointments.BookAppointment(
            new Patient { Id = patientId, Name = patientName }, professionalId, slots[selection - 1],
            method == "1" ? BookingMethod.InClinic : BookingMethod.ByPhone);
        Console.WriteLine($"Appointment booked for {appointment.Patient.Name} at {appointment.DateTime:yyyy-MM-dd HH:mm}.");
    }
}
