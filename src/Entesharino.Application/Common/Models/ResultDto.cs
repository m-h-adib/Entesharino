namespace Entesharino.Application.Common.Models;

public class ResultDto
{
    public bool Success { get; set; }
    public string? Message { get; set; }

    public static ResultDto Ok(string? message = null) =>
        new()
        {
            Success = true,
            Message = message
        };

    public static ResultDto Fail(string message) =>
        new()
        {
            Success = false,
            Message = message
        };
}

public class ResultDto<T> : ResultDto
{
    public T? Data { get; set; }

    public static ResultDto<T> Ok(T data, string? message = null) =>
        new()
        {
            Success = true,
            Message = message,
            Data = data
        };

    public static new ResultDto<T> Fail(string message) =>
        new()
        {
            Success = false,
            Message = message
        };
}

public class ResultOfList<T> : ResultDto
{
    public IReadOnlyList<T> Data { get; set; } = [];

    public int TotalCount { get; set; }

    public static ResultOfList<T> Ok(
        IReadOnlyList<T> data,
        int totalCount,
        string? message = null) =>
        new()
        {
            Success = true,
            Message = message,
            Data = data,
            TotalCount = totalCount
        };

    public static new ResultOfList<T> Fail(string message) =>
        new()
        {
            Success = false,
            Message = message
        };
}
