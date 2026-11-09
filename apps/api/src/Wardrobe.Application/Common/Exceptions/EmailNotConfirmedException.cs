namespace Wardrobe.Application.Common.Exceptions;

public class EmailNotConfirmedException : Exception
{
    public EmailNotConfirmedException(string message = "Email is not confirmed.") : base(message)
    {
    }
}
