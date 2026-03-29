using DeliveryWebLoL.Models;
using System.Text.Json.Serialization;

namespace DeliveryWebLoL.DTO.Auth
{
    public class AuthRequest
    {
        public class LoginRequest
        {
            public string Username { get; set; }
            public string Password { get; set; }
        }

        public class RegisterRequest
        {
            public string Username { get; set; }
            public string Password { get; set; }
            public string PhoneNum { get; set; }
            public string? Email { get; set; }
            public int Role { get; set; } = 4;
        }

        public class SetRoleRequest
        {
            public string Username { get; set; } = null!;
            public int Role { get; set; }
            public string Extra { get; set; } = string.Empty;
        }
    }
    
}