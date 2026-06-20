using JP_Morgan_POC.Model;
using JP_Morgan_POC.MyContext;
using Microsoft.EntityFrameworkCore;

namespace JP_Morgan_POC.Repositories
{
    public class ContactRepository : IContactRepository
    {
        private readonly ApplicationDbContext _context;

        public ContactRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<EmpContactDetails>> GetAllAsync()
        {
            try
            {
                return await _context.EmpContactDetails.ToListAsync();

            }
            catch (Exception ex)
            {

                throw;
            }
        }

        public async Task<EmpContactDetails?> GetByIdAsync(int id)
        {
            return await _context.EmpContactDetails.FindAsync(id);
        }

        public async Task AddAsync(EmpContactDetails contact)
        {
            await _context.EmpContactDetails.AddAsync(contact);
        }

        public async Task UpdateAsync(EmpContactDetails contact)
        {
            _context.EmpContactDetails.Update(contact);
            await Task.CompletedTask; // EF tracking marks it modified, execution is synchronous until SaveChanges
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
