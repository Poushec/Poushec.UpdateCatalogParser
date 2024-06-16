namespace Poushec.UpdateCatalogParser.Exceptions
{
    public class RequestToCatalogTimedOutException : System.Exception
    {
        public RequestToCatalogTimedOutException() : base() { }
        public RequestToCatalogTimedOutException(string message) : base(message) { }
        public RequestToCatalogTimedOutException(string message, System.Exception innerException) : base(message, innerException) { }
    }
}