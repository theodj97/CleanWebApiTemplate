using CleanWebApiTemplate.Host.Routes.Todo.Filter;
using CleanWebApiTemplate.Host.Routes.Todo.Get;

namespace CleanWebApiTemplate.Testing.Common;

public static class ApiRoutes
{
    public static class Todo
    {
        private const string BaseRoute = "api/Todo";
        public static string GetById(string id) => $"{BaseRoute}/{id}";

        public static string Update(string id) => $"{BaseRoute}/{id}";

        public static string Delete(string id) => $"{BaseRoute}/{id}";

        public static string Filtered(FilteredTodoRequest request)
        {
            var queryParams = new List<string>();

            if (request.Ids != null && request.Ids.Length > 0)
                queryParams.AddRange(request.Ids.Select(id => $"Ids={id}"));

            if (request.Title != null && request.Title.Length > 0)
                queryParams.AddRange(request.Title.Select(title => $"Title={title}"));

            if (request.Status != null && request.Status.Length > 0)
                queryParams.AddRange(request.Status.Select(status => $"Status={status}"));

            if (request.CreatedBy != null && request.CreatedBy.Length > 0)
                queryParams.AddRange(request.CreatedBy.Select(createdBy => $"CreatedBy={createdBy}"));

            if (!string.IsNullOrEmpty(request.StartDate))
                queryParams.Add($"StartDate={request.StartDate}");

            if (!string.IsNullOrEmpty(request.EndDate))
                queryParams.Add($"EndDate={request.EndDate}");

            if (request.PageNumber.HasValue)
                queryParams.Add($"PageNumber={request.PageNumber.Value}");

            if (request.PageSize.HasValue)
                queryParams.Add($"PageSize={request.PageSize.Value}");

            if (request.OrderBy.HasValue)
                queryParams.Add($"OrderBy={request.OrderBy.Value}");

            if (request.OrderDescending is not null && request.OrderDescending.Value)
                queryParams.Add("OrderDescending=true");

            return queryParams.Count > 0 ? $"{BaseRoute}?{string.Join("&", queryParams)}" : BaseRoute;
        }

        public static string Create() => "api/Todo";

        public static string GetTitles(GetTodoTitlesRequest request)
        {
            var queryParams = new List<string>
            {
                $"PageNumber={request.PageNumber}",
                $"PageSize={request.PageSize}"
            };

            if (request.OrderBy is not null)
                queryParams.Add($"OrderBy={request.OrderBy}");

            if (request.OrderDescending.HasValue)
                queryParams.Add($"OrderDescending={request.OrderDescending.Value.ToString().ToLower()}");

            return $"api/Todo/titles?{string.Join("&", queryParams)}";
        }
    }
}
