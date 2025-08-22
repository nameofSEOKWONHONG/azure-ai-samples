namespace Document.Intelligence.Agent.Infrastructure.Data;

/// <summary>
/// JSON RESPONSE의 대한 공통 사항
/// </summary>
/// <typeparam name="T"></typeparam>
public class Results<T>
{
    public bool IsSucceed { get; set; }
    public IEnumerable<string> Messages { get; set; }
    public T Data { get; set; }

    public static Results<T> Success(T data) => new() { IsSucceed = true, Data = data};

    public static Task<Results<T>> SuccessAsync(T data) => Task.FromResult(Success(data));
    public static Task<Results<T>> FailAsync() => Task.FromResult(new Results<T>() { IsSucceed = false });
    public static Task<Results<T>> FailAsync(string message) => Task.FromResult(new Results<T>() { IsSucceed = false, Messages = [message]});
    public static Task<Results<T>> FailAsync(IEnumerable<string> messages) => Task.FromResult(new Results<T>() {IsSucceed = false, Messages = messages});
}