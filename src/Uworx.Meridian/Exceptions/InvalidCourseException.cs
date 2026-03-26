namespace Uworx.Meridian.Exceptions;

public class InvalidCourseException : Exception
{
    public InvalidCourseException(string message) : base(message)
    {
    }

    public InvalidCourseException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
