using System.Globalization;
using CleanWebApiTemplate.Application.Common.Handlers;
using CleanWebApiTemplate.Application.Common.Results;
using CleanWebApiTemplate.Application.Common.Validation;
using CleanWebApiTemplate.Domain.Project;

namespace CleanWebApiTemplate.Application.Project.Update;

public sealed class UpdateProjectHandler(IProjectRepository projectRepository,
                                      ValidatorSet<UpdateProjectInput> validators)
    : ValidationService, IHandler<UpdateProjectInput, ProjectDto>
{
    public Task<Result<ProjectDto>> HandleAsync(UpdateProjectInput input, CancellationToken cancellationToken) =>
        ExecuteAsync(input, validators, async () =>
        {
            ProjectEntity? projectDb = await projectRepository.GetByIdAsync(
                Ulid.Parse(input.Id, CultureInfo.InvariantCulture),
                input.VisibleOwner,
                cancellationToken);

            if (projectDb is null)
                return Result<ProjectDto>.Failure(new NotFoundError("ID was not found."));

            if (input.Name is not null)
            {
                string trimmedName = input.Name.Trim();
                if (string.IsNullOrWhiteSpace(trimmedName))
                    return Result<ProjectDto>.Failure(new BadRequestError("Name can't be empty."));

                // Names are unique per owner. The owner comes from the stored row, so an admin
                // updating another caller's Project still checks against that caller's names.
                if (projectDb.Name != trimmedName
                    && await projectRepository.NameExistsAsync(trimmedName,
                                                             projectDb.CreatedBy,
                                                             projectDb.Id,
                                                             cancellationToken))
                {
                    return Result<ProjectDto>.Failure(
                        new ConflictError("A Project with this name already exists for this owner."));
                }
            }

            if (input.PatchFields(projectDb) is false)
                return Result<ProjectDto>.Success(projectDb.ToDto());

            Result<bool> update = await projectRepository.UpdateAsync(projectDb, cancellationToken);

            if (update.IsFailure)
                return Result<ProjectDto>.Failure(update.Error!);

            return update.Value
                ? Result<ProjectDto>.Success(projectDb.ToDto())
                : Result<ProjectDto>.Failure(new NotFoundError("ID was not found."));
        }, cancellationToken);
}
