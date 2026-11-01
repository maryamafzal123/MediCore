namespace MediCore.Models
{
    public enum UserRole
    {
        Admin,
        Doctor,
        Patient
    }

    public class User : BaseEntity
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public UserRole Role { get; set; }
        public bool IsActive { get; set; } = true;

        public Doctor? Doctor { get; set; }
        public Patient? Patient { get; set; }
    }
}