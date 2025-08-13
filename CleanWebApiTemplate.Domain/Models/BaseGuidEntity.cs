namespace CleanWebApiTemplate.Domain.Models;

public class BaseGuidEntity : BaseEntity<Guid>
{
    public BaseGuidEntity()
    => Id = Guid.NewGuid();

    public BaseGuidEntity(Guid id)
    => Id = id;
}
