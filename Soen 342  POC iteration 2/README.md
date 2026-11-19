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

Availability starts empty. `CreateAvailability` stores a slot for a positive
professional ID with a start and end time, rejecting invalid time ranges and
overlapping slots for the same professional. Adjacent slots are allowed.
Searching returns future, unbooked slot start times; booking reserves one whole
slot. `CreateRepeatingAvailability` repeats selected weekdays across an inclusive
date range, dividing each day's hours into equal slots. Daily times must be
within one day (end before midnight), and the positive slot duration must divide
the daily range exactly. Any overlap rejects the entire schedule. A range with
no matching weekdays creates no slots.

For example, Mondays in November with 30-minute slots from 09:00 to 12:00:

```csharp
service.CreateRepeatingAvailability(
    101, new DateTime(2026, 11, 1), new DateTime(2026, 11, 30),
    new[] { DayOfWeek.Monday }, TimeSpan.FromHours(9),
    TimeSpan.FromHours(12), TimeSpan.FromMinutes(30));
```

The demo creates 30-minute slots from 09:00 to 12:00 on tomorrow's weekday
for four weeks. Times use the clinic computer's local time. Monthly recurrence,
CSV import/export, and a professional directory are not implemented yet.

Data is held in memory for one service instance and is lost when the application
exits. Both booking methods follow the same availability rules. This iteration
does not include a user interface, database, cancellation, or authentication.
