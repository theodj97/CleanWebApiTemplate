using System.Globalization;
using {App}.Application.Abstractions.Handlers;
using {App}.Application.Abstractions.Repositories;
using {App}.Application.Dtos.Project;
using {App}.Application.Results;
using {App}.Application.Validation;
using {App}.Domain.Models.Entities;

namespace {App}.Application.Project.Handlers.GetById;

public sealed class GetProjectByIdHandler(IProjectRepository projectRepository,
                                          ValidatorSet<GetProjectByIdInput> validators)
    : ValidationService, IHandler<GetProjectByIdInput, ProjectDto?>
{
    public Task<Result<ProjectDto?>> HandleAsync(GetProjectByIdInput input, CancellationToken cancellationToken) =>
        ExecuteAsync(input, validators, async () =>
        {
            ProjectEntity? project = await projectRepository.GetByIdAsync(
                Ulid.Parse(input.Id, CultureInfo.InvariantCulture),
                input.CreatedBy,
                cancellationToken);

            return Result<ProjectDto?>.Success(project?.ToDto());
        }, cancellationToken);
}
