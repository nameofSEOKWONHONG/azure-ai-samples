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

public class PagedResults<T>
{
    public PagedResults(List<T> data)
    {
        Data = data;
    }

    public IEnumerable<T> Data { get; set; }

    internal PagedResults(bool succeeded, IEnumerable<T> data = default, List<string> messages = null, int count = 0, int page = 1, int pageSize = 10)
    {
        Data = data;
        PageNo = page;
        Succeeded = succeeded;
        PageSize = pageSize;
        TotalPages = (int)Math.Ceiling(count / (double)pageSize);
        TotalCount = count;
    }

    public static PagedResults<T> Fail()
    {
        return Fail(new List<string>() { });
    }
    
    public static PagedResults<T> Fail(string message)
    {
        return Fail(new List<string>() { message });
    }

    public static PagedResults<T> Fail(List<string> messages)
    {
        return new PagedResults<T>(false, default, messages);
    }

    public static Task<PagedResults<T>> FailAsync()
    {
        return FailAsync(new List<string>() { });
    }

    public static Task<PagedResults<T>> FailAsync(string message)
    {
        return FailAsync(new List<string>() { message });
    }

    public static Task<PagedResults<T>> FailAsync(List<string> messages)
    {
        return Task.FromResult(new PagedResults<T>(false, default, messages));
    }

    public static PagedResults<T> Success(IEnumerable<T> data, int totalCount, int currentPage, int pageSize)
    {
        var result = new PagedResults<T>(true, data, null, totalCount, currentPage, pageSize);
        result.Messages = new List<string>() { "Success." };
        return result;
    }

    public static Task<PagedResults<T>> SuccessAsync(IEnumerable<T> data, int totalCount, int currentPage,
        int pageSize)
    {
        return Task.FromResult(Success(data, totalCount, currentPage, pageSize));
    }

    public int PageNo { get; set; }

    public int TotalPages { get; set; }

    public int TotalCount { get; set; }
    public int PageSize { get; set; }

    public bool HasPreviousPage => PageNo > 1;

    public bool HasNextPage => PageNo < TotalPages;

    public List<string> Messages { get; set; } = new List<string>();
    
    public Dictionary<string, string> ValidateErrors { get; set; }

    public bool Succeeded { get; set; }
}