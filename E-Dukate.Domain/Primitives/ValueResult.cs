namespace E_Dukate.Domain.Primitives;

public class ValueResult<T>
{
    public bool IsSuccess { get; }
    public string ErrorMessage { get; }
    public T? Value { get; }

    protected ValueResult(bool isSuccess, T? value, string errorMessage)
    {
        IsSuccess = isSuccess;
        Value = value;
        ErrorMessage = errorMessage ?? string.Empty;
    }

    public static ValueResult<T> Success(T value) => new ValueResult<T>(true, value, string.Empty);
    public static ValueResult<T> Failure(string errorMessage) => new ValueResult<T>(false, default, errorMessage ?? string.Empty);
}