using System.Net;

namespace EMS.Core.Exceptions;

public class ExternalServiceException : Exception
{
    // 👇 Read-Only Property. Middleware isse padhkar Response Status Code Set karega.
    public HttpStatusCode StatusCode { get; } = HttpStatusCode.ServiceUnavailable;  // 503

    public ExternalServiceException(string message) : base(message)
    {
    }

    public ExternalServiceException(string message, Exception innerException) : base(message, innerException)
    {
    }
}