using ProjectBullet.Core.Entities;

namespace ProjectBullet.Core.Repositories;

/// <summary>
/// Stores records to a database.
/// </summary>
public class DbRecordRepository : DbRepository<RecordEntity>, IRecordRepository
{
    public DbRecordRepository(ApplicationDbContext context)
        : base(context)
    {

    }
}
