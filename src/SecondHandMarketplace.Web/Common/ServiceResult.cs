namespace SecondHandMarketplace.Web.Common;

/// <summary>
/// Chuẩn kết quả trả về thống nhất cho toàn bộ Service và API.
/// Cấu trúc: Success, Code, Message, Data, Errors.
/// Tuân thủ quy ước thiết kế API Contract cho Core.
/// </summary>
public class ServiceResult<T>
{
    public bool Success { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }
    public IReadOnlyList<string>? Errors { get; set; }

    public static ServiceResult<T> Ok(T data, string message = "Thành công.", string code = "SUCCESS") =>
        new()
        {
            Success = true,
            Code = code,
            Message = message,
            Data = data,
            Errors = null
        };

    public static ServiceResult<T> Fail(string code, string message, IReadOnlyList<string>? errors = null) =>
        new()
        {
            Success = false,
            Code = code,
            Message = message,
            Data = default,
            Errors = errors ?? (string.IsNullOrWhiteSpace(message) ? Array.Empty<string>() : new[] { message })
        };

    public static ServiceResult<T> Fail(string code, string message, string error) =>
        Fail(code, message, new[] { error });
}

/// <summary>
/// ServiceResult phi generic cho các thao tác không cần trả về Data.
/// </summary>
public class ServiceResult
{
    public bool Success { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public object? Data { get; set; }
    public IReadOnlyList<string>? Errors { get; set; }

    public static ServiceResult Ok(string message = "Thành công.", string code = "SUCCESS", object? data = null) =>
        new()
        {
            Success = true,
            Code = code,
            Message = message,
            Data = data,
            Errors = null
        };

    public static ServiceResult Fail(string code, string message, IReadOnlyList<string>? errors = null) =>
        new()
        {
            Success = false,
            Code = code,
            Message = message,
            Data = null,
            Errors = errors ?? (string.IsNullOrWhiteSpace(message) ? Array.Empty<string>() : new[] { message })
        };

    public static ServiceResult Fail(string code, string message, string error) =>
        Fail(code, message, new[] { error });
}
