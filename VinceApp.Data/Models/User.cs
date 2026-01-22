using System.ComponentModel.DataAnnotations;

namespace VinceApp.Data.Models
{
    public enum UserRole
    {
        Disabled = 0,
        Admin = 1,
        Manager = 2,
        Cashier = 3,
    }
    public class User
    {

        

        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string Username { get; set; } 

        [Required]
        public string PasswordHash { get; set; }

        [Required] 
        [EmailAddress] 
        [MaxLength(256)]
        public string EmailAddress { get; set; }

        public bool IsEmailConfirmed { get; set; } = false;

        public UserRole Role { get; set; }
        [Required]
        [MaxLength(100)]
        public string FullName { get; set; }
    }
}