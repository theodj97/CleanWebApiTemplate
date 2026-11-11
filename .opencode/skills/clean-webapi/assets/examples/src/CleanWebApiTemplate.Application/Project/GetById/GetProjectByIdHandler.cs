using System.Globalization;
using CleanWebApiTemplate.Application.Common.Handlers;
using CleanWebApiTemplate.Application.Common.Results;
using CleanWebApiTemplate.Application.Common.Validation;
using CleanWebApiTemplate.Domain.Project;

namespace CleanWebApiTemplate.Application.Project.GetById;

public sealed class GetProjectByIdHandler(IProjectRepository projectRepository,
                                       ValidatorSet<GetProjectByIdInput> validators)
    : ValidationService, IHandler<GetProjectByIdInput, ProjectDto>
{
    public Task<Result<ProjectDto>> HandleAsync(GetProjectByIdInput input, CancellationToken cancellationToken) =>
        ExecuteAsync(input, validators, async () =>
        {
            ProjectEntity? project = await projectRepository.GetByIdAsync(
                Ulid.Parse(input.Id, CultureInfo.InvariantCulture),
                input.VisibleOwner,
                cancellationToken);

            return project is null
                ? Result<ProjectDto>.Failure(new NotFoundError("ID was not found."))
                : Result<ProjectDto>.Success(project.ToDto());
        }, cancellationToken);
}
