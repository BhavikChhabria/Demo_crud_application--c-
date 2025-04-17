namespace MiddlewareDemo.Models
{
    public class UserDTO
    {
        public int Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string City { get; set; }
        public string Pincode { get; set; }
        public string MobileNumber { get; set; }
        public DateTime? LastLoginUser { get; set; }
        public int LoginCount { get; set; }

        // ✅ Constructor to handle User & UserDetail
        public UserDTO(User user, UserDetail userDetail)
        {
            Id = user.Id;
            FirstName = user.FirstName;
            LastName = user.LastName;
            Email = user.Email;
            LoginCount = user.LoginCount;

            City = userDetail?.City ?? string.Empty;
            Pincode = userDetail?.Pincode ?? string.Empty;
            MobileNumber = userDetail?.MobileNumber ?? string.Empty;
            LastLoginUser = userDetail?.LastLoginUser ?? DateTime.UtcNow;
        }
    }
}
