using Web.ViewModels;

namespace Web.Services.Admin;

public interface IPrincipalClassesService
{
    Task<PrincipalClassesViewModel> BuildClassesViewModelAsync(
        int grade,
        CancellationToken cancellationToken = default);
}
