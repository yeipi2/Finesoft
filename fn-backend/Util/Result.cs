namespace fs_backend.Util;

public class Result<T>
{
    public bool Succeeded { get; private set; }
    public T? Data { get; private set; }
    public string[] Errors { get; private set; } = Array.Empty<string>();

    private Result() { }

    public static Result<T> Success(T data)
    {
        return new Result<T>
        {
            Succeeded = true,
            Data = data,
            Errors = Array.Empty<string>()
        };
    }

    public static Result<T> Failure(params string[] errors)
    {
        return new Result<T>
        {
            Succeeded = false,
            Data = default,
            Errors = errors
        };
    }

    public static Result<T> Failure(IEnumerable<string> errors)
    {
        return new Result<T>
        {
            Succeeded = false,
            Data = default,
            Errors = errors.ToArray()
        };
    }
}

public class Result
{
    public bool Succeeded { get; private set; }
    public string[] Errors { get; private set; } = Array.Empty<string>();

    private Result() { }

    public static Result Success()
    {
        return new Result
        {
            Succeeded = true,
            Errors = Array.Empty<string>()
        };
    }

    public static Result Failure(params string[] errors)
    {
        return new Result
        {
            Succeeded = false,
            Errors = errors
        };
    }

    public static Result Failure(IEnumerable<string> errors)
    {
        return new Result
        {
            Succeeded = false,
            Errors = errors.ToArray()
        };
    }
}
