namespace Poushec.UpdateCatalogParser.Exceptions
{
    public class UnableToCollectUpdateDetailsException : System.Exception 
    {
        public UnableToCollectUpdateDetailsException() : base() { }
        public UnableToCollectUpdateDetailsException(string message) : base(message) { }
        public UnableToCollectUpdateDetailsException(string message, System.Exception innerException) : base(message, innerException) { }
    }
}