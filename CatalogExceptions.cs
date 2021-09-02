using System;

namespace UpdateCatalog.Exceptions
{
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
}