using JP_Morgan_POC.Model;
using Microsoft.EntityFrameworkCore;

namespace JP_Morgan_POC.MyContext
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {

        }

        public DbSet<EmpContactDetails> EmpContactDetails => Set<EmpContactDetails>();
        public DbSet<EmpContactDetailsOutbox> EmpContactDetailsOutbox => Set<EmpContactDetailsOutbox>();
        public DbSet<SyncControl> SyncControls => Set<SyncControl>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<EmpContactDetails>().ToTable(tb => tb.UseSqlOutputClause(false)); // disable OUTPUT for this table // other configuration... }

            modelBuilder.Entity<EmpContactDetails>(entity =>
            {
                entity.ToTable("EmpContactDetails", "dbo");
                entity.HasKey(x => x.Id);

                entity.Property(x => x.EmpAddress).HasMaxLength(200).IsRequired();
                entity.Property(x => x.EmpProfile).HasMaxLength(100).IsRequired();
                entity.Property(x => x.SynStatus).HasMaxLength(500).IsRequired();
                entity.Property(x => x.EmpFirstName).HasMaxLength(100).IsRequired();
                entity.Property(x => x.EmpLastName).HasMaxLength(100).IsRequired();
                entity.Property(x => x.CreatedAt).IsRequired();
            });

            modelBuilder.Entity<EmpContactDetailsOutbox>(entity =>
            {
                entity.ToTable("EmpContactDetails_Outbox", "dbo");
                entity.HasKey(x => x.OutboxId);

                entity.Property(x => x.EventType).HasMaxLength(20).IsRequired();
                entity.Property(x => x.Status).HasMaxLength(20).IsRequired();
                entity.Property(x => x.ErrorMessage).HasMaxLength(2000);

                entity.HasOne(x => x.EmpContactDetails)
                      .WithMany()
                      .HasForeignKey(x => x.EmpId);
            });

            modelBuilder.Entity<SyncControl>(entity =>
            {
                entity.ToTable("SyncControl", "dbo");
                entity.HasKey(x => x.EntityName);

                entity.Property(x => x.EntityName).HasMaxLength(100);
                entity.Property(x => x.LastSchemaHash).HasMaxLength(200);
                entity.Property(x => x.StopReason).HasMaxLength(2000);
            });
        }
    }
}

