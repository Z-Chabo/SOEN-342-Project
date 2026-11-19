using System.Collections.Concurrent;

internal static class Tests
{
    private static readonly DateTime Today = new(2026, 11, 17);
    private static readonly Patient Patient = new() { Id = 1, Name = "John" };

    private static void Main()
    {
        CheckRepeatingAvailability();
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

    private static void CheckRepeatingAvailability()
    {
        var service = new AppointmentService(() => new DateTime(2026, 10, 31));
        var first = new DateTime(2026, 11, 1);
        var last = new DateTime(2026, 11, 30);
        var monday = new[] { DayOfWeek.Monday };
        var start = TimeSpan.FromHours(9);
        var end = TimeSpan.FromHours(12);
        var duration = TimeSpan.FromMinutes(30);
        var slots = service.CreateRepeatingAvailability(101, first, last, monday, start, end, duration);
        Check(slots.Count == 30 && slots.All(s => s.Start.DayOfWeek == DayOfWeek.Monday && s.End - s.Start == duration),
            "Five Mondays with six half-hour slots each");
        Check(slots.First().Start == first.AddDays(1).AddHours(9) && slots.Last().End == last.AddHours(12),
            "Inclusive date range and daily boundaries");
        Check(service.SearchAvailability(101, first.AddDays(2)).Count == 0, "No slots on unselected days");
        service.BookAppointment(Patient, 101, slots[0].Start, BookingMethod.ByPhone);
        Check(service.SearchAvailability(101, slots[0].Start.Date).Count == 5, "Book generated availability");

        var multiple = service.CreateRepeatingAvailability(102, first, first.AddDays(6),
            new[] { DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Monday }, start, end, TimeSpan.FromMinutes(45));
        Check(multiple.Count == 8, "Multiple weekdays, duplicate weekdays and 45-minute slots");
        Check(service.CreateRepeatingAvailability(103, first, first, monday, start, end, duration).Count == 0,
            "No matching weekdays returns no slots");
        Reject<ArgumentOutOfRangeException>(() => service.CreateRepeatingAvailability(0, first, last, monday, start, end, duration));
        Reject<ArgumentException>(() => service.CreateRepeatingAvailability(103, last, first, monday, start, end, duration));
        Reject<ArgumentNullException>(() => service.CreateRepeatingAvailability(103, first, last, null!, start, end, duration));
        foreach (var days in new[] { Array.Empty<DayOfWeek>(), new[] { (DayOfWeek)7 } })
            Reject<ArgumentException>(() => service.CreateRepeatingAvailability(103, first, last, days, start, end, duration));
        foreach (var invalid in new[] { TimeSpan.Zero, TimeSpan.FromMinutes(-30), TimeSpan.FromMinutes(40), TimeSpan.FromHours(4) })
            Reject<ArgumentException>(() => service.CreateRepeatingAvailability(103, first, last, monday, start, end, invalid));
        Reject<ArgumentException>(() => service.CreateRepeatingAvailability(103, first, last, monday, end, start, duration));
        Reject<ArgumentException>(() => service.CreateRepeatingAvailability(103, first, last, monday, TimeSpan.FromHours(-1), end, duration));
        Reject<ArgumentException>(() => service.CreateRepeatingAvailability(103, first, last, monday, start, TimeSpan.FromHours(25), duration));

        service.CreateAvailability(103, last.AddHours(10), last.AddHours(11));
        Reject<InvalidOperationException>(() => service.CreateRepeatingAvailability(103, first, last, monday, start, end, duration));
        Check(service.SearchAvailability(103, first.AddDays(1)).Count == 0 && service.SearchAvailability(103, last).Count == 1,
            "Late conflict leaves the entire schedule unchanged");
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
