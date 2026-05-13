using MediCore.Models;

namespace MediCore.DTOs
{
    // Data we RECEIVE when user registers
    // Like Django's RegisterSerializer
    public class RegisterDto
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public UserRole Role { get; set; }
    }
}