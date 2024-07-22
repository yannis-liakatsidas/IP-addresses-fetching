using Microsoft.EntityFrameworkCore;

namespace IpAddressesAPI.Models
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Models.IPAddress> IPAddresses { get; set; }
        public DbSet<Country> Countries { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Configure IpAddress entity
            modelBuilder.Entity<IPAddress>()
                .ToTable("IPAddresses")  // Map to table IPAddresses
                .HasKey(ip => ip.Id);

            modelBuilder.Entity<IPAddress>()
                .HasOne(ip => ip.Country)
                .WithMany(c => c.IPAddresses) // Ensure Country has a collection of IpAddresses if needed
                .HasForeignKey(ip => ip.CountryId)
                .OnDelete(DeleteBehavior.Restrict); // Configure delete behavior

            // Configure Country entity
            modelBuilder.Entity<Country>()
                .ToTable("Countries")  // Map to table Countries
                .HasKey(c => c.Id);
        }
    }
}
