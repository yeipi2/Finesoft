namespace fs_backend.Util;

public class ServiceResult<T>
{
    public bool Succeeded { get; set; }
    public T? Data { get; set; }
    public IEnumerable<string> Errors { get; set; } = Enumerable.Empty<string>();

    public static ServiceResult<T> Success(T data) => new() { Succeeded = true, Data = data };
    public static ServiceResult<T> Failure(IEnumerable<string> errors) => new() { Succeeded = false, Errors = errors };
    public static ServiceResult<T> Failure(string error) => new() { Succeeded = false, Errors = new[] { error } };
}