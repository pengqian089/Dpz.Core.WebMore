namespace Dpz.Core.WebMore.Models;

public class ResultInformation
{
    public bool Success { get; set; }

    public string Message { get; set; } = "操作成功";
}

public class ResultInformation<T> : ResultInformation
{
    public T? Data { get; set; }

    public static ResultInformation<T> ToSuccess(T data) => new() { Data = data, Success = true };

    public static ResultInformation<T> ToFail(string message) =>
        new() { Message = message, Success = false };
}
