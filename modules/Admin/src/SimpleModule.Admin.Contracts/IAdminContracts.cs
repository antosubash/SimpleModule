namespace SimpleModule.Admin.Contracts;

public interface IAdminContracts
{
    Task<AdminOverviewDto> GetAdminOverviewAsync(CancellationToken cancellationToken = default);
}
