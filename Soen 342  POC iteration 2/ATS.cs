
using System;
using System.Collections.Generic;
using System.Linq;

public enum BookingMethod
{
    InClinic,
    ByPhone
}

public class Patient
{
    public int Id { get; init; }
    public string Name { get; init; } = "";
}

public class MedicalProfessional
{
    public int Id { get; init; }
    public string FirstName { get; init; } = "";
    public string LastName { get; init; } = "";
    public string Title { get; init; } = "";
    public string Specialty { get; init; } = "";
}

public class Availability
{
    public int Id { get; init; }
    public int ProfessionalId { get; init; }
    public DateTime Start { get; init; }
    public DateTime End { get; init; }
}

public class Appointment
{
    public int Id { get; init; }
    public Patient Patient { get; init; } = null!;
    public int ProfessionalId { get; init; }
    public DateTime DateTime { get; init; }
    public BookingMethod Method { get; init; }
}

public class AppointmentService
{
    private readonly List<Appointment> appointments = new();
    private readonly List<Availability> availabilities = new();
    private readonly object sync = new();
    private readonly Func<DateTime> now;

    public AppointmentService(Func<DateTime>? now = null)
    {
        this.now = now ?? (() => DateTime.Now);
    }

    public Availability CreateAvailability(int professionalId, DateTime start, DateTime end)
    {
        if (professionalId <= 0)
            throw new ArgumentOutOfRangeException(nameof(professionalId));
        if (end <= start)
            throw new ArgumentException("End must be after start.", nameof(end));

        lock (sync)
        {
            if (availabilities.Any(a => a.ProfessionalId == professionalId &&
                start < a.End && end > a.Start))
                throw new InvalidOperationException("The availability overlaps an existing slot.");

            var availability = new Availability
            {
                Id = availabilities.Count + 1,
                ProfessionalId = professionalId,
                Start = start,
                End = end
            };
            availabilities.Add(availability);
            return availability;
        }
    }

    // Use case 1: Search for Availability
    public List<DateTime> SearchAvailability(
        int professionalId, DateTime date)
    {
        if (professionalId <= 0)
            throw new ArgumentOutOfRangeException(nameof(professionalId));

        lock (sync)
        {
            return FindAvailableSlots(professionalId, date, now());
        }
    }

    private List<DateTime> FindAvailableSlots(
        int professionalId, DateTime date, DateTime currentTime)
    {
        return availabilities
            .Where(slot => slot.ProfessionalId == professionalId &&
                slot.Start.Date == date.Date && slot.Start > currentTime &&
                !appointments.Any(a => a.ProfessionalId == professionalId &&
                    a.DateTime == slot.Start))
            .OrderBy(slot => slot.Start)
            .Select(slot => slot.Start)
            .ToList();
    }

    // Use case 2: Book an Appointment
    public Appointment BookAppointment(
        Patient patient,
        int professionalId,
        DateTime requestedTime,
        BookingMethod method)
    {
        ArgumentNullException.ThrowIfNull(patient);
        if (patient.Id <= 0 || string.IsNullOrWhiteSpace(patient.Name))
            throw new ArgumentException("A patient must have a positive ID and a name.", nameof(patient));
        if (professionalId <= 0)
            throw new ArgumentOutOfRangeException(nameof(professionalId));
        if (!Enum.IsDefined(method))
            throw new ArgumentOutOfRangeException(nameof(method));

        // Check availability and reserve the slot as one operation.
        lock (sync)
        {
            // Indirectly use Search Availability.
            var available = SearchAvailability(
                professionalId, requestedTime.Date);

            if (!available.Contains(requestedTime))
            {
                throw new InvalidOperationException(
                    "The requested appointment is unavailable.");
            }

            Appointment appointment = new Appointment
            {
                Id = appointments.Count + 1,
                Patient = patient,
                ProfessionalId = professionalId,
                DateTime = requestedTime,
                Method = method
            };

            appointments.Add(appointment);

            return appointment;
        }
    }
}

public class Program
{
    public static void Main()
    {
        AppointmentService service = new();

        Patient patient = new Patient
        {
            Id = 1,
            Name = "John"
        };

        int professionalId = 101;
        DateTime tomorrow = DateTime.Today.AddDays(1);

        // Explicitly create tomorrow's slots for this demonstration.
        for (int hour = 9; hour < 17; hour++)
            service.CreateAvailability(professionalId,
                tomorrow.AddHours(hour), tomorrow.AddHours(hour + 1));

        // Direct Search Availability
        var slots = service.SearchAvailability(
            professionalId, tomorrow);

        Console.WriteLine("Available appointments:");

        foreach (var slot in slots)
        {
            Console.WriteLine(slot);
        }

        // Booking a patient who arrived at the clinic
        service.BookAppointment(
            patient,
            professionalId,
            tomorrow.AddHours(10),
            BookingMethod.InClinic);

        // Booking a patient over the phone
        service.BookAppointment(
            patient,
            professionalId,
            tomorrow.AddHours(11),
            BookingMethod.ByPhone);

        Console.WriteLine("Appointments booked successfully!");
    }
}
