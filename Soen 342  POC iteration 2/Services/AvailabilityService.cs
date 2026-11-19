using System;
using System.Collections.Generic;
using System.Linq;

public class AvailabilityService
{
    internal ScheduleState State { get; } = new();
    private readonly Func<DateTime> now;

    public AvailabilityService(Func<DateTime>? now = null)
    {
        this.now = now ?? (() => DateTime.Now);
    }

    public Availability CreateAvailability(int professionalId, DateTime start, DateTime end)
    {
        if (professionalId <= 0)
            throw new ArgumentOutOfRangeException(nameof(professionalId));
        if (end <= start)
            throw new ArgumentException("End must be after start.", nameof(end));

        lock (State.Sync)
        {
            if (State.Availabilities.Any(a => a.ProfessionalId == professionalId &&
                start < a.End && end > a.Start))
                throw new InvalidOperationException("The availability overlaps an existing slot.");

            var availability = new Availability
            {
                Id = State.Availabilities.Count + 1,
                ProfessionalId = professionalId,
                Start = start,
                End = end
            };
            State.Availabilities.Add(availability);
            return availability;
        }
    }

    // Dates are inclusive. Each selected weekday repeats throughout the range.
    public List<Availability> CreateRepeatingAvailability(
        int professionalId, DateTime startDate, DateTime endDate,
        IEnumerable<DayOfWeek> daysOfWeek, TimeSpan startTime,
        TimeSpan endTime, TimeSpan slotDuration)
    {
        if (professionalId <= 0)
            throw new ArgumentOutOfRangeException(nameof(professionalId));
        if (endDate.Date < startDate.Date)
            throw new ArgumentException("End date must not precede start date.", nameof(endDate));
        ArgumentNullException.ThrowIfNull(daysOfWeek);
        var days = daysOfWeek.ToHashSet();
        if (days.Count == 0 || days.Any(day => !Enum.IsDefined(day)))
            throw new ArgumentException("Select at least one valid weekday.", nameof(daysOfWeek));
        if (startTime < TimeSpan.Zero || endTime >= TimeSpan.FromDays(1) || endTime <= startTime)
            throw new ArgumentException("Daily times must be within one day, with end after start.");
        if (slotDuration <= TimeSpan.Zero || (endTime - startTime).Ticks % slotDuration.Ticks != 0)
            throw new ArgumentException("A positive slot duration must divide the daily time range exactly.", nameof(slotDuration));

        var slots = new List<(DateTime Start, DateTime End)>();
        for (var date = startDate.Date; ; date = date.AddDays(1))
        {
            if (days.Contains(date.DayOfWeek))
                for (var time = startTime; time < endTime; time += slotDuration)
                    slots.Add((date.Add(time), date.Add(time + slotDuration)));
            if (date == endDate.Date)
                break;
        }

        lock (State.Sync)
        {
            // Check the whole schedule before saving so conflicts cannot leave a partial schedule.
            if (slots.Any(slot => State.Availabilities.Any(a =>
                a.ProfessionalId == professionalId && slot.Start < a.End && slot.End > a.Start)))
                throw new InvalidOperationException("The repeating schedule overlaps an existing slot.");

            return slots.Select(slot => CreateAvailability(professionalId, slot.Start, slot.End)).ToList();
        }
    }

    // Use case 1: Search for Availability
    public List<DateTime> SearchAvailability(
        int professionalId, DateTime date)
    {
        if (professionalId <= 0)
            throw new ArgumentOutOfRangeException(nameof(professionalId));

        lock (State.Sync)
        {
            return FindAvailableSlots(professionalId, date, now());
        }
    }

    private List<DateTime> FindAvailableSlots(
        int professionalId, DateTime date, DateTime currentTime)
    {
        return State.Availabilities
            .Where(slot => slot.ProfessionalId == professionalId &&
                slot.Start.Date == date.Date && slot.Start > currentTime &&
                !State.Appointments.Any(a => a.ProfessionalId == professionalId &&
                    a.DateTime == slot.Start))
            .OrderBy(slot => slot.Start)
            .Select(slot => slot.Start)
            .ToList();
    }

}
