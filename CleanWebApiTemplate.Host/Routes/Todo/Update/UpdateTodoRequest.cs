﻿namespace CleanWebApiTemplate.Host.Routes.Todo.Update;

public struct UpdateTodoRequest
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public int? Status { get; set; }
}
