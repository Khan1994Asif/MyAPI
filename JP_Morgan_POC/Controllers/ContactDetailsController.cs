using JP_Morgan_POC.Model;
using JP_Morgan_POC.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace JP_Morgan_POC.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ContactDetailsController : ControllerBase
    {
        private readonly IContactService _contactService;
        public ContactDetailsController(IContactService contactService)
        {
            _contactService = contactService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<EmpContactDetails>>> GetContacts()
        {
            var contacts = await _contactService.GetAllContactsAsync();
            return Ok(contacts);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<EmpContactDetails>> GetContact(int id)
        {
            var contact = await _contactService.GetContactByIdAsync(id);
            if (contact == null)
            {
                return NotFound(new { message = $"Contact with ID {id} not found." });
            }
            return Ok(contact);
        }

        [HttpPost]
        public async Task<ActionResult<EmpContactDetails>> CreateContact([FromBody] EmpContactDetailsDTO contact)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = new EmpContactDetails()
            {
                EmpAddress = contact.EmpAddress,
                EmpProfile = contact.EmpProfile,
                EmpFirstName = contact.EmpFirstName,
                EmpLastName = contact.EmpLastName,
                SynStatus = "Pending",
                CreatedAt = DateTime.Now
            };

            var createdContact = await _contactService.CreateContactAsync(result);
            return CreatedAtAction(nameof(GetContact), new { id = createdContact.Id }, createdContact);
        }
    }
}
