using JP_Morgan_POC.Model;

namespace JP_Morgan_POC.Repositories
{
    public interface IContactRepository
    {
        Task<IEnumerable<EmpContactDetails>> GetAllAsync();
        Task<EmpContactDetails?> GetByIdAsync(int id);
        Task AddAsync(EmpContactDetails contact);
        Task UpdateAsync(EmpContactDetails contact);
        Task SaveChangesAsync();
    }
}
