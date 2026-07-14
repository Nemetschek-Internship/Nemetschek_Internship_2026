using Web.ViewModels;

namespace Web.Services.Admin;

public interface IPrincipalDashboardService
{
    Task<PrincipalHomeViewModel> BuildHomeViewModelAsync(CancellationToken cancellationToken = default);
}
