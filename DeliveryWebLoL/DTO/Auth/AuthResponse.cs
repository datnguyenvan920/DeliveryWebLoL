namespace DeliveryWebLoL.DTO.Auth
{
    public class AuthResponse
    {
        public class LoginResponse
        {
            public Guid UserID { get; set; }
            public string Username { get; set; } = null!;
            public int Role { get; set; }
            public string AccessToken { get; set; } = null!;
            public string RefreshToken { get; set; } = null!;
            public DateTime AccessTokenExpiresAt { get; set; }
            public DateTime RefreshTokenExpiresAt { get; set; }
        }

        public class RegisterResponse
        {
            public string Message { get; set; } = "Lỗi không thể tạo tài khoản";
            public bool Status { get; set; }
        }

    }
}
