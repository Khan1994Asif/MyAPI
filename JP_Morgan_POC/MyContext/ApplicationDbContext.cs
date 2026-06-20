using JP_Morgan_POC.Model;
using Microsoft.EntityFrameworkCore;

namespace JP_Morgan_POC.MyContext
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {

        }

        public DbSet<EmpContactDetails> EmpContactDetails { get; set; }
    }
}
