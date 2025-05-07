namespace CleanWebApiTemplate.Host.Routes.ResponseModels;

public interface IBaseResponse<DtoType, ResponseType>
{
    static abstract ResponseType? ToResponse(DtoType? dto);
}
