# Summary
<details open><summary>Summary</summary>

|||
|:---|:---|
| Generated on: | 10/30/2026 - 07:49:30 |
| Parser: | Cobertura |
| Assemblies: | 5 |
| Classes: | 71 |
| Files: | 57 |
| **Line coverage:** | 84% (1558 of 1853) |
| Covered lines: | 1558 |
| Uncovered lines: | 295 |
| Coverable lines: | 1853 |
| Total lines: | 3175 |
| **Branch coverage:** | 81.3% (477 of 586) |
| Covered branches: | 477 |
| Total branches: | 586 |
| **Method coverage:** | [Feature is only available for sponsors](https://reportgenerator.io/pro) |

</details>

## Coverage
<details><summary>CleanWebApiTemplate.Api - 72%</summary>

|**Name**|**Line**|**Branch**|
|:---|---:|---:|
|**CleanWebApiTemplate.Api**|**72%**|**65.3%**|
|CleanWebApiTemplate.Api.Common.Abstractions.EndpointRouteBuilderExtensions|45.4%|16.6%|
|CleanWebApiTemplate.Api.Common.BaseApiRouter|100%|100%|
|CleanWebApiTemplate.Api.Configuration.AppSettingsValidator|100%|100%|
|CleanWebApiTemplate.Api.Configuration.AuthenticationSettings|98.5%|96.1%|
|CleanWebApiTemplate.Api.Configuration.GlobalExceptionHandler|95.2%|77.2%|
|CleanWebApiTemplate.Api.Configuration.OpenApiConfiguration|1.7%|2.8%|
|CleanWebApiTemplate.Api.Configuration.ProblemDetailsFactory|100%||
|CleanWebApiTemplate.Api.ConfigureApplication|94.4%|100%|
|CleanWebApiTemplate.Api.ConfigureServices|98.3%|100%|
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
|CleanWebApiTemplate.Api.Routes.Todo.TodoRoutes|91.3%|25%|
|CleanWebApiTemplate.Api.Routes.Todo.Update.UpdateTodoRequest|100%||

</details>
<details><summary>CleanWebApiTemplate.Application - 97.4%</summary>

|**Name**|**Line**|**Branch**|
|:---|---:|---:|
|**CleanWebApiTemplate.Application**|**97.4%**|**99.3%**|
|CleanWebApiTemplate.Application.Abstractions.BaseAbstractValidator<TRequest<br/>>|100%||
|CleanWebApiTemplate.Application.Abstractions.BaseAbstractValidator<TRequest<br/>>|95.1%|100%|
|CleanWebApiTemplate.Application.Abstractions.DateRange|100%||
|CleanWebApiTemplate.Application.Todo.Create.CreateTodoInput|100%||
|CleanWebApiTemplate.Application.Todo.Create.CreateTodoInputValidator|100%|100%|
|CleanWebApiTemplate.Application.Todo.Create.CreateTodoService|100%||
|CleanWebApiTemplate.Application.Todo.Delete.DeleteTodoInputValidator|100%||
|CleanWebApiTemplate.Application.Todo.Delete.DeleteTodoService|100%||
|CleanWebApiTemplate.Application.Todo.Filtered.FilteredTodoInput|100%|100%|
|CleanWebApiTemplate.Application.Todo.Filtered.FilteredTodoInputValidator|100%|96.8%|
|CleanWebApiTemplate.Application.Todo.Filtered.FilteredTodoService|100%|100%|
|CleanWebApiTemplate.Application.Todo.GetById.GetTodoByIdInputValidator|100%||
|CleanWebApiTemplate.Application.Todo.GetById.GetTodoByIdService|100%|100%|
|CleanWebApiTemplate.Application.Todo.GetTitles.GetTodoTitlesInput|100%||
|CleanWebApiTemplate.Application.Todo.GetTitles.GetTodoTitlesInputValidator|100%|100%|
|CleanWebApiTemplate.Application.Todo.GetTitles.GetTodoTitlesService|100%|100%|
|CleanWebApiTemplate.Application.Todo.IdAndTitle|100%||
|CleanWebApiTemplate.Application.Todo.TodoValidator<TRequest>|93.7%|100%|
|CleanWebApiTemplate.Application.Todo.TodoValidator<TRequest>|87.8%|100%|
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
