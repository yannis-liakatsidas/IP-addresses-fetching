using System.ComponentModel.DataAnnotations;

namespace IpAddressesAPI.Models
{
    public class Country
    {
        [Required]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string Name { get; set; } = null!;

        [Required]
        [Length(minimumLength: 2, maximumLength: 2)]
        public string TwoLetterCode { get; set; } = null!;

        [Required]
        [Length(minimumLength: 3, maximumLength: 3)]
        public string ThreeLetterCode { get; set; } = null!;

        [Required]
        public DateTime CreatedAt { get; set; }

        // Navigation Property
        public ICollection<IPAddress> IPAddresses { get; set; } = new List<IPAddress>();
    }
}
