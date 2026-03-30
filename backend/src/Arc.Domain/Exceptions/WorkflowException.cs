namespace Arc.Domain.Exceptions;

public sealed class WorkflowException : DomainException
{
    public WorkflowException(string message) : base(message)
    {
    }
}