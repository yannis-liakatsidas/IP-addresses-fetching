using System.ComponentModel.DataAnnotations;
using System.Net;

namespace IpAddressesAPI.Models
{
    public class IPAddress
    {
        [Required]
        public int Id { get; set; }
        
        [Required]
        public int CountryId { get; set; }

        [Required]
        public System.Net.IPAddress IP { get; set; } = null!;

        [Required]
        public DateTime CreatedAt { get; set; }

        [Required]
        public DateTime UpdatedAt { get; set; }

        // Navigation Property
        public Country Country { get; set; } = null!;
    }
}
