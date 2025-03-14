using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ContactsBook.Models
{
    public class Contact
    {
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string FirstName { get; set; }

        [Required]
        [StringLength(50)]
        public string LastName { get; set; }

        // Navigation properties
        public List<Email> Emails { get; set; } = new List<Email>();
        public List<Phone> Phones { get; set; } = new List<Phone>();
        public List<Address> Addresses { get; set; } = new List<Address>();
    }

    public class Email
    {
        public int Id { get; set; }

        [Required]
        [EmailAddress]
        [StringLength(100)]
        public string EmailAddress { get; set; }

        // Foreign key
        public int ContactId { get; set; }
        public Contact Contact { get; set; }
    }

    public class Phone
    {
        public int Id { get; set; }

        [Required]
        [Phone]
        [StringLength(20)]
        public string PhoneNumber { get; set; }

        [StringLength(50)]
        public string PhoneType { get; set; } // e.g., Home, Work, Mobile

        // Foreign key
        public int ContactId { get; set; }
        public Contact Contact { get; set; }
    }

    public class Address
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string StreetAddress { get; set; }

        [StringLength(50)]
        public string City { get; set; }

        [StringLength(50)]
        public string State { get; set; }

        [StringLength(20)]
        public string ZipCode { get; set; }

        [StringLength(50)]
        public string Country { get; set; }

        [StringLength(50)]
        public string AddressType { get; set; } // e.g., Home, Work

        // Foreign key
        public int ContactId { get; set; }
        public Contact Contact { get; set; }
    }
}