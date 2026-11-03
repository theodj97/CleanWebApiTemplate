# Summary
<details open><summary>Summary</summary>

|||
|:---|:---|
| Generated on: | 10/29/2026 - 16:20:07 |
| Parser: | Cobertura |
| Assemblies: | 5 |
| Classes: | 69 |
| Files: | 55 |
| **Line coverage:** | 82.7% (1372 of 1659) |
| Covered lines: | 1372 |
| Uncovered lines: | 287 |
| Coverable lines: | 1659 |
| Total lines: | 2883 |
| **Branch coverage:** | 79.7% (410 of 514) |
| Covered branches: | 410 |
| Total branches: | 514 |
| **Method coverage:** | [Feature is only available for sponsors](https://reportgenerator.io/pro) |

</details>

## Coverage
<details><summary>CleanWebApiTemplate.Api - 66%</summary>

|**Name**|**Line**|**Branch**|
|:---|---:|---:|
|**CleanWebApiTemplate.Api**|**66%**|**56.9%**|
|CleanWebApiTemplate.Api.Common.Abstractions.EndpointRouteBuilderExtensions|45.4%|16.6%|
|CleanWebApiTemplate.Api.Common.BaseApiRouter|100%|100%|
|CleanWebApiTemplate.Api.Configuration.AppSettingsValidator|100%|100%|
|CleanWebApiTemplate.Api.Configuration.GlobalExceptionHandler|100%|100%|
|CleanWebApiTemplate.Api.Configuration.OpenApiConfiguration|1.7%|2.8%|
|CleanWebApiTemplate.Api.ConfigureApplication|96%|100%|
|CleanWebApiTemplate.Api.ConfigureServices|94.5%|90.9%|
|CleanWebApiTemplate.Api.Extensions.ApiResultExtensions|100%|100%|
|CleanWebApiTemplate.Api.Extensions.ApiResultExtensions<T>|100%|100%|
|CleanWebApiTemplate.Api.Extensions.ApiResultExtensions<TDto, TResponse>|100%|100%|
|CleanWebApiTemplate.Api.Extensions.EndpointExtension|100%|100%|
|CleanWebApiTemplate.Api.Helpers.AppConfigurationHelper|100%|100%|
|CleanWebApiTemplate.Api.Models.Responses.Todo.TodoResponse|100%||
|CleanWebApiTemplate.Api.Models.Responses.Todo.TodoTitleResponse|100%|50%|
|CleanWebApiTemplate.Api.Program|100%||
|CleanWebApiTemplate.Api.Routes.Todo.Create.CreateTodoRequest|100%||
|CleanWebApiTemplate.Api.Routes.Todo.Filter.FilteredTodoRequest|100%|100%|
|CleanWebApiTemplate.Api.Routes.Todo.Get.GetTodoTitlesRequest|100%||
|CleanWebApiTemplate.Api.Routes.Todo.TodoRoutes|91.3%|14.2%|
|CleanWebApiTemplate.Api.Routes.Todo.Update.UpdateTodoRequest|100%||

</details>
<details><summary>CleanWebApiTemplate.Application - 99.7%</summary>

|**Name**|**Line**|**Branch**|
|:---|---:|---:|
|**CleanWebApiTemplate.Application**|**99.7%**|**100%**|
|CleanWebApiTemplate.Application.Abstractions.BaseAbstractValidator<TRequest<br/>>|100%||
|CleanWebApiTemplate.Application.Abstractions.BaseAbstractValidator<TRequest<br/>>|100%|100%|
|CleanWebApiTemplate.Application.Abstractions.DateRange|100%||
|CleanWebApiTemplate.Application.Todo.Create.CreateTodoInput|100%||
|CleanWebApiTemplate.Application.Todo.Create.CreateTodoInputValidator|100%|100%|
|CleanWebApiTemplate.Application.Todo.Create.CreateTodoService|100%||
|CleanWebApiTemplate.Application.Todo.Delete.DeleteTodoInputValidator|100%||
|CleanWebApiTemplate.Application.Todo.Delete.DeleteTodoService|100%||
|CleanWebApiTemplate.Application.Todo.Filtered.FilteredTodoInput|100%|100%|
|CleanWebApiTemplate.Application.Todo.Filtered.FilteredTodoInputValidator|100%|100%|
|CleanWebApiTemplate.Application.Todo.Filtered.FilteredTodoService|100%|100%|
|CleanWebApiTemplate.Application.Todo.GetById.GetTodoByIdInputValidator|100%||
|CleanWebApiTemplate.Application.Todo.GetById.GetTodoByIdService|100%|100%|
|CleanWebApiTemplate.Application.Todo.GetTitles.GetTodoTitlesInput|100%||
|CleanWebApiTemplate.Application.Todo.GetTitles.GetTodoTitlesInputValidator|100%|100%|
|CleanWebApiTemplate.Application.Todo.GetTitles.GetTodoTitlesService|100%|100%|
|CleanWebApiTemplate.Application.Todo.IdAndTitle|100%||
|CleanWebApiTemplate.Application.Todo.TodoValidator<TRequest>|100%|100%|
|CleanWebApiTemplate.Application.Todo.TodoValidator<TRequest>|100%|100%|
|CleanWebApiTemplate.Application.Todo.Update.UpdateTodoInput|100%|100%|
|CleanWebApiTemplate.Application.Todo.Update.UpdateTodoInputValidator|100%|100%|
|CleanWebApiTemplate.Application.Todo.Update.UpdateTodoService|93.7%|100%|
|CleanWebApiTemplate.Application.Validation.ValidationRunner<TInput>|100%|100%|
|CleanWebApiTemplate.Application.Validation.ValidationService<TInput, TResul<br/>t>|100%|100%|

</details>
<details><summary>CleanWebApiTemplate.Domain - 100%</summary>

|**Name**|**Line**|**Branch**|
|:---|---:|---:|
|**CleanWebApiTemplate.Domain**|**100%**|**100%**|
|CleanWebApiTemplate.Domain.Configuration.Constants|100%||
|CleanWebApiTemplate.Domain.Configuration.SectionBase|100%||
|CleanWebApiTemplate.Domain.Models.Entities.TodoEntity|100%||
|CleanWebApiTemplate.Domain.ResultModel.BadRequestError|100%||
|CleanWebApiTemplate.Domain.ResultModel.ConflictError|100%||
|CleanWebApiTemplate.Domain.ResultModel.DomainError|100%||
|CleanWebApiTemplate.Domain.ResultModel.Error|100%||
|CleanWebApiTemplate.Domain.ResultModel.ForbiddenError|100%||
|CleanWebApiTemplate.Domain.ResultModel.NotFoundError|100%||
|CleanWebApiTemplate.Domain.ResultModel.Result<T>|100%||
|CleanWebApiTemplate.Domain.ResultModel.Result<T>|100%|100%|
|CleanWebApiTemplate.Domain.ResultModel.UnauthorizedError|100%||

</details>
<details><summary>CleanWebApiTemplate.Infrastructure - 96.8%</summary>

|**Name**|**Line**|**Branch**|
|:---|---:|---:|
|**CleanWebApiTemplate.Infrastructure**|**96.8%**|**88.5%**|
|CleanWebApiTemplate.Infrastructure.Constants.TodoTable|100%||
|CleanWebApiTemplate.Infrastructure.Data.NpgsqlConnectionFactory|100%||
|CleanWebApiTemplate.Infrastructure.Repositories.TodoRepository|98.1%|89%|
|CleanWebApiTemplate.Infrastructure.Repositories.TodoSortColumns|100%||
|CleanWebApiTemplate.Infrastructure.Secrets.OpenBaoClient|90%|83.3%|
|CleanWebApiTemplate.Infrastructure.Secrets.OpenBaoConfigurationExtensions|96.7%|91.6%|
|CleanWebApiTemplate.Infrastructure.Secrets.OpenBaoConfigurationProvider|100%|100%|
|CleanWebApiTemplate.Infrastructure.Secrets.OpenBaoConfigurationSource|100%||
|CleanWebApiTemplate.Infrastructure.Secrets.OpenBaoException|100%||
|CleanWebApiTemplate.Infrastructure.Secrets.OpenBaoOptions|100%||
|CleanWebApiTemplate.Infrastructure.Secrets.OpenBaoWriteRequest|100%||

</details>
<details><summary>CleanWebApiTemplate.ServiceDefaults - 95.7%</summary>

|**Name**|**Line**|**Branch**|
|:---|---:|---:|
|**CleanWebApiTemplate.ServiceDefaults**|**95.7%**|**100%**|
|CleanWebApiTemplate.ServiceDefaults.Extensions|95.7%|100%|
|CleanWebApiTemplate.ServiceDefaults.Extensions<TBuilder>|95.7%|100%|

</details>
