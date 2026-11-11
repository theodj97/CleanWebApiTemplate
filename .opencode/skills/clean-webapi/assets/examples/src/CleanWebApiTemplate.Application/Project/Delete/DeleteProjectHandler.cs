using System.Globalization;
using CleanWebApiTemplate.Application.Common.Handlers;
using CleanWebApiTemplate.Application.Common.Results;
using CleanWebApiTemplate.Application.Common.Validation;

namespace CleanWebApiTemplate.Application.Project.Delete;

public sealed class DeleteProjectHandler(IProjectRepository projectRepository,
                                      ValidatorSet<DeleteProjectInput> validators)
    : ValidationService, IHandler<DeleteProjectInput, bool>
{
    public Task<Result<bool>> HandleAsync(DeleteProjectInput input, CancellationToken cancellationToken) =>
        ExecuteAsync(input, validators, async () =>
        {
            bool deleted = await projectRepository.DeleteAsync(
                Ulid.Parse(input.Id, CultureInfo.InvariantCulture),
                input.VisibleOwner,
                cancellationToken);

            return deleted
                ? Result<bool>.Success(true)
                : Result<bool>.Failure(new NotFoundError("ID was not found."));
        }, cancellationToken);
}
