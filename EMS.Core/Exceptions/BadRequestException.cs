using System.Net;

namespace EMS.Core.Exceptions;

public class BadRequestException : Exception
{
    // 👇 Read-Only Property. Middleware इसे पढ़कर Response Status Code Set करेगा.
    public HttpStatusCode StatusCode { get; } = HttpStatusCode.BadRequest;  // 400
    
    // 👇 Constructor (निर्माता). : base(message) Parent Class (Exception) को Message भेजता है.
    public BadRequestException(string message) : base(message)
    {
    }

    // 👇 Optional Constructor (अगर किसी और Exception को Wrap करना हो).
    public BadRequestException(string message, Exception innerException) : base(message, innerException)
    {
    }
    
}