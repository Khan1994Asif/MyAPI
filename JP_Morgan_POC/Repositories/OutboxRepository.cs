using Dapper;
using JP_Morgan_POC.Model;
using JP_Morgan_POC.MyContext;
using Microsoft.Data.SqlClient;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;


namespace JP_Morgan_POC.Repositories
{

    public class OutboxRepository
    {
        private readonly ApplicationDbContext _dbContext;

        public OutboxRepository(ApplicationDbContext context)
        {
            _dbContext = context;
        }

        public async Task<List<EmpContactDetailsOutbox>> LockPendingBatchAsync(CancellationToken ct = default)
        {

            //var pendingRows = await _dbContext.EmpContactDetailsOutbox
            //    .Where(x => x.Status == "Pending")
            //    .OrderBy(x => x.OutboxId)
            //    .Take(batchSize)
            //    .ToListAsync(ct);

            var pendingRows = await _dbContext.EmpContactDetailsOutbox
                            .Where(x => x.Status == "Pending")
                            .Include(x => x.EmpContactDetails)
                            .OrderBy(x => x.OutboxId)
                            //.Take(batchSize)
                            .ToListAsync(ct);

            return pendingRows;

        }

        public async Task<EmpContactDetailsRow?> GetEmployeeAsync(int empId, CancellationToken ct = default)
        {
            return await _dbContext.EmpContactDetails
                .Where(x => x.Id == empId)
                .Select(x => new EmpContactDetailsRow
                {
                    Id = x.Id,
                    EmpAddress = x.EmpAddress,
                    EmpProfile = x.EmpProfile,
                    SynStatus = x.SynStatus,
                    CreatedAt = x.CreatedAt,
                    EmpFirstName = x.EmpFirstName,
                    EmpLastName = x.EmpLastName
                })
                .SingleOrDefaultAsync(ct);
        }

        public async Task<List<EmpContactDetailsRow>> GetEmployeesAsync(List<int> empIds, CancellationToken ct = default)
        {
            return await _dbContext.EmpContactDetails
                .Where(x => empIds.Contains(x.Id))
                .Select(x => new EmpContactDetailsRow
                {
                    Id = x.Id,
                    EmpAddress = x.EmpAddress,
                    EmpProfile = x.EmpProfile,
                    SynStatus = x.SynStatus,
                    CreatedAt = x.CreatedAt,
                    EmpFirstName = x.EmpFirstName,
                    EmpLastName = x.EmpLastName
                })
                .ToListAsync(ct);
        }

        public async Task MarkProcessedAsync(long outboxId, CancellationToken ct = default)
        {
            var row = await _dbContext.EmpContactDetailsOutbox
                .SingleAsync(x => x.OutboxId == outboxId, ct);

            row.Status = "Processed";
            row.ProcessedAtUtc = DateTime.UtcNow;
            row.ErrorMessage = null;

            await _dbContext.SaveChangesAsync(ct);
        }

        public async Task MarkProcessedAsync(IEnumerable<long> outboxIds, CancellationToken ct = default)
        {
            var ids = outboxIds.ToList();

            var rows = await _dbContext.EmpContactDetailsOutbox
                .Where(x => ids.Contains(x.OutboxId))
                .ToListAsync(ct);

            var now = DateTime.UtcNow;

            foreach (var row in rows)
            {
                row.Status = "Processed";
                row.ProcessedAtUtc = now;
                row.ErrorMessage = null;
            }

            await _dbContext.SaveChangesAsync(ct);
        }

        public async Task MarkFailedAsync(long outboxId, string error, CancellationToken ct = default)
        {
            var row = await _dbContext.EmpContactDetailsOutbox
                .SingleAsync(x => x.OutboxId == outboxId, ct);

            row.RetryCount += 1;
            row.Status = row.RetryCount >= 5 ? "Failed" : "Pending";
            row.ErrorMessage = error;

            await _dbContext.SaveChangesAsync(ct);
        }

        public async Task MarkBlockedAsync(long outboxId, string error, CancellationToken ct = default)
        {
            var row = await _dbContext.EmpContactDetailsOutbox
                .SingleAsync(x => x.OutboxId == outboxId, ct);

            row.Status = "Blocked";
            row.ErrorMessage = error;

            await _dbContext.SaveChangesAsync(ct);
        }

        public async Task UpdateSourceStatusAsync(int empId, string status, CancellationToken ct = default)
        {
            var row = await _dbContext.EmpContactDetails
                .SingleAsync(x => x.Id == empId, ct);

            row.SynStatus = status;
            await _dbContext.SaveChangesAsync(ct);
        }

        public async Task UpdateSourceStatusesAsync(List<int> empIds, string status, CancellationToken ct = default)
        {
            var rows = await _dbContext.EmpContactDetails
                .Where(x => empIds.Contains(x.Id))
                .ToListAsync(ct);

            foreach (var row in rows)
            {
                row.SynStatus = status;
            }

            await _dbContext.SaveChangesAsync(ct);
        }
    }
}
