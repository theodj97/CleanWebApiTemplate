namespace CleanWebApiTemplate.Api.Common.Abstractions;

/// <summary>One feature's route group. Implementations are registered explicitly by their module.</summary>
public interface IGroupMap
{
    void MapGroup(IEndpointRouteBuilder app);
}
