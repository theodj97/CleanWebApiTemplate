namespace CleanWebApiTemplate.Domain.Models;

public interface IBaseEntity<TKey>
{
    public TKey Id { get; set; }
}
