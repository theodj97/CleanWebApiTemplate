namespace CleanWebApiTemplate.Domain.Common;

/// <summary>Base class for entities: the key is the only member every entity shares.</summary>
public abstract class BaseEntity<TKey> : IBaseEntity<TKey>
{
    public abstract TKey Id { get; set; }
}
