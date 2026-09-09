namespace Jarvis.Shared.Models;

public class Result
{
    public bool IsSuccess { get; protected set; }
    public bool IsFailure => !IsSuccess;
    public string Error { get; protected set; } = string.Empty;

    protected Result() { }

    public static Result Success() => new() { IsSuccess = true };
    public static Result Failure(string error) => new() { IsSuccess = false, Error = error };

    public static Result<T> Success<T>(T value) => Result<T>.Success(value);
    public static Result<T> Failure<T>(string error) => Result<T>.Failure(error);
}

public class Result<T> : Result
{
    private T? _value;

    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException($"Cannot access Value of a failed result. Error: {Error}");

    private Result() { }

    public new static Result<T> Success(T value) => new() { IsSuccess = true, _value = value };
    public new static Result<T> Failure(string error) => new() { IsSuccess = false, Error = error };

    public static implicit operator Result<T>(T value) => Success(value);

    public Result<TOut> Map<TOut>(Func<T, TOut> map) =>
        IsSuccess ? Result<TOut>.Success(map(Value)) : Result<TOut>.Failure(Error);

    public async Task<Result<TOut>> MapAsync<TOut>(Func<T, Task<TOut>> map)
    {
        if (IsFailure) return Result<TOut>.Failure(Error);
        var mapped = await map(Value);
        return Result<TOut>.Success(mapped);
    }
}