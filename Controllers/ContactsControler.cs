using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using ContactsBook.Data;
using ContactsBook.Models;
using System.IO;
using System.Text;
using ClosedXML.Excel;

namespace ContactsBook.Controllers
{
    [Authorize]
    public class ContactsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ContactsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Contacts
        public async Task<IActionResult> Index(string searchString)
        {
            var contacts = _context.Contacts
                .Include(c => c.Emails)
                .Include(c => c.Phones)
                .Include(c => c.Addresses)
                .AsQueryable();

            if (!String.IsNullOrEmpty(searchString))
            {
                contacts = contacts.Where(c => 
                    c.FirstName.Contains(searchString) || 
                    c.LastName.Contains(searchString) ||
                    c.Emails.Any(e => e.EmailAddress.Contains(searchString)) ||
                    c.Phones.Any(p => p.PhoneNumber.Contains(searchString)) ||
                    c.Addresses.Any(a => a.StreetAddress.Contains(searchString) || 
                                        a.City.Contains(searchString) || 
                                        a.State.Contains(searchString))
                );
            }

            return View(await contacts.ToListAsync());
        }

        // GET: Contacts/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var contact = await _context.Contacts
                .Include(c => c.Emails)
                .Include(c => c.Phones)
                .Include(c => c.Addresses)
                .FirstOrDefaultAsync(m => m.Id == id);
                
            if (contact == null)
            {
                return NotFound();
            }

            return View(contact);
        }

        // GET: Contacts/Create
        [Authorize(Roles = "Admin,User")]
        public IActionResult Create()
        {
            return View();
        }

        // POST: Contacts/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,User")]
        public async Task<IActionResult> Create([Bind("Id,FirstName,LastName")] Contact contact, 
            string[] emailAddresses, string[] phoneNumbers, string[] phoneTypes,
            string[] streetAddresses, string[] cities, string[] states, string[] zipCodes, string[] countries, string[] addressTypes)
        {
            if (ModelState.IsValid)
            {
                // Add the contact
                _context.Add(contact);
                await _context.SaveChangesAsync();

                // Add emails
                for (int i = 0; i < emailAddresses.Length; i++)
                {
                    if (!string.IsNullOrWhiteSpace(emailAddresses[i]))
                    {
                        _context.Emails.Add(new Email
                        {
                            EmailAddress = emailAddresses[i],
                            ContactId = contact.Id
                        });
                    }
                }

                // Add phones
                for (int i = 0; i < phoneNumbers.Length; i++)
                {
                    if (!string.IsNullOrWhiteSpace(phoneNumbers[i]))
                    {
                        _context.Phones.Add(new Phone
                        {
                            PhoneNumber = phoneNumbers[i],
                            PhoneType = phoneTypes.Length > i ? phoneTypes[i] : "Unknown",
                            ContactId = contact.Id
                        });
                    }
                }

                // Add addresses
                for (int i = 0; i < streetAddresses.Length; i++)
                {
                    if (!string.IsNullOrWhiteSpace(streetAddresses[i]))
                    {
                        _context.Addresses.Add(new Address
                        {
                            StreetAddress = streetAddresses[i],
                            City = cities.Length > i ? cities[i] : "",
                            State = states.Length > i ? states[i] : "",
                            ZipCode = zipCodes.Length > i ? zipCodes[i] : "",
                            Country = countries.Length > i ? countries[i] : "",
                            AddressType = addressTypes.Length > i ? addressTypes[i] : "Unknown",
                            ContactId = contact.Id
                        });
                    }
                }

                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(contact);
        }

        // GET: Contacts/Edit/5
        [Authorize(Roles = "Admin,User")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var contact = await _context.Contacts
                .Include(c => c.Emails)
                .Include(c => c.Phones)
                .Include(c => c.Addresses)
                .FirstOrDefaultAsync(m => m.Id == id);
                
            if (contact == null)
            {
                return NotFound();
            }
            
            return View(contact);
        }

        // POST: Contacts/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,User")]
        public async Task<IActionResult> Edit(int id, [Bind("Id,FirstName,LastName")] Contact contact,
            int[] emailIds, string[] emailAddresses,
            int[] phoneIds, string[] phoneNumbers, string[] phoneTypes,
            int[] addressIds, string[] streetAddresses, string[] cities, string[] states, string[] zipCodes, string[] countries, string[] addressTypes)
        {
            if (id != contact.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Update the contact
                    _context.Update(contact);

                    // Handle emails
                    var existingEmails = await _context.Emails.Where(e => e.ContactId == contact.Id).ToListAsync();
                    
                    // Delete emails not in the form
                    foreach (var email in existingEmails)
                    {
                        if (!emailIds.Contains(email.Id))
                        {
                            _context.Emails.Remove(email);
                        }
                    }
                    
                    // Update or add emails
                    for (int i = 0; i < emailAddresses.Length; i++)
                    {
                        if (!string.IsNullOrWhiteSpace(emailAddresses[i]))
                        {
                            if (i < emailIds.Length && emailIds[i] > 0)
                            {
                                var email = await _context.Emails.FindAsync(emailIds[i]);
                                if (email != null)
                                {
                                    email.EmailAddress = emailAddresses[i];
                                    _context.Update(email);
                                }
                            }
                            else
                            {
                                _context.Emails.Add(new Email
                                {
                                    EmailAddress = emailAddresses[i],
                                    ContactId = contact.Id
                                });
                            }
                        }
                    }

                    // Handle phones using the same pattern
                    var existingPhones = await _context.Phones.Where(p => p.ContactId == contact.Id).ToListAsync();
                    
                    foreach (var phone in existingPhones)
                    {
                        if (!phoneIds.Contains(phone.Id))
                        {
                            _context.Phones.Remove(phone);
                        }
                    }
                    
                    for (int i = 0; i < phoneNumbers.Length; i++)
                    {
                        if (!string.IsNullOrWhiteSpace(phoneNumbers[i]))
                        {
                            if (i < phoneIds.Length && phoneIds[i] > 0)
                            {
                                var phone = await _context.Phones.FindAsync(phoneIds[i]);
                                if (phone != null)
                                {
                                    phone.PhoneNumber = phoneNumbers[i];
                                    phone.PhoneType = phoneTypes.Length > i ? phoneTypes[i] : "Unknown";
                                    _context.Update(phone);
                                }
                            }
                            else
                            {
                                _context.Phones.Add(new Phone
                                {
                                    PhoneNumber = phoneNumbers[i],
                                    PhoneType = phoneTypes.Length > i ? phoneTypes[i] : "Unknown",
                                    ContactId = contact.Id
                                });
                            }
                        }
                    }

                    // Handle addresses using the same pattern
                    var existingAddresses = await _context.Addresses.Where(a => a.ContactId == contact.Id).ToListAsync();
                    
                    foreach (var address in existingAddresses)
                    {
                        if (!addressIds.Contains(address.Id))
                        {
                            _context.Addresses.Remove(address);
                        }
                    }
                    
                    for (int i = 0; i < streetAddresses.Length; i++)
                    {
                        if (!string.IsNullOrWhiteSpace(streetAddresses[i]))
                        {
                            if (i < addressIds.Length && addressIds[i] > 0)
                            {
                                var address = await _context.Addresses.FindAsync(addressIds[i]);
                                if (address != null)
                                {
                                    address.StreetAddress = streetAddresses[i];
                                    address.City = cities.Length > i ? cities[i] : "";
                                    address.State = states.Length > i ? states[i] : "";
                                    address.ZipCode = zipCodes.Length > i ? zipCodes[i] : "";
                                    address.Country = countries.Length > i ? countries[i] : "";
                                    address.AddressType = addressTypes.Length > i ? addressTypes[i] : "Unknown";
                                    _context.Update(address);
                                }
                            }
                            else
                            {
                                _context.Addresses.Add(new Address
                                {
                                    StreetAddress = streetAddresses[i],
                                    City = cities.Length > i ? cities[i] : "",
                                    State = states.Length > i ? states[i] : "",
                                    ZipCode = zipCodes.Length > i ? zipCodes[i] : "",
                                    Country = countries.Length > i ? countries[i] : "",
                                    AddressType = addressTypes.Length > i ? addressTypes[i] : "Unknown",
                                    ContactId = contact.Id
                                });
                            }
                        }
                    }

                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ContactExists(contact.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(contact);
        }

        // GET: Contacts/Delete/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var contact = await _context.Contacts
                .Include(c => c.Emails)
                .Include(c => c.Phones)
                .Include(c => c.Addresses)
                .FirstOrDefaultAsync(m => m.Id == id);
                
            if (contact == null)
            {
                return NotFound();
            }

            return View(contact);
        }

        // POST: Contacts/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var contact = await _context.Contacts.FindAsync(id);
            if (contact != null)
            {
                _context.Contacts.Remove(contact);
                await _context.SaveChangesAsync();
            }
            
            return RedirectToAction(nameof(Index));
        }

        private bool ContactExists(int id)
        {
            return _context.Contacts.Any(e => e.Id == id);
        }

        // Export to CSV
        public async Task<IActionResult> ExportToCsv(string searchString)
        {
            var contacts = _context.Contacts
                .Include(c => c.Emails)
                .Include(c => c.Phones)
                .Include(c => c.Addresses)
                .AsQueryable();

            if (!String.IsNullOrEmpty(searchString))
            {
                contacts = contacts.Where(c => 
                    c.FirstName.Contains(searchString) || 
                    c.LastName.Contains(searchString) ||
                    c.Emails.Any(e => e.EmailAddress.Contains(searchString)) ||
                    c.Phones.Any(p => p.PhoneNumber.Contains(searchString)) ||
                    c.Addresses.Any(a => a.StreetAddress.Contains(searchString) || 
                                        a.City.Contains(searchString) || 
                                        a.State.Contains(searchString))
                );
            }

            var contactsList = await contacts.ToListAsync();

            var builder = new StringBuilder();
            builder.AppendLine("Id,FirstName,LastName,Emails,Phones,Addresses");

            foreach (var contact in contactsList)
            {
                var emails = string.Join("|", contact.Emails.Select(e => e.EmailAddress));
                var phones = string.Join("|", contact.Phones.Select(p => $"{p.PhoneType}: {p.PhoneNumber}"));
                var addresses = string.Join("|", contact.Addresses.Select(a => 
                    $"{a.AddressType}: {a.StreetAddress}, {a.City}, {a.State} {a.ZipCode}, {a.Country}".Trim(' ', ',')
                ));

                builder.AppendLine($"{contact.Id},{contact.FirstName},{contact.LastName},\"{emails}\",\"{phones}\",\"{addresses}\"");
            }

            return File(Encoding.UTF8.GetBytes(builder.ToString()), "text/csv", "contacts.csv");
        }
        public async Task<IActionResult> ExportToExcel(string searchString)
{
    var contacts = _context.Contacts
        .Include(c => c.Emails)
        .Include(c => c.Phones)
        .Include(c => c.Addresses)
        .AsQueryable();

    if (!String.IsNullOrEmpty(searchString))
    {
        contacts = contacts.Where(c => 
            c.FirstName.Contains(searchString) || 
            c.LastName.Contains(searchString) ||
            c.Emails.Any(e => e.EmailAddress.Contains(searchString)) ||
            c.Phones.Any(p => p.PhoneNumber.Contains(searchString)) ||
            c.Addresses.Any(a => a.StreetAddress.Contains(searchString) || 
                                a.City.Contains(searchString) || 
                                a.State.Contains(searchString))
        );
    }

    var contactsList = await contacts.ToListAsync();

    using (var workbook = new XLWorkbook())
    {
        var worksheet = workbook.Worksheets.Add("Contacts");
        var currentRow = 1;
        worksheet.Cell(currentRow, 1).Value = "Id";
        worksheet.Cell(currentRow, 2).Value = "FirstName";
        worksheet.Cell(currentRow, 3).Value = "LastName";
        worksheet.Cell(currentRow, 4).Value = "Emails";
        worksheet.Cell(currentRow, 5).Value = "Phones";
        worksheet.Cell(currentRow, 6).Value = "Addresses";

        foreach (var contact in contactsList)
        {
            currentRow++;
            worksheet.Cell(currentRow, 1).Value = contact.Id;
            worksheet.Cell(currentRow, 2).Value = contact.FirstName;
            worksheet.Cell(currentRow, 3).Value = contact.LastName;
            worksheet.Cell(currentRow, 4).Value = string.Join("|", contact.Emails.Select(e => e.EmailAddress));
            worksheet.Cell(currentRow, 5).Value = string.Join("|", contact.Phones.Select(p => $"{p.PhoneType}: {p.PhoneNumber}"));
            worksheet.Cell(currentRow, 6).Value = string.Join("|", contact.Addresses.Select(a => 
                $"{a.AddressType}: {a.StreetAddress}, {a.City}, {a.State} {a.ZipCode}, {a.Country}".Trim(' ', ',')
            ));
        }

        using (var stream = new MemoryStream())
        {
            workbook.SaveAs(stream);
            var content = stream.ToArray();
            return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "contacts.xlsx");
        }
    }
}
    }
}

        
       