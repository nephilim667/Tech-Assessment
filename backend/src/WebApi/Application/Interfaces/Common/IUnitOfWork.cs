namespace WebApi.Application.Interfaces.Common
{
    internal interface IUnitOfWork
    {
        Task SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
