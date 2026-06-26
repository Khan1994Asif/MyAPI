using Microsoft.Data.SqlClient;
using Dapper;

namespace JP_Morgan_POC.Repositories
{
    public class SyncControlRepository
    {
        private readonly IConfiguration _config;

        public SyncControlRepository(IConfiguration config)
        {
            _config = config;
        }

        private SqlConnection CreateConnection() =>
            new SqlConnection(_config.GetConnectionString("DefaultConnection"));

        public async Task<(bool IsEnabled, string? LastSchemaHash, DateTime? LastRefreshUtc)> GetAsync()
        {
            var sql = @"
SELECT IsEnabled, LastSchemaHash, LastMetadataRefreshUtc
FROM dbo.SyncControl
WHERE EntityName = N'EmpContactDetailsToContact';";

            await using var conn = CreateConnection();
            return await conn.QuerySingleAsync<(bool, string?, DateTime?)>(sql);
        }

        public async Task UpdateMetadataAsync(string schemaHash)
        {
            var sql = @"
UPDATE dbo.SyncControl
SET LastSchemaHash = @SchemaHash,
    LastMetadataRefreshUtc = SYSUTCDATETIME()
WHERE EntityName = N'EmpContactDetailsToContact';";

            await using var conn = CreateConnection();
            await conn.ExecuteAsync(sql, new { SchemaHash = schemaHash });
        }

        public async Task DisableAsync(string reason)
        {
            var sql = @"
UPDATE dbo.SyncControl
SET IsEnabled = 0,
    StopReason = @Reason
WHERE EntityName = N'EmpContactDetailsToContact';";

            await using var conn = CreateConnection();
            await conn.ExecuteAsync(sql, new { Reason = reason });
        }

        public async Task EnableAsync()
        {
            var sql = @"
UPDATE dbo.SyncControl
SET IsEnabled = 1,
    StopReason = NULL
WHERE EntityName = N'EmpContactDetailsToContact';";

            await using var conn = CreateConnection();
            await conn.ExecuteAsync(sql);
        }
    }
}
