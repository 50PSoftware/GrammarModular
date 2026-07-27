using Grammar.Czech.Interfaces;
using System.Text.RegularExpressions;

namespace Grammar.Czech.Services
{
    /// <summary>
    /// Validates how a numeral is written where digits and letters meet.
    /// </summary>
    /// <remarks>
    /// The rule the Internetová jazyková příručka states (id=790, 160, 785) is short: a numeral written in
    /// digits takes no ending and no hyphen. So 20krát, 12procentní and 8metrový are right, while 20-krát,
    /// 12-ti-procentní, od 10-ti let and 5tý are all wrong. An ordinal is marked by a full stop instead —
    /// 5. patro, 28. října — and the one place a hyphen belongs is after a placeholder letter: x-stupňový,
    /// n-tá.
    /// <para>
    /// Deliberately separate from <see cref="ICzechOrthographyService"/>, which is about the morphonology of
    /// endings — jotation, ě against e — and has nothing to do with how digits are spelled out.
    /// </para>
    /// </remarks>
    public class CzechNumeralOrthographyService : ICzechNumeralOrthographyService
    {
        // A single letter standing in for a number: x-stupňový, n-tá. The only place a hyphen is correct.
        private static readonly Regex Placeholder = new(@"^\p{L}-\p{L}+$", RegexOptions.Compiled);

        private static readonly Regex DigitsOnly = new(@"^\d+$", RegexOptions.Compiled);

        // An ordinal in digits: 5., 28., 1953.
        private static readonly Regex OrdinalWithPeriod = new(@"^\d+\.$", RegexOptions.Compiled);

        // A hyphen anywhere after digits: 20-krát, 12-ti-procentní, 5-ti.
        private static readonly Regex HyphenAfterDigits = new(@"^\d+-", RegexOptions.Compiled);

        // The spelled-out ending of an oblique case glued to digits: 10ti, 8mi, 12tiprocentní.
        private static readonly Regex CaseEndingAfterDigits = new(@"^\d+(ti|mi)", RegexOptions.Compiled);

        // An ordinal ending glued to digits: 5tý, 19tá, 8mý, 8mého, o 5tém. Both consonants have to be
        // covered because the ending copies the spelled-out numeral — pátý gives t, osmý gives m. The long
        // vowel is what keeps 8metrový and 300korunová out of it.
        private static readonly Regex OrdinalEndingAfterDigits = new(@"^\d+[tm]([ýáéí]|ou)", RegexOptions.Compiled);

        // Digits joined straight to a word, which is the correct pattern: 20krát, 256členná, 8metrový.
        private static readonly Regex DigitsThenWord = new(@"^(\d+)(\p{L}+)$", RegexOptions.Compiled);

        /// <summary>
        /// Determines whether the token is written correctly.
        /// </summary>
        /// <param name="token">The single token to check, without surrounding whitespace.</param>
        /// <param name="reason">The Czech explanation of what is wrong, or null when the token is valid.</param>
        /// <returns><see langword="true"/> when the token is correct; otherwise, <see langword="false"/>.</returns>
        public bool IsValid(string token, out string? reason)
        {
            reason = null;

            if (string.IsNullOrWhiteSpace(token))
            {
                return true;
            }

            if (Placeholder.IsMatch(token) || DigitsOnly.IsMatch(token) || OrdinalWithPeriod.IsMatch(token))
            {
                return true;
            }

            if (HyphenAfterDigits.IsMatch(token))
            {
                reason = $"'{token}': k číslici se nepřipojuje spojovník — správně je například 20krát, 12procentní.";

                return false;
            }

            if (CaseEndingAfterDigits.IsMatch(token))
            {
                reason = $"'{token}': k číslici se nepřipojuje koncovka -ti/-mi — správně je například od 10 let, 12procentní.";

                return false;
            }

            if (OrdinalEndingAfterDigits.IsMatch(token))
            {
                reason = $"'{token}': řadová číslovka se v číslicích píše s tečkou — správně je například 5., 28.";

                return false;
            }

            return true;
        }

        /// <summary>
        /// Rewrites an incorrectly written token into its correct form.
        /// </summary>
        /// <param name="token">The single token to rewrite.</param>
        /// <returns>The corrected token, or the token unchanged when it is already correct.</returns>
        public string Normalize(string token)
        {
            if (IsValid(token, out _))
            {
                return token;
            }

            // An ordinal glued to digits loses the ending and gains the full stop: 5tý → 5.
            if (OrdinalEndingAfterDigits.IsMatch(token))
            {
                return DigitsThenWord.Match(token) is { Success: true } ordinal
                    ? ordinal.Groups[1].Value + "."
                    : token;
            }

            // Drop the hyphens and any -ti-/-mi- padding, then keep whatever word is left attached:
            // 12-ti-procentní → 12procentní, 10ti → 10, 20-krát → 20krát.
            var stripped = Regex.Replace(token, @"^(\d+)-?(?:ti|mi)?-?", "$1");

            if (DigitsOnly.IsMatch(stripped) || DigitsThenWord.IsMatch(stripped))
            {
                return stripped;
            }

            return token;
        }
    }
}
