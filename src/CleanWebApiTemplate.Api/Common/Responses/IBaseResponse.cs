namespace CleanWebApiTemplate.Api.Common.Responses;

/// <summary>
/// Maps one DTO or projection to the response the API returns. A static abstract member, so the mapping is
/// resolved at compile time: no reflection, no factories and no Native AOT annotations.
/// </summary>
public interface IBaseResponse<in TDto, out TResponse> where TResponse : IBaseResponse<TDto, TResponse>
{
    static abstract TResponse ToResponseModel(TDto dto);
}
