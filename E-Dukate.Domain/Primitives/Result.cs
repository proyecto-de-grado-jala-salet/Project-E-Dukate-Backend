namespace E_Dukate.Domain.Primitives;

public class Result
{
    public bool IsSuccess { get; }
    public string ErrorMessage { get; }

    protected Result(bool isSuccess, string errorMessage)
    {
        IsSuccess = isSuccess;
        ErrorMessage = errorMessage;
    }

    public static Result Success() => new Result(true, null);
    public static Result Failure(string errorMessage) => new Result(false, errorMessage);
}