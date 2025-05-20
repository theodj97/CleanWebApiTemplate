namespace CleanWebApiTemplate.Host.Models.Interfaces;

// This interface is created here because it needs a IEndpointRouteBuilder class that depends of a Microsoft.NET.Sdk.Web dependency project.
public interface IGroupMap
{
    void MapGroup(IEndpointRouteBuilder app);
}

