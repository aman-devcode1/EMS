using System.Net;

namespace EMS.Core.Exceptions;

public class ConflictException : Exception
{
    // 👇 Read-Only Property. Middleware इसे पढ़कर Response Status Code Set करेगा.
    public HttpStatusCode StatusCode { get; } = HttpStatusCode.Conflict;  // 409
    
    // 👇 Constructor (निर्माता). : base(message) Parent Class (Exception) को Message भेजता है.
    public ConflictException(string message) : base(message)
    {
    }

    // 👇 Optional Constructor (अगर किसी और Exception को Wrap करना हो).
    public ConflictException(string message, Exception innerException) : base(message, innerException)
    {
    }
    
}