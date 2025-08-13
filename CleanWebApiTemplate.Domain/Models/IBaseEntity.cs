namespace CleanWebApiTemplate.Domain.Models;

public interface IBaseEntity<TKey>
{
    TKey Id { get; set; }
}
