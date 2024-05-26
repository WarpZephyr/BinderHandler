namespace BinderHandler.Hashes
{
    /// <summary>
    /// Reports when a value is a duplicate.
    /// </summary>
    public class DuplicateValueException : Exception
    {
        public DuplicateValueException() { }
        public DuplicateValueException(string message) : base(message) { }
        public DuplicateValueException(string message, Exception inner) : base(message, inner) { }
    }
}
