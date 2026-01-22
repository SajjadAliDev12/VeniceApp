using System;
using System.ComponentModel.DataAnnotations;

namespace VinceApp.Data.Models
{
    public class AuditLog
    {
        public int Id { get; set; }

       
        [MaxLength(100)]
        public string UserFullName { get; set; }

        
        public DateTime Timestamp { get; set; } = DateTime.Now;

        
        [Required]
        [MaxLength(50)]
        public string Action { get; set; }

        
        [Required]
        [MaxLength(100)]
        public string TableName { get; set; }

        
        [Required]
        [MaxLength(100)]
        public string RecordId { get; set; }

        
        public string Changes { get; set; }
    }
}