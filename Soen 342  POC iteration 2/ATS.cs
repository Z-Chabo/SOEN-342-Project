using System;

public class Program
{
    public static void Main()
    {
        AvailabilityService availabilityService = new();
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
                    break;
                case "4":
                    CsvFile.importFromCSV(availabilityService);
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
                    Console.WriteLine("Please choose an option from 0 to 5.");
                    break;
            }
        }
    }
}
