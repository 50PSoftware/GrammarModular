using Grammar.Czech.Interfaces;
using Grammar.Czech.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Grammar.Czech.Test
{
    /// <summary>
    /// Verifies which following words trigger the vocalized variant of a preposition.
    /// </summary>
    [TestClass]
    public sealed class PrepositionVocalizationTests
    {
        private static ICzechPrepositionService service = null!;

        /// <summary>
        /// Builds the full service graph once for the whole fixture.
        /// </summary>
        [ClassInitialize]
        public static void SetupClass(TestContext _)
        {
            var services = new ServiceCollection();
            services.AddCzechGrammarServices();
            service = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true })
                              .GetRequiredService<ICzechPrepositionService>();
        }

        /// <summary>
        /// Vocalization triggered by the same consonant, its voicing counterpart, the mn- cluster,
        /// or a sibilant-initial cluster after v and z.
        /// </summary>
        /// <param name="preposition">The preposition to place.</param>
        /// <param name="followingWord">The word that follows it.</param>
        /// <param name="expected">The expected surface form of the preposition.</param>
        [DataTestMethod]
        // same consonant or voicing pair
        [DataRow("v", "vodě", "ve")]
        [DataRow("v", "fázi", "ve")]
        [DataRow("s", "sestrou", "se")]
        [DataRow("s", "zahradou", "se")]
        [DataRow("s", "ženou", "se")]
        [DataRow("z", "země", "ze")]
        [DataRow("z", "školy", "ze")]
        [DataRow("k", "kamarádovi", "ke")]
        [DataRow("k", "gauči", "ke")]
        // mn- cluster
        [DataRow("s", "mnou", "se")]
        [DataRow("k", "mně", "ke")]
        [DataRow("v", "mně", "ve")]
        [DataRow("bez", "mne", "beze")]
        // sibilant-initial cluster after v
        [DataRow("v", "škole", "ve")]
        [DataRow("v", "smyslu", "ve")]
        // the second consonant of the cluster repeats the preposition's
        [DataRow("v", "dveřích", "ve")]
        [DataRow("v", "svém", "ve")]
        [DataRow("v", "dvou", "ve")]
        [DataRow("k", "skoku", "ke")]
        // three consonants running
        [DataRow("k", "středu", "ke")]
        [DataRow("s", "vstupem", "se")]
        [DataRow("v", "skladišti", "ve")]
        [DataRow("z", "vzpomínek", "ze")]
        public void Vocalize_TriggeringWord_ReturnsVocalizedForm(string preposition, string followingWord, string expected)
        {
            Assert.AreEqual(expected, service.Vocalize(preposition, followingWord));
        }

        /// <summary>
        /// A single consonant or a vowel leaves the preposition unchanged, and prepositions without a
        /// vocalized variant never take one.
        /// </summary>
        /// <param name="preposition">The preposition to place.</param>
        /// <param name="followingWord">The word that follows it.</param>
        [DataTestMethod]
        [DataRow("v", "lese")]
        [DataRow("v", "domě")]
        [DataRow("v", "Praze")]
        [DataRow("v", "autě")]
        [DataRow("z", "lesa")]
        [DataRow("k", "domu")]
        [DataRow("s", "bratrem")]
        // a syllabic preposition keeps its own vowel, even before the same consonant
        [DataRow("bez", "zákona")]
        [DataRow("od", "dveří")]
        // no vocalized variant at all
        [DataRow("na", "stole")]
        [DataRow("do", "domu")]
        [DataRow("o", "olympiádě")]
        public void Vocalize_NonTriggeringWord_ReturnsPrepositionUnchanged(string preposition, string followingWord)
        {
            Assert.AreEqual(preposition, service.Vocalize(preposition, followingWord));
        }

        /// <summary>
        /// An unknown preposition is passed through rather than rejected — the data is a working set,
        /// not a closed list.
        /// </summary>
        [TestMethod]
        public void Vocalize_UnknownPreposition_ReturnsItUnchanged()
        {
            Assert.AreEqual("přes", service.Vocalize("přes", "silnici"));
        }
    }
}
