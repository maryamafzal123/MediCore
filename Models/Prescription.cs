namespace MediCore.Models
{
    public class Prescription : BaseEntity
    {
        public string Diagnosis { get; set; } = string.Empty;
        public string Medications { get; set; } = string.Empty;
        public string Instructions { get; set; } = string.Empty;
        public DateTime ValidUntil { get; set; }

        public int AppointmentId { get; set; }
        public Appointment Appointment { get; set; } = null!;

        public int PatientId { get; set; }
        public Patient Patient { get; set; } = null!;
    }
}