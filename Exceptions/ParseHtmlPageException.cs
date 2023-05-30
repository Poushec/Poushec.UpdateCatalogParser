namespace Poushec.UpdateCatalogParser.Exceptions;

public class ParseHtmlPageException : System.Exception 
{
    public ParseHtmlPageException() : base() { }
    public ParseHtmlPageException(string message) : base(message) { }
    public ParseHtmlPageException(string message, System.Exception innerException) : base(message, innerException) { }
}