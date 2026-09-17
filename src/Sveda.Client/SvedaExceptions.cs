namespace Sveda.Client;

public class SvedaException : Exception
{
    public SvedaException(string message, Exception? inner = null)
        : base(message, inner)
    {
    }
}

public sealed class SvedaAuthenticationException : SvedaException
{
    public SvedaAuthenticationException(string message, Exception? inner = null)
        : base(message, inner)
    {
    }
}

public sealed class SvedaApiException : SvedaException
{
    public SvedaApiException(string message, int statusCode, string? body = null, Exception? inner = null)
        : base(message, inner)
    {
        StatusCode = statusCode;
        Body = body;
    }

    public int StatusCode { get; }

    public string? Body { get; }
}

public sealed class SvedaTransportException : SvedaException
{
    public SvedaTransportException(string message, Exception? inner = null)
        : base(message, inner)
    {
    }
}

public sealed class SvedaUnserializableResponseException : SvedaException
{
    public SvedaUnserializableResponseException(string message, Exception? inner = null)
        : base(message, inner)
    {
    }
}

public sealed class SvedaConfigurationException : SvedaException
{
    public SvedaConfigurationException(string message)
        : base(message)
    {
    }
}
