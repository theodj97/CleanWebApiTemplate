namespace CleanWebApiTemplate.Domain.Models;

public abstract class BaseEntity
{
    public required Ulid Id { get; set; } = Ulid.NewUlid();
}
