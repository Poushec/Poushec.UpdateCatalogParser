namespace Poushec.UpdateCatalog.Exceptions;

public class UpdateWasNotFoundException : System.Exception
{
    public UpdateWasNotFoundException() : base() { }
    public UpdateWasNotFoundException(string message) : base(message) { }
    public UpdateWasNotFoundException(string message, System.Exception innerException) : base(message, innerException) { }
}