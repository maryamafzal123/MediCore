namespace MediCore.DTOs
{
    // Data we RECEIVE when creating a prescription
    public class CreatePrescriptionDto
    {
        public int AppointmentId { get; set; }
        public int PatientId { get; set; }
        public string Diagnosis { get; set; } = string.Empty;
        public string Medications { get; set; } = string.Empty;
        public string Instructions { get; set; } = string.Empty;
        public DateTime ValidUntil { get; set; }
    }

    // Data we SEND BACK when returning prescription info
    public class PrescriptionResponseDto
    {
        public int Id { get; set; }
        public string Diagnosis { get; set; } = string.Empty;
        public string Medications { get; set; } = string.Empty;
        public string Instructions { get; set; } = string.Empty;
        public DateTime ValidUntil { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public string DoctorName { get; set; } = string.Empty;
        public DateTime AppointmentDate { get; set; }
    }
}