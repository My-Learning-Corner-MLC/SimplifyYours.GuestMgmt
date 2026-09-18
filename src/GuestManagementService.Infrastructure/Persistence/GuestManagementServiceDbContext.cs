using GuestManagementService.Domain.EventReferences;
using GuestManagementService.Domain.Guests;
using GuestManagementService.Domain.Seating;
using GuestManagementService.Infrastructure.Persistence.Inbox;
using Microsoft.EntityFrameworkCore;

namespace GuestManagementService.Infrastructure.Persistence;

public sealed class GuestManagementServiceDbContext(DbContextOptions<GuestManagementServiceDbContext> options)
    : DbContext(options)
{
    public DbSet<Guest> Guests => Set<Guest>();

    public DbSet<EventReference> EventReferences => Set<EventReference>();

    public DbSet<SeatingLayout> SeatingLayouts => Set<SeatingLayout>();

    public DbSet<InboxMessage> InboxMessages => Set<InboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(GuestManagementServiceDbContext).Assembly);

        base.OnModelCreating(modelBuilder);
    }
}
