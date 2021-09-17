using System;

namespace UpdateCatalog.Exceptions
{
    public class CatalogErrorException : Exception
    {
        public CatalogErrorException() : base() { }
        public CatalogErrorException(string message) : base(message) { }
    }
    public class CatalogNoResultsException : Exception
    {
        public CatalogNoResultsException() : base() { }
        public CatalogNoResultsException(string message) : base(message) { }
    } 

    public class UnableToCollectUpdateDetailsException : Exception 
    {
        public UnableToCollectUpdateDetailsException() : base() { }
        public UnableToCollectUpdateDetailsException(string message) : base(message) { }
    }

    public class RequestToCatalogTimedOutException : Exception
    {
        public RequestToCatalogTimedOutException() : base() { }
        public RequestToCatalogTimedOutException(string message) : base(message) { }
    }

    public class ParseHtmlPageException : Exception 
    {
        public ParseHtmlPageException() : base() { }
        public ParseHtmlPageException(string message) : base(message) { }
    }

    public class UpdateWasNotFoundException : Exception
    {
        public UpdateWasNotFoundException() : base() { }
        public UpdateWasNotFoundException(string message) : base(message) { }
    }
}