using CleanWebApiTemplate.Application.Common.Handlers;
using CleanWebApiTemplate.Application.Common.Results;
using CleanWebApiTemplate.Application.Common.Validation;
using CleanWebApiTemplate.Domain.Project;

namespace CleanWebApiTemplate.Application.Project.Filter;

public sealed class FilteredProjectHandler(IProjectRepository projectRepository,
                                        ValidatorSet<FilteredProjectInput> validators)
    : ValidationService, IHandler<FilteredProjectInput, IEnumerable<ProjectDto>>
{
    public Task<Result<IEnumerable<ProjectDto>>> HandleAsync(FilteredProjectInput input, CancellationToken cancellationToken) =>
        ExecuteAsync(input, validators, async () =>
        {
            IReadOnlyList<ProjectEntity> projects = await projectRepository.SearchAsync(input.ToFilter(), cancellationToken);

            return Result<IEnumerable<ProjectDto>>.Success(projects.Select(project => project.ToDto()));
        }, cancellationToken);
}
