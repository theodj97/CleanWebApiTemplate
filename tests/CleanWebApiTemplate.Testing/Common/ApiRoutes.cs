namespace CleanWebApiTemplate.Testing.Common;

public static class ApiRoutes
{
    public static class Todo
    {
        private const string BaseRoute = "api/Todo";
        public static string GetById(string id) => $"{BaseRoute}/{id}";
        public static string Update(string id) => $"{BaseRoute}/{id}";
        public static string Delete(string id) => $"{BaseRoute}/{id}";
        public static string Filter() => $"{BaseRoute}/filter";
        public static string Create() => BaseRoute;
        public static string GetTitles() => $"{BaseRoute}/titles";
    }
}
