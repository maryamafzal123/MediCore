namespace MediCore.Models
{
    public enum AppointmentStatus
    {
        Scheduled,
        Completed,
        Cancelled
    }

    public class Appointment : BaseEntity
    {
        public DateTime AppointmentDate { get; set; }
        public string Notes { get; set; } = string.Empty;
        public AppointmentStatus Status { get; set; } = AppointmentStatus.Scheduled;

        // Foreign Keys
        public int DoctorId { get; set; }
        public Doctor Doctor { get; set; } = null!;

        public int PatientId { get; set; }
        public Patient Patient { get; set; } = null!;

        public Prescription? Prescription { get; set; }
    }
}