namespace SleepEditWeb.Application.Protocol;

public class ProtocolResult
{
    protected ProtocolResult(bool isSuccess, string errorCode, string errorMessage)
    {
        IsSuccess = isSuccess;
        ErrorCode = isSuccess ? string.Empty : (errorCode ?? string.Empty);
        ErrorMessage = isSuccess ? string.Empty : (errorMessage ?? string.Empty);
    }

    public bool IsSuccess { get; }

    public bool IsFailure => !IsSuccess;

    public string ErrorCode { get; }

    public string ErrorMessage { get; }

    public static ProtocolResult Success()
    {
        return new ProtocolResult(isSuccess: true, errorCode: string.Empty, errorMessage: string.Empty);
    }

    public static ProtocolResult Failure(string errorCode, string errorMessage)
    {
        return new ProtocolResult(isSuccess: false, errorCode, errorMessage);
    }

    public ProtocolResult<T> ToFailure<T>()
    {
        return ProtocolResult<T>.Failure(ErrorCode, ErrorMessage);
    }
}

public sealed class ProtocolResult<T> : ProtocolResult
{
    private readonly T? _value;

    private ProtocolResult(T value)
        : base(isSuccess: true, errorCode: string.Empty, errorMessage: string.Empty)
    {
        _value = value;
    }

    private ProtocolResult(string errorCode, string errorMessage)
        : base(isSuccess: false, errorCode, errorMessage)
    {
        _value = default;
    }

    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Cannot access Value for a failed result.");

    public static ProtocolResult<T> Success(T value)
    {
        return new ProtocolResult<T>(value);
    }

    public new static ProtocolResult<T> Failure(string errorCode, string errorMessage)
    {
        return new ProtocolResult<T>(errorCode, errorMessage);
    }
}
