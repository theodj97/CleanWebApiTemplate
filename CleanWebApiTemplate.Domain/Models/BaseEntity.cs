namespace CleanWebApiTemplate.Domain.Models;

public abstract class BaseEntity<TKey> : IBaseEntity<TKey>
{
    public TKey Id { get; set; } = default!;
}
