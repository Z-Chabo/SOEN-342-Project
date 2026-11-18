# Appointment booking proof of concept

This iteration supports booking an appointment for a patient at the clinic or
over the phone, and searching a professional's availability directly or during
booking.

- `Patient` identifies the patient.
- `Appointment` stores the patient, professional ID, time, and booking method.
- `BookingMethod` distinguishes `InClinic` and `ByPhone` bookings.
- `AppointmentService` searches available slots and reserves an available slot.
  Booking checks and reservation occur under one lock to prevent two callers
  from reserving the same professional and time in a service instance.

## Run

Requires the .NET 10 SDK. Run the demonstration with:

```powershell
dotnet run --project ATS.csproj
```

Run the dependency-free workflow checks with:

```powershell
dotnet run --project tests/ATS.Tests.csproj
```

## Iteration assumptions

Appointments last one hour, starting on the hour from 09:00 through 16:00
every day. Times use the clinic computer's local time. Only future slots may
be booked. The service accepts positive professional IDs; a professional
directory, holidays, and individual working schedules are outside this iteration.

Data is held in memory for one service instance and is lost when the application
exits. Both booking methods follow the same availability rules. This iteration
does not include a user interface, database, cancellation, or authentication.
