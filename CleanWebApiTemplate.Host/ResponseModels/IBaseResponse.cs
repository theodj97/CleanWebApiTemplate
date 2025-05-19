namespace CleanWebApiTemplate.Host.ResponseModels;

public interface IBaseResponse<DtoType, ResponseType> where ResponseType : IBaseResponse<DtoType, ResponseType>
{
    static abstract ResponseType? ToResponseModel(DtoType? dto);
}
