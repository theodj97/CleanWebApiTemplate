namespace CleanWebApiTemplate.Domain.Models;

public abstract class BaseEntity
{
    public Ulid Id { get; set; } = Ulid.NewUlid();
}
