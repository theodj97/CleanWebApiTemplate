namespace CleanWebApiTemplate.Host.Models.Responses;

public interface IBaseResponse<in DtoType, out ResponseType> where ResponseType : IBaseResponse<DtoType, ResponseType>
{
    public abstract static ResponseType? ToResponseModel(DtoType? dto);
}
