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
            throw new ArgumentException("Parent id cannot be empty.", nameof(id));

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
            throw new ArgumentException("Parent UserId cannot be empty.", nameof(parent));

        await _parentRepository.CreateAsync(parent, cancellationToken);
    }

    public async Task UpdateAsync(Parent parent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(parent);

        var existing = await _parentRepository.GetByIdAsync(parent.Id, cancellationToken);
        if (existing is null)
            throw new InvalidOperationException("Parent not found.");

        await _parentRepository.UpdateAsync(parent, cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Parent id cannot be empty.", nameof(id));

        await _parentRepository.DeleteAsync(id, cancellationToken);
    }
}