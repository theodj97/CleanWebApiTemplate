namespace CleanWebApiTemplate.Api.Models.Responses;

public interface IBaseResponse<in TDto, out TResponse> where TResponse : IBaseResponse<TDto, TResponse>
{
    public abstract static TResponse? ToResponseModel(TDto? dto);
}
