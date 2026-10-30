# Summary
<details open><summary>Summary</summary>

|||
|:---|:---|
| Generated on: | 10/30/2026 - 12:35:19 |
| Parser: | Cobertura |
| Assemblies: | 5 |
| Classes: | 71 |
| Files: | 60 |
| **Line coverage:** | 98% (1886 of 1924) |
| Covered lines: | 1886 |
| Uncovered lines: | 38 |
| Coverable lines: | 1924 |
| Total lines: | 3223 |
| **Branch coverage:** | 95.6% (553 of 578) |
| Covered branches: | 553 |
| Total branches: | 578 |
| **Method coverage:** | [Feature is only available for sponsors](https://reportgenerator.io/pro) |

</details>

## Coverage
<details><summary>CleanWebApiTemplate.Api - 98.8%</summary>

|**Name**|**Line**|**Branch**|
|:---|---:|---:|
|**CleanWebApiTemplate.Api**|**98.8%**|**95.9%**|
|CleanWebApiTemplate.Api.Common.Abstractions.EndpointRouteBuilderExtensions|100%|100%|
|CleanWebApiTemplate.Api.Common.HttpContextCurrentUser|100%|100%|
|CleanWebApiTemplate.Api.Configuration.AppSettingsValidator|100%|100%|
|CleanWebApiTemplate.Api.Configuration.AuthenticationSettings|98.5%|96.1%|
|CleanWebApiTemplate.Api.Configuration.CommonOpenApiSchemas|100%|100%|
|CleanWebApiTemplate.Api.Configuration.Constants|100%||
|CleanWebApiTemplate.Api.Configuration.GlobalExceptionHandler|95.2%|77.2%|
|CleanWebApiTemplate.Api.Configuration.OpenApiConfiguration|100%|91.6%|
|CleanWebApiTemplate.Api.Configuration.OpenApiSchemaHelpers|100%|100%|
|CleanWebApiTemplate.Api.Configuration.ProblemDetailsFactory|100%||
|CleanWebApiTemplate.Api.Configuration.SectionBase|100%||
|CleanWebApiTemplate.Api.Configuration.TodoOpenApiSchemas|100%|100%|
|CleanWebApiTemplate.Api.ConfigureApplication|94.4%|100%|
|CleanWebApiTemplate.Api.ConfigureServices|98.3%|100%|
|CleanWebApiTemplate.Api.Extensions.ApiResultExtensions|98.2%|100%|
|CleanWebApiTemplate.Api.Extensions.ApiResultExtensions<T>|98.2%|100%|
|CleanWebApiTemplate.Api.Extensions.ApiResultExtensions<TDto, TResponse>|98.2%|100%|
|CleanWebApiTemplate.Api.Extensions.EndpointExtension|100%|100%|
|CleanWebApiTemplate.Api.Helpers.AppConfigurationHelper|95.5%|100%|
|CleanWebApiTemplate.Api.Models.Responses.Todo.TodoResponse|100%||
|CleanWebApiTemplate.Api.Models.Responses.Todo.TodoTitleResponse|100%|50%|
|CleanWebApiTemplate.Api.Program|100%||
|CleanWebApiTemplate.Api.Routes.Todo.Create.CreateTodoRequest|100%||
|CleanWebApiTemplate.Api.Routes.Todo.Filter.FilteredTodoRequest|100%|100%|
|CleanWebApiTemplate.Api.Routes.Todo.Get.GetTodoTitlesRequest|100%||
|CleanWebApiTemplate.Api.Routes.Todo.TodoRoutes|100%|81.2%|
|CleanWebApiTemplate.Api.Routes.Todo.Update.UpdateTodoRequest|100%||

</details>
<details><summary>CleanWebApiTemplate.Application - 98.6%</summary>

|**Name**|**Line**|**Branch**|
|:---|---:|---:|
|**CleanWebApiTemplate.Application**|**98.6%**|**100%**|
|CleanWebApiTemplate.Application.Abstractions.BaseAbstractValidator<TRequest<br/>>|100%||
|CleanWebApiTemplate.Application.Abstractions.BaseAbstractValidator<TRequest<br/>>|95.8%|100%|
|CleanWebApiTemplate.Application.Abstractions.DateRange|100%||
|CleanWebApiTemplate.Application.Dtos.Todo.TodoDtoExtensions|100%||
|CleanWebApiTemplate.Application.Results.BadRequestError|100%||
|CleanWebApiTemplate.Application.Results.ConflictError|100%||
|CleanWebApiTemplate.Application.Results.DomainError|100%||
|CleanWebApiTemplate.Application.Results.Error|100%||
|CleanWebApiTemplate.Application.Results.ForbiddenError|100%||
|CleanWebApiTemplate.Application.Results.NotFoundError|100%||
|CleanWebApiTemplate.Application.Results.Result<T>|100%|100%|
|CleanWebApiTemplate.Application.Results.UnauthorizedError|100%||
|CleanWebApiTemplate.Application.Todo.Create.CreateTodoInput|100%||
|CleanWebApiTemplate.Application.Todo.Create.CreateTodoInputValidator|100%|100%|
|CleanWebApiTemplate.Application.Todo.Create.CreateTodoService|100%|100%|
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
|CleanWebApiTemplate.Application.Todo.TodoValidator<TRequest>|95.6%|100%|
|CleanWebApiTemplate.Application.Todo.Update.UpdateTodoInput|100%|100%|
|CleanWebApiTemplate.Application.Todo.Update.UpdateTodoInputValidator|100%|100%|
|CleanWebApiTemplate.Application.Todo.Update.UpdateTodoService|95.1%|100%|
|CleanWebApiTemplate.Application.Validation.ValidationRunner<TInput>|100%|100%|
|CleanWebApiTemplate.Application.Validation.ValidationService<TInput, TResul<br/>t>|100%||

</details>
<details><summary>CleanWebApiTemplate.Domain - 100%</summary>

|**Name**|**Line**|**Branch**|
|:---|---:|---:|
|**CleanWebApiTemplate.Domain**|**100%**|****|
|CleanWebApiTemplate.Domain.Models.Entities.TodoEntity|100%||

</details>
<details><summary>CleanWebApiTemplate.Infrastructure - 95.8%</summary>

|**Name**|**Line**|**Branch**|
|:---|---:|---:|
|**CleanWebApiTemplate.Infrastructure**|**95.8%**|**87%**|
|CleanWebApiTemplate.Infrastructure.Data.NpgsqlConnectionFactory|100%||
|CleanWebApiTemplate.Infrastructure.Repositories.Todo.Helpers.TodoSortColumn<br/>s|100%||
|CleanWebApiTemplate.Infrastructure.Repositories.Todo.TodoRepository|96.4%|86.8%|
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
