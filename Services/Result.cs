namespace TaskManager.Services;

public sealed class Result
{
    public string? Error { get; }
    public bool Succeeded => Error is null;

    private Result(string? error) => Error = error;

    public static Result Success() => new(null);
    public static Result Fail(string error) => new(error);
}

public sealed class Result<T>
{
    public T? Value { get; }
    public string? Error { get; }
    public bool Succeeded => Error is null;

    private Result(T? value, string? error)
    {
        Value = value;
        Error = error;
    }

    public static Result<T> Success(T value) => new(value, null);
    public static Result<T> Fail(string error) => new(default, error);
}
