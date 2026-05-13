using MediCore.Models;

namespace MediCore.DTOs
{
    // Data we RECEIVE when booking an appointment
    public class CreateAppointmentDto
    {
        public int DoctorId { get; set; }
        public DateTime AppointmentDate { get; set; }
        public string Notes { get; set; } = string.Empty;
    }

    // Data we SEND BACK when returning appointment info
    public class AppointmentResponseDto
    {
        public int Id { get; set; }
        public DateTime AppointmentDate { get; set; }
        public string Notes { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string DoctorName { get; set; } = string.Empty;
        public string PatientName { get; set; } = string.Empty;
        public string Specialization { get; set; } = string.Empty;
    }

    // Data we RECEIVE when updating appointment status
    public class UpdateAppointmentStatusDto
    {
        public AppointmentStatus Status { get; set; }
    }
}