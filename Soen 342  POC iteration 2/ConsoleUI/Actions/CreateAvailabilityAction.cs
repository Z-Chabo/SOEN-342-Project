using System;
using System.Collections.Generic;

public static class CreateAvailabilityAction
{
    public static void Execute(AvailabilityService service)
    {
        string choice = ConsoleInput.ReadRequired("Availability type (1 = Single slot, 2 = Repeating schedule): ");
        if (choice != "1" && choice != "2")
        {
            Console.WriteLine("Please choose 1 or 2.");
            return;
        }

        int professionalId = ConsoleInput.ReadPositiveInt("Medical professional ID: ");
        if (choice == "1")
        {
            DateTime start = ConsoleInput.ReadDateTime("Slot start", "yyyy-MM-dd HH:mm");
            DateTime end = ConsoleInput.ReadDateTime("Slot end", "yyyy-MM-dd HH:mm");
            var slot = service.CreateAvailability(professionalId, start, end);
            Console.WriteLine($"Availability #{slot.Id} created.");
            return;
        }

        DateTime startDate = ConsoleInput.ReadDateTime("Start date", "yyyy-MM-dd");
        DateTime endDate = ConsoleInput.ReadDateTime("End date (inclusive)", "yyyy-MM-dd");
        var days = new List<DayOfWeek>();
        while (true)
        {
            string input = ConsoleInput.ReadRequired("Weekdays, separated by commas (e.g. Monday, Wednesday): ");
            days.Clear();
            bool valid = true;
            foreach (string value in input.Split(','))
            {
                if (!Enum.TryParse<DayOfWeek>(value.Trim(), true, out var day) || !Enum.IsDefined(day))
                {
                    valid = false;
                    break;
                }
                days.Add(day);
            }
            if (valid) break;
            Console.WriteLine("Enter valid weekday names separated by commas.");
        }

        TimeSpan startTime = ConsoleInput.ReadDateTime("Daily start time", "HH:mm").TimeOfDay;
        TimeSpan endTime = ConsoleInput.ReadDateTime("Daily end time", "HH:mm").TimeOfDay;
        int minutes = ConsoleInput.ReadPositiveInt("Slot duration in minutes: ");
        var created = service.CreateRepeatingAvailability(professionalId, startDate, endDate,
            days, startTime, endTime, TimeSpan.FromMinutes(minutes));
        Console.WriteLine($"Created {created.Count} availability slots.");
    }
}
