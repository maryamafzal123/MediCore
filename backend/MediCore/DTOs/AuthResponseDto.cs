namespace MediCore.DTOs
{
    // Data we SEND BACK after successful login/register
    // Like Django's TokenObtainPairSerializer response
    public class AuthResponseDto
    {
        public string Token { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
    }
}