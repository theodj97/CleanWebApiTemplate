﻿namespace CleanWebApiTemplate.Domain.ResultModel;

public class ResultModel
{
    public required int StatusCode { get; set; }
    public required string Title { get; set; }
    public required string Type { get; set; }
    public required string Detail { get; set; }
}
