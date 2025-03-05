namespace CleanWebApiTemplate.Host.Routes.Todo.Update;

public class UpdateTodoRequest
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public byte? Status { get; set; }
}
