namespace GuestManagementService.Application.Abstractions.Common;

public interface IUnitOfWork
{
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
