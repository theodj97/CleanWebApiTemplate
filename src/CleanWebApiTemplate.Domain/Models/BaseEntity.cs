namespace CleanWebApiTemplate.Domain.Models;

public abstract class BaseEntity<TKey> : IBaseEntity<TKey>
{
    public abstract TKey Id { get; set; }
}
