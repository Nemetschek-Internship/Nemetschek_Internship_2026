using Entities.Models;
using Services.Interfaces.Parents;
using Services.Repositories;

namespace Services.Services.Parents;

public class ParentService : IParentService
{
    private readonly IParentRepository _parentRepository;

    public ParentService(IParentRepository parentRepository)
    {
        _parentRepository = parentRepository;
    }

    public Task<Parent?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Идентификаторът на родителя не може да бъде празен.", nameof(id));

        return _parentRepository.GetByIdAsync(id, cancellationToken);
    }

    public Task<IReadOnlyList<Parent>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return _parentRepository.GetAllAsync(cancellationToken);
    }

    public async Task CreateAsync(Parent parent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(parent);

        if (parent.UserId == Guid.Empty)
            throw new ArgumentException("Идентификаторът на потребителя за този родител не може да бъде празен.", nameof(parent));

        await _parentRepository.CreateAsync(parent, cancellationToken);
    }

    public async Task UpdateAsync(Parent parent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(parent);

        var existing = await _parentRepository.GetByIdAsync(parent.Id, cancellationToken);
        if (existing is null)
            throw new InvalidOperationException("Родителят не беше намерен.");

        await _parentRepository.UpdateAsync(parent, cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Идентификаторът на родителя не може да бъде празен.", nameof(id));

        await _parentRepository.DeleteAsync(id, cancellationToken);
    }
}
