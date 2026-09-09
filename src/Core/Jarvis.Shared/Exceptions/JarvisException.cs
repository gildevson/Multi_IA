namespace Jarvis.Shared.Exceptions;

public class JarvisException : Exception
{
    public string? Code { get; }

    public JarvisException(string message) : base(message) { }

    public JarvisException(string message, string code) : base(message)
    {
        Code = code;
    }

    public JarvisException(string message, Exception inner) : base(message, inner) { }
}