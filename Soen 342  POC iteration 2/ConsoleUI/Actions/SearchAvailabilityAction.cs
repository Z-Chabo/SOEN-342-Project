using System;

public static class SearchAvailabilityAction
{
    public static void Execute(AvailabilityService service)
    {
        int professionalId = ConsoleInput.ReadPositiveInt("Medical professional ID: ");
        DateTime date = ConsoleInput.ReadDateTime("Search date", "yyyy-MM-dd");
        var slots = service.SearchAvailability(professionalId, date);
        if (slots.Count == 0)
        {
            Console.WriteLine("No available appointments found.");
            return;
        }

        Console.WriteLine("Available appointment start times:");
        foreach (var slot in slots)
            Console.WriteLine(slot.ToString("yyyy-MM-dd HH:mm"));
    }
}
