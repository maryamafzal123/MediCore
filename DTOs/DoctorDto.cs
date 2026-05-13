namespace MediCore.DTOs
{
    // Data we RECEIVE when creating a doctor profile
    public class CreateDoctorDto
    {
        public string Specialization { get; set; } = string.Empty;
        public string LicenseNumber { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public decimal ConsultationFee { get; set; }
    }

    // Data we SEND BACK when returning doctor info
    public class DoctorResponseDto
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Specialization { get; set; } = string.Empty;
        public string LicenseNumber { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public decimal ConsultationFee { get; set; }
    }
}