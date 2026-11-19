using System;

public class Appointment
{
    public int Id { get; init; }
    public Patient Patient { get; init; } = null!;
    public int ProfessionalId { get; init; }
    public DateTime DateTime { get; init; }
    public BookingMethod Method { get; init; }
}
