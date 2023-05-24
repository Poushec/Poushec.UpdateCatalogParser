namespace Poushec.UpdateCatalog.Exceptions;

public class UpdateWasNotFoundException : System.Exception
{
    public UpdateWasNotFoundException() : base() { }
    public UpdateWasNotFoundException(string message) : base(message) { }
}