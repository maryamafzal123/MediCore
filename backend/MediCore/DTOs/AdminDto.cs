namespace MediCore.DTOs
{
    public class AdminStatsDto
    {
        public int TotalDoctors { get; set; }
        public int TotalPatients { get; set; }
        public int TotalAppointments { get; set; }
        public int TodayAppointments { get; set; }
        public int TotalPrescriptions { get; set; }
        public Dictionary<string, int> AppointmentsByStatus { get; set; } = new();
        public Dictionary<string, int> DoctorsBySpecialization { get; set; } = new();
        public List<DailyCountDto> Last7Days { get; set; } = new();
    }

    public class DailyCountDto
    {
        public string Date { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    public class UserListDto
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class MeDto
    {
        public int UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public bool HasProfile { get; set; }
    }
}
