namespace MediCore.Services
{
    // Converts between hospital local time (e.g. Pakistan, UTC+5) and UTC,
    // and builds the list of bookable time slots for a day.
    public class HospitalSchedule
    {
        public int UtcOffsetHours { get; }
        public int OpeningHour { get; }
        public int ClosingHour { get; }
        public int SlotMinutes { get; }

        public HospitalSchedule(IConfiguration config)
        {
            var s = config.GetSection("Hospital");
            UtcOffsetHours = s.GetValue("UtcOffsetHours", 5);
            OpeningHour = s.GetValue("OpeningHour", 9);
            ClosingHour = s.GetValue("ClosingHour", 17);
            SlotMinutes = s.GetValue("SlotMinutes", 30);
        }

        public DateTime ToLocal(DateTime utc) => DateTime.SpecifyKind(utc, DateTimeKind.Utc).AddHours(UtcOffsetHours);

        public DateTime LocalToUtc(DateTime local) => DateTime.SpecifyKind(local.AddHours(-UtcOffsetHours), DateTimeKind.Utc);

        // All slot start times (UTC) for a local calendar date
        public IEnumerable<DateTime> SlotsFor(DateOnly localDate)
        {
            var start = localDate.ToDateTime(new TimeOnly(OpeningHour, 0));
            var end = localDate.ToDateTime(new TimeOnly(ClosingHour, 0));
            for (var t = start; t < end; t = t.AddMinutes(SlotMinutes))
                yield return LocalToUtc(t);
        }

        public bool IsValidSlot(DateTime utc)
        {
            var local = ToLocal(utc);
            var minutesFromOpen = (local.Hour - OpeningHour) * 60 + local.Minute;
            return local.Second == 0
                && minutesFromOpen >= 0
                && local.Hour < ClosingHour
                && minutesFromOpen % SlotMinutes == 0;
        }
    }
}
