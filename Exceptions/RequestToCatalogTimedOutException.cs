namespace Poushec.UpdateCatalog.Exceptions;

public class RequestToCatalogTimedOutException : System.Exception
{
    public RequestToCatalogTimedOutException() : base() { }
    public RequestToCatalogTimedOutException(string message) : base(message) { }
}