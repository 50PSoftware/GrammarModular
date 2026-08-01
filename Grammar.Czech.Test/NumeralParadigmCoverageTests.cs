using Grammar.Core.Enums;
using Grammar.Czech.Interfaces;
using Grammar.Czech.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Text;

namespace Grammar.Czech.Test
{
    /// <summary>
    /// Sweeps every registered numeral across every case, gender and number, and records where the data has
    /// no form.
    /// </summary>
    /// <remarks>
    /// CzechNumeralComposer falls back to the lemma when a form is missing, which is the last silent path
    /// left in the library. Unlike an unresolved lemma, that is not a bad request — it is a hole in a
    /// paradigm, and throwing at generation time would punish the caller for the library's own gaps. Making
    /// the holes visible here is the alternative: they get filled in the data, once, instead of surfacing as
    /// a wrong word at run time.
    /// </remarks>
    [TestClass]
    public sealed class NumeralParadigmCoverageTests
    {
        private static ICzechNumeralService numerals = null!;
        private static INumeralDataProvider data = null!;

        /// <summary>
        /// Builds the full service graph once for the whole fixture.
        /// </summary>
        [ClassInitialize]
        public static void SetupClass(TestContext _)
        {
            var services = new ServiceCollection();
            services.AddCzechGrammarServices();
            var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true });

            numerals = provider.GetRequiredService<ICzechNumeralService>();
            data = provider.GetRequiredService<INumeralDataProvider>();
        }

        /// <summary>
        /// The declinable numerals resolve in every case, so no caller can hit the composer's fallback
        /// through one of them.
        /// </summary>
        [TestMethod]
        public void EveryDeclinableNumeral_HasAFormInEveryCase()
        {
            var gaps = CollectGaps();

            Assert.AreEqual(
                0,
                gaps.Count,
                "Číslovky bez tvaru pro některou kombinaci:" + Environment.NewLine + Describe(gaps));
        }

        private static List<string> CollectGaps()
        {
            var gaps = new List<string>();

            foreach (var (lemma, entry) in data.GetNumerals())
            {
                // An indeclinable numeral has one form on purpose; it is not a gap.
                if (entry.Morphology == Enums.NumeralMorphology.Indeclinable)
                {
                    continue;
                }

                foreach (var @case in Enum.GetValues<Case>())
                {
                    foreach (var gender in Enum.GetValues<Gender>())
                    {
                        foreach (var number in Enum.GetValues<Number>())
                        {
                            foreach (var animate in new[] { true, false })
                            {
                                if (numerals.TryGetForm(lemma, @case, gender, number, animate, null) is null)
                                {
                                    gaps.Add($"{lemma} · {@case} · {gender} · {number} · {(animate ? "živ." : "neživ.")}");
                                }
                            }
                        }
                    }
                }
            }

            return gaps;
        }

        // Grouped by lemma, because a whole missing gender slot would otherwise read as dozens of failures.
        private static string Describe(List<string> gaps)
        {
            var report = new StringBuilder();

            foreach (var group in gaps.GroupBy(gap => gap.Split(" · ")[0]).OrderByDescending(g => g.Count()))
            {
                report.AppendLine($"  {group.Key}: {group.Count()} kombinací, např. {group.First()}");
            }

            return report.ToString();
        }
    }
}
