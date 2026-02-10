using ProjectBullet.Core.Entities;

namespace ProjectBullet.Core.Repositories;

/// <summary>
/// Stores guests to a database.
/// </summary>
public class DbGuestRepository : DbRepository<GuestEntity>, IGuestRepository
{
    public DbGuestRepository(ApplicationDbContext context)
        : base(context)
    {

    }
}
