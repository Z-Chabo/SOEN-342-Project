using System;
using System.IO;

public class Program
{
    public static void Main()
    {
        AvailabilityService availabilityService = new();
        AppointmentService appointmentService = new(availabilityService);

        Console.WriteLine("Welcome to Appointment Management.");
        try
        {
            while (true)
            {
                Console.WriteLine("\nAppointment Management");
                Console.WriteLine("1. Book an appointment");
                Console.WriteLine("2. Search for availability");
                Console.WriteLine("3. Create availability");
                Console.WriteLine("0. Exit");

                try
                {
                    switch (ConsoleInput.ReadRequired("Choose an option: "))
                    {
                        case "1":
                            BookingAction.Execute(appointmentService);
                            break;
                        case "2":
                            SearchAvailabilityAction.Execute(availabilityService);
                            break;
                        case "3":
                            CreateAvailabilityAction.Execute(availabilityService);
                            break;
                        case "0":
                            return;
                        default:
                            Console.WriteLine("Please choose an option from 0 to 3.");
                            break;
                    }
                }
                catch (EndOfStreamException)
                {
                    return;
                }
                catch (Exception exception) when (
                    exception is ArgumentException || exception is InvalidOperationException)
                {
                    Console.WriteLine($"Could not complete the operation: {exception.Message}");
                }
            }
        }
        finally
        {
            Console.WriteLine("Goodbye!");
        }
    }
}
