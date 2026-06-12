namespace PosSystem.SharedKernel;
 
public sealed class Result<T>
{
    public bool IsSuccess { get; }
    public T? Value { get; }
    public string? Error { get; }
    public int StatusCode { get; }
 
    private Result(bool success, T? value, string? error, int statusCode)
        => (IsSuccess, Value, Error, StatusCode) = (success, value, error, statusCode);
 
    public static Result<T> Ok(T value) => new(true, value, null, 200);
    public static Result<T> Created(T value) => new(true, value, null, 201);
    public static Result<T> NotFound(string msg) => new(false, default, msg, 404);
    public static Result<T> Fail(string msg, int code = 400) => new(false, default, msg, code);
    public static Result<T> Unauthorized(string msg = "Unauthorized") => new(false, default, msg, 401);
}
 
public record PagedResult<T>(IEnumerable<T> Items, int Page, int PageSize, int Total)
{
    public int TotalPages => (int)Math.Ceiling((double)Total / PageSize);
}
 
