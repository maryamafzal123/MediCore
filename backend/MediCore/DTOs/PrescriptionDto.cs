using System.ComponentModel.DataAnnotations;

namespace MediCore.DTOs
{
    public class CreatePrescriptionDto
    {
        [Required(ErrorMessage = "Appointment is required")]
        public int AppointmentId { get; set; }

        // Optional: taken from the appointment itself, kept for backward compatibility
        public int PatientId { get; set; }

        [Required(ErrorMessage = "Diagnosis is required")]
        [StringLength(500, ErrorMessage = "Diagnosis cannot exceed 500 characters")]
        public string Diagnosis { get; set; } = string.Empty;

        [Required(ErrorMessage = "Medications are required")]
        public string Medications { get; set; } = string.Empty;

        [Required(ErrorMessage = "Instructions are required")]
        public string Instructions { get; set; } = string.Empty;

        [Required(ErrorMessage = "Valid until date is required")]
        [FutureDate(ErrorMessage = "Valid until date must be in the future")]
        public DateTime ValidUntil { get; set; }
    }

    public class PrescriptionResponseDto
    {
        public int Id { get; set; }
        public int AppointmentId { get; set; }
        public string Diagnosis { get; set; } = string.Empty;
        public string Medications { get; set; } = string.Empty;
        public string Instructions { get; set; } = string.Empty;
        public DateTime ValidUntil { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public string DoctorName { get; set; } = string.Empty;
        public DateTime AppointmentDate { get; set; }
    }
}