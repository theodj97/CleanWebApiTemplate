using CleanWebApiTemplate.Application.Common.Handlers;
using CleanWebApiTemplate.Application.Common.Results;
using CleanWebApiTemplate.Application.Common.Validation;

namespace CleanWebApiTemplate.Application.Project.GetNames;

public sealed class GetProjectNamesHandler(IProjectRepository projectRepository,
                                         ValidatorSet<GetProjectNamesInput> validators)
    : ValidationService, IHandler<GetProjectNamesInput, IEnumerable<ProjectName>>
{
    public Task<Result<IEnumerable<ProjectName>>> HandleAsync(GetProjectNamesInput input, CancellationToken cancellationToken) =>
        ExecuteAsync(input, validators, async () =>
        {
            IReadOnlyList<ProjectName> projectedProjects = await projectRepository.GetNamesAsync(
                input.PageNumber,
                input.PageSize,
                input.SortProperties,
                input.VisibleOwner,
                cancellationToken);

            return Result<IEnumerable<ProjectName>>.Success(projectedProjects);
        }, cancellationToken);
}
