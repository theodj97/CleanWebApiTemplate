namespace CleanWebApiTemplate.Domain.Common;

/// <summary>An entity with a single key. Every stored row has one.</summary>
public interface IBaseEntity<TKey>
{
    TKey Id { get; set; }
}
