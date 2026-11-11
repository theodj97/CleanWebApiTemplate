using {App}.Application.Abstractions.Handlers;
using {App}.Application.Abstractions.Repositories;
using {App}.Application.Dtos.Project;
using {App}.Application.Results;
using {App}.Application.Validation;
using {App}.Domain.Models.Entities;

namespace {App}.Application.Project.Handlers.Filtered;

public sealed class FilteredProjectHandler(IProjectRepository projectRepository,
                                           ValidatorSet<FilteredProjectInput> validators)
    : ValidationService, IHandler<FilteredProjectInput, IEnumerable<ProjectDto?>>
{
    public Task<Result<IEnumerable<ProjectDto?>>> HandleAsync(FilteredProjectInput input, CancellationToken cancellationToken) =>
        ExecuteAsync(input, validators, async () =>
        {
            IReadOnlyList<ProjectEntity> projects = await projectRepository.SearchAsync(input.ToFilter(), cancellationToken);

            return Result<IEnumerable<ProjectDto?>>.Success(projects.Select(x => x.ToDto()));
        }, cancellationToken);
}
