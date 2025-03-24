using CleanWebApiTemplate.Host.Routes.Todo.Filter;

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

            return queryParams.Count > 0 ? $"{BaseRoute}?{string.Join("&", queryParams)}" : BaseRoute;
        }


        public static string Create() => "api/Todo";

        public static string GetTitles(int pageNumber, int pageSize) => $"api/Todo/titles?PageNumber={pageNumber}&PageSize={pageSize}";
    }
}
