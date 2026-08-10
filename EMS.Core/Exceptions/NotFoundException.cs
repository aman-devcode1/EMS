using System.Net;

namespace EMS.Core.Exceptions;

public class NotFoundException : Exception
{
    // 👇 Read-Only Property. Middleware इसे पढ़कर Response Status Code Set करेगा.
    public HttpStatusCode StatusCode { get; } = HttpStatusCode.NotFound;  // 404
    
    // 👇 Constructor (निर्माता). : base(message) Parent Class (Exception) को Message भेजता है.
    public NotFoundException(string message) : base(message)
    {
    }

    // 👇 Optional Constructor (अगर किसी और Exception को Wrap करना हो).
    public NotFoundException(string message, Exception innerException) : base(message, innerException)
    {
    }
    
}