namespace CleanWebApiTemplate.Domain.Models;

public class BaseUlidEntity : BaseEntity<Ulid>
{
    public BaseUlidEntity()
    => Id = Ulid.NewUlid();

    public BaseUlidEntity(Ulid id)
    => Id = id;
}
