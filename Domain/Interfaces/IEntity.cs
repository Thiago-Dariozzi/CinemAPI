namespace Domain.Interfaces;

// Lo que comparten todas las entidades persistidas: Id Guid y soft delete con IsActive.
public interface IEntity
{
    Guid Id { get; }

    bool IsActive { get; set; }
}
