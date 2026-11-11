using CleanWebApiTemplate.Application.Common.Handlers;
using CleanWebApiTemplate.Application.Common.Results;
using CleanWebApiTemplate.Application.Common.Validation;
using CleanWebApiTemplate.Domain.Project;

namespace CleanWebApiTemplate.Application.Project.Create;

public sealed class CreateProjectHandler(IProjectRepository projectRepository,
                                      ValidatorSet<CreateProjectInput> validators)
    : ValidationService, IHandler<CreateProjectInput, ProjectDto>
{
    public Task<Result<ProjectDto>> HandleAsync(CreateProjectInput input, CancellationToken cancellationToken) =>
        ExecuteAsync(input, validators, async () =>
        {
            ProjectEntity projectEntity = input.ToEntity();

            DateTime actualUtcMoment = DateTime.UtcNow;
            projectEntity.CreatedAt = actualUtcMoment;
            projectEntity.UpdatedAt = actualUtcMoment;

            // Uniqueness is a business rule, not input validation, so the handler owns it and
            // reports a conflict. The unique index still protects the race below.
            if (await projectRepository.NameExistsAsync(projectEntity.Name,
                                                     projectEntity.CreatedBy,
                                                     cancellationToken: cancellationToken))
            {
                return Result<ProjectDto>.Failure(
                    new ConflictError("A Project with this name already exists for this owner."));
            }

            Result<ProjectEntity> insertion = await projectRepository.InsertAsync(projectEntity, cancellationToken);

            return insertion.IsFailure
                ? Result<ProjectDto>.Failure(insertion.Error!)
                : Result<ProjectDto>.Success(insertion.Value!.ToDto());
        }, cancellationToken);
}
