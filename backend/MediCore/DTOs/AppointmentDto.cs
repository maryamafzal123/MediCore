using System.ComponentModel.DataAnnotations;
using MediCore.Models;

namespace MediCore.DTOs
{
    public class CreateAppointmentDto
    {
        [Required(ErrorMessage = "Doctor is required")]
        public int DoctorId { get; set; }

        [Required(ErrorMessage = "Appointment date is required")]
        [FutureDate(ErrorMessage = "Appointment date must be in the future")]
        public DateTime AppointmentDate { get; set; }

        [StringLength(500, ErrorMessage = "Notes cannot exceed 500 characters")]
        public string Notes { get; set; } = string.Empty;
    }

    public class AppointmentResponseDto
    {
        public int Id { get; set; }
        public int DoctorId { get; set; }
        public int PatientId { get; set; }
        public bool HasPrescription { get; set; }
        public DateTime AppointmentDate { get; set; }
        public string Notes { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string DoctorName { get; set; } = string.Empty;
        public string PatientName { get; set; } = string.Empty;
        public string Specialization { get; set; } = string.Empty;
    }

    public class UpdateAppointmentStatusDto
    {
        [Required(ErrorMessage = "Status is required")]
        public AppointmentStatus Status { get; set; }
    }

    public class TimeSlotDto
    {
        public DateTime StartUtc { get; set; }
        public string LocalTime { get; set; } = string.Empty;   // "09:30"
        public bool IsAvailable { get; set; }
    }

    // Custom validation attribute for future dates
    public class FutureDateAttribute : ValidationAttribute
    {
        public override bool IsValid(object? value)
        {
            if (value is DateTime date)
                return date > DateTime.UtcNow;
            return false;
        }
    }
}