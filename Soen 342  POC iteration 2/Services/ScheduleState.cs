using System.Collections.Generic;

// Both services use the same state and lock so checking and booking stay atomic.
internal sealed class ScheduleState
{
    internal List<MedicalProfessional> Professionals { get; } = new();
    internal List<Availability> Availabilities { get; } = new();
    internal List<Appointment> Appointments { get; } = new();
    internal object Sync { get; } = new();
}
