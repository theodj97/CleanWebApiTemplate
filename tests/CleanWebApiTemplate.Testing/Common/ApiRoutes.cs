namespace CleanWebApiTemplate.Testing.Common;

public static class ApiRoutes
{
    public static class Todo
    {
        private const string BaseRoute = "api/Todo";
        public static string GetById(string id) => $"{BaseRoute}/{id}";

        public static string Update(string id) => $"{BaseRoute}/{id}";

        public static string Delete(string id) => $"{BaseRoute}/{id}";

        public static string Get(string? ids = null,
                                 string? title = null,
                                 string? status = null,
                                 string? createdBy = null)
        {
            var queryParams = new List<string>();

            if (!string.IsNullOrEmpty(ids)) queryParams.Add($"Ids={ids}");
            if (!string.IsNullOrEmpty(title)) queryParams.Add($"Title={title}");
            if (!string.IsNullOrEmpty(status)) queryParams.Add($"Status={status}");
            if (!string.IsNullOrEmpty(createdBy)) queryParams.Add($"CreatedBy={createdBy}");

            return queryParams.Count != 0 ? $"{BaseRoute}?{string.Join("&", queryParams)}" : BaseRoute;
        }

        public static string Create() => "api/Todo";

        public static string GetTitles(int pageNumber, int pageSize) => $"api/Todo/titles?PageNumber={pageNumber}&PageSize={pageSize}";
    }
}
