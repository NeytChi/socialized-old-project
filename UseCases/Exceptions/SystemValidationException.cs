namespace UseCases.Exceptions
{
    public class SystemValidationException : Exception
    {
        public SystemValidationException(string message) : base(message) { }
    }
}
