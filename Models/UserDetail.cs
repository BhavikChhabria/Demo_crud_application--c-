using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MiddlewareDemo.Models
{
    public class UserDetail
    {
        [Key]
        public int Id { get; set; }  // 🔹 Primary Key

        [Required]
        [ForeignKey("User")]
        public int UserId { get; set; }  // 🔹 FK to Users Table

        [Required(ErrorMessage = "City is required.")]
        public string City { get; set; } = string.Empty;

        [Required(ErrorMessage = "Pincode is required.")]
        public string Pincode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mobile Number is required.")]
        public string MobileNumber { get; set; } = string.Empty;

        public DateTime LastLoginUser { get; set; } = DateTime.UtcNow;


        public virtual User User { get; set; } = null!;
    }
}
