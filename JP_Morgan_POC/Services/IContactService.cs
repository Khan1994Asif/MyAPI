using JP_Morgan_POC.Model;

namespace JP_Morgan_POC.Services
{
    public interface IContactService
    {
        Task<IEnumerable<EmpContactDetails>> GetAllContactsAsync();
        Task<EmpContactDetails?> GetContactByIdAsync(int id);
        Task<EmpContactDetails> CreateContactAsync(EmpContactDetails contact);
    }
}
