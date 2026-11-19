using System.Collections.Concurrent;

internal static class Tests
{
    private static readonly DateTime Today = new(2026, 11, 17);
    private static readonly Patient Patient = new() { Id = 1, Name = "John" };

    private static void Main()
    {
        var service = new AppointmentService(() => Today.AddHours(10));
        var tomorrow = Today.AddDays(1);
        Check(service.SearchAvailability(101, tomorrow).Count == 0, "Start with no availability");
        Reject<InvalidOperationException>(() => service.BookAppointment(Patient, 101, tomorrow.AddHours(10), BookingMethod.InClinic));
        foreach (var date in new[] { Today.AddDays(-1), Today, tomorrow })
            foreach (var professionalId in new[] { 101, 102 })
                for (int hour = 9; hour < 17; hour++)
                    service.CreateAvailability(professionalId, date.AddHours(hour), date.AddHours(hour + 1));

        Reject<ArgumentOutOfRangeException>(() => service.CreateAvailability(0, tomorrow, tomorrow.AddHours(1)));
        Reject<ArgumentException>(() => service.CreateAvailability(101, tomorrow, tomorrow));
        Reject<ArgumentException>(() => service.CreateAvailability(101, tomorrow.AddHours(1), tomorrow));
        Reject<InvalidOperationException>(() => service.CreateAvailability(101, tomorrow.AddHours(9), tomorrow.AddHours(10)));
        Reject<InvalidOperationException>(() => service.CreateAvailability(101, tomorrow.AddHours(8.5), tomorrow.AddHours(9.5)));
        Reject<InvalidOperationException>(() => service.CreateAvailability(101, tomorrow.AddHours(8), tomorrow.AddHours(18)));

        var shortSlot = service.CreateAvailability(103, tomorrow.AddHours(8), tomorrow.AddHours(8).AddMinutes(45));
        var adjacent = service.CreateAvailability(103, shortSlot.End, tomorrow.AddHours(9).AddMinutes(15));
        Check(shortSlot.Id != adjacent.Id && shortSlot.ProfessionalId == 103, "Unique availability IDs and professional");
        Check(service.SearchAvailability(103, tomorrow).SequenceEqual(new[] { shortSlot.Start, adjacent.Start }), "Variable durations and adjacent slots");
        service.BookAppointment(Patient, 103, adjacent.Start, BookingMethod.ByPhone);
        Check(service.SearchAvailability(103, tomorrow).SequenceEqual(new[] { shortSlot.Start }), "Book a non-hourly slot");
        Check(service.SearchAvailability(101, tomorrow).Count == 8, "Eight hourly slots");
        Check(service.SearchAvailability(101, Today).First() == Today.AddHours(11), "Exclude elapsed slots");
        Check(service.SearchAvailability(101, Today.AddDays(-1)).Count == 0, "Exclude past dates");

        var walkIn = service.BookAppointment(Patient, 101, tomorrow.AddHours(10), BookingMethod.InClinic);
        var phone = service.BookAppointment(Patient, 101, tomorrow.AddHours(11), BookingMethod.ByPhone);
        Check(walkIn.Method == BookingMethod.InClinic && phone.Method == BookingMethod.ByPhone,
            "Preserve booking method");
        Check(walkIn.Patient == Patient && walkIn.ProfessionalId == 101 && walkIn.Id != phone.Id,
            "Preserve booking details and unique IDs");
        Check(service.SearchAvailability(101, tomorrow).Count == 6, "Remove booked slots");
        Check(service.SearchAvailability(102, tomorrow).Count == 8, "Separate professional schedules");
        Reject<InvalidOperationException>(() => service.BookAppointment(Patient, 101, walkIn.DateTime, BookingMethod.ByPhone));
        foreach (var time in new[] { Today.AddHours(9), tomorrow.AddHours(8), tomorrow.AddHours(17), tomorrow.AddHours(12).AddMinutes(30) })
            Reject<InvalidOperationException>(() => service.BookAppointment(Patient, 101, time, BookingMethod.InClinic));
        Reject<ArgumentNullException>(() => service.BookAppointment(null!, 101, tomorrow.AddHours(12), BookingMethod.ByPhone));
        Reject<ArgumentException>(() => service.BookAppointment(new Patient(), 101, tomorrow.AddHours(12), BookingMethod.ByPhone));
        Reject<ArgumentOutOfRangeException>(() => service.SearchAvailability(0, tomorrow));
        Reject<ArgumentOutOfRangeException>(() => service.BookAppointment(Patient, 0, tomorrow.AddHours(12), BookingMethod.ByPhone));
        Reject<ArgumentOutOfRangeException>(() => service.BookAppointment(Patient, 101, tomorrow.AddHours(12), (BookingMethod)99));

        var attempts = new ConcurrentBag<Appointment>();
        Parallel.For(0, 20, _ =>
        {
            try { attempts.Add(service.BookAppointment(Patient, 101, tomorrow.AddHours(12), BookingMethod.ByPhone)); }
            catch (InvalidOperationException) { }
        });
        Check(attempts.Count == 1, "Only one concurrent booking succeeds");
        Console.WriteLine("All appointment workflow checks passed.");
    }

    private static void Check(bool condition, string description)
    {
        if (!condition) throw new Exception(description);
    }

    private static void Reject<T>(Action action) where T : Exception
    {
        try { action(); }
        catch (T) { return; }
        throw new Exception($"Expected {typeof(T).Name}.");
    }
}
