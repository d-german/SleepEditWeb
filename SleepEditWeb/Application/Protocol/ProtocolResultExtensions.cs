namespace SleepEditWeb.Application.Protocol;

public static class ProtocolResultExtensions
{
    public static ProtocolResult<TOut> Map<TIn, TOut>(
        this ProtocolResult<TIn> result,
        Func<TIn, TOut> mapper)
    {
        if (result.IsFailure)
        {
            return ProtocolResult<TOut>.Failure(result.ErrorCode, result.ErrorMessage);
        }

        return ProtocolResult<TOut>.Success(mapper(result.Value));
    }

    public static ProtocolResult<TOut> Bind<TIn, TOut>(
        this ProtocolResult<TIn> result,
        Func<TIn, ProtocolResult<TOut>> binder)
    {
        if (result.IsFailure)
        {
            return ProtocolResult<TOut>.Failure(result.ErrorCode, result.ErrorMessage);
        }

        return binder(result.Value);
    }

    public static ProtocolResult<T> Tap<T>(
        this ProtocolResult<T> result,
        Action<T> action)
    {
        if (result.IsFailure)
        {
            return result;
        }

        action(result.Value);
        return result;
    }
}
