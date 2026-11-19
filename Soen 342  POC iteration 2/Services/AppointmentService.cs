using System;

public class AppointmentService
{
    private readonly AvailabilityService availabilityService;
    private readonly ScheduleState state;

    public AppointmentService(AvailabilityService availabilityService)
    {
        ArgumentNullException.ThrowIfNull(availabilityService);
        this.availabilityService = availabilityService;
        state = availabilityService.State;
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
        lock (state.Sync)
        {
            // Indirectly use Search Availability.
            var available = availabilityService.SearchAvailability(
                professionalId, requestedTime.Date);

            if (!available.Contains(requestedTime))
            {
                throw new InvalidOperationException(
                    "The requested appointment is unavailable.");
            }

            Appointment appointment = new Appointment
            {
                Id = state.Appointments.Count + 1,
                Patient = patient,
                ProfessionalId = professionalId,
                DateTime = requestedTime,
                Method = method
            };

            state.Appointments.Add(appointment);

            return appointment;
        }
    }
}
