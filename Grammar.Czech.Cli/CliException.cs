namespace Grammar.Czech.Cli
{
    /// <summary>
    /// Signals a failure the user caused and can fix, as opposed to a defect in the library.
    /// </summary>
    /// <remarks>
    /// Carried separately so the top level can print the message alone. A mistyped case is not a stack
    /// trace, and printing one for it makes a tool look broken when it merely did not understand.
    /// </remarks>
    public sealed class CliException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CliException"/> type.
        /// </summary>
        /// <param name="message">The message explaining what to do differently.</param>
        public CliException(string message) : base(message)
        { }
    }
}
