using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using ContactsBook.Models;

namespace ContactsBook.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Contact> Contacts { get; set; }
        public DbSet<Email> Emails { get; set; }
        public DbSet<Phone> Phones { get; set; }
        public DbSet<Address> Addresses { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure relationships
            modelBuilder.Entity<Email>()
                .HasOne(e => e.Contact)
                .WithMany(c => c.Emails)
                .HasForeignKey(e => e.ContactId);

            modelBuilder.Entity<Phone>()
                .HasOne(p => p.Contact)
                .WithMany(c => c.Phones)
                .HasForeignKey(p => p.ContactId);

            modelBuilder.Entity<Address>()
                .HasOne(a => a.Contact)
                .WithMany(c => c.Addresses)
                .HasForeignKey(a => a.ContactId);
        }
    }

    // Custom user class for Identity
    public class ApplicationUser : IdentityUser
    {
        // Add additional user properties here if needed
        public string FirstName { get; set; }
        public string LastName { get; set; }
    }
}