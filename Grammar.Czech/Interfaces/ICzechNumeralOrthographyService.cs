namespace Grammar.Czech.Interfaces
{
    /// <summary>
    /// Validates how a numeral is written when digits and letters meet.
    /// </summary>
    public interface ICzechNumeralOrthographyService
    {
        /// <summary>
        /// Determines whether the token is written correctly.
        /// </summary>
        /// <param name="token">The single token to check, without surrounding whitespace.</param>
        /// <param name="reason">The Czech explanation of what is wrong, or null when the token is valid.</param>
        /// <returns><see langword="true"/> when the token is correct; otherwise, <see langword="false"/>.</returns>
        bool IsValid(string token, out string? reason);

        /// <summary>
        /// Rewrites an incorrectly written token into its correct form.
        /// </summary>
        /// <param name="token">The single token to rewrite.</param>
        /// <returns>The corrected token, or the token unchanged when it is already correct.</returns>
        string Normalize(string token);
    }
}
