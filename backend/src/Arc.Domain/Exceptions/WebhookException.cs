namespace Arc.Domain.Exceptions;

public sealed class WebhookException : DomainException
{
    public WebhookException(string message) : base(message)
    {
    }
}