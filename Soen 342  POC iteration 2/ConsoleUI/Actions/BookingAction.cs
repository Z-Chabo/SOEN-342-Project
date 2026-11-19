using System;

public static class BookingAction
{
    public static void Execute(AppointmentService service)
    {
        var patient = new Patient
        {
            Id = ConsoleInput.ReadPositiveInt("Patient ID: "),
            Name = ConsoleInput.ReadRequired("Patient name: ")
        };
        int professionalId = ConsoleInput.ReadPositiveInt("Medical professional ID: ");
        DateTime requestedTime = ConsoleInput.ReadDateTime("Appointment date and time", "yyyy-MM-dd HH:mm");
        string choice;
        do
        {
            choice = ConsoleInput.ReadRequired("Booking method (1 = In clinic, 2 = By phone): ");
            if (choice != "1" && choice != "2")
                Console.WriteLine("Please choose 1 or 2.");
        } while (choice != "1" && choice != "2");

        var appointment = service.BookAppointment(patient, professionalId, requestedTime,
            choice == "1" ? BookingMethod.InClinic : BookingMethod.ByPhone);
        Console.WriteLine($"Appointment #{appointment.Id} booked for {patient.Name} on {appointment.DateTime:yyyy-MM-dd HH:mm}.");
    }
}
