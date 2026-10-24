using Domain.Interfaces;

namespace Domain.Entities;

public class Screen : IEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Capacity { get; set; }
    public bool IsActive { get; set; } = true;
}
