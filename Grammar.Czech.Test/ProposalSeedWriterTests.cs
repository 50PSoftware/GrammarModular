using Grammar.Czech.Lexicon.Tool;

namespace Grammar.Czech.Test
{
    /// <summary>
    /// Verifies which of the collected proposals a draft seed actually keeps.
    /// </summary>
    [TestClass]
    public sealed class ProposalSeedWriterTests
    {
        /// <summary>
        /// Verifies that a rejected proposal never ends up in the draft, even without
        /// <c>--jen-potvrzene</c> — somebody already looked at it and turned it down, which is a
        /// stronger verdict than "not yet confirmed" and should never be second-guessed by the flag
        /// that otherwise includes every unconfirmed guess.
        /// </summary>
        [TestMethod]
        public void RejectedProposalIsExcludedRegardlessOfOnlyConfirmed()
        {
            var root = Directory.CreateTempSubdirectory();

            try
            {
                var proposalsPath = Path.Combine(root.FullName, "navrhy.json");

                File.WriteAllText(proposalsPath, """
                    [
                      { "Lemma": "dobreslovo", "Category": "Noun", "IsConfirmed": false, "IsRejected": false },
                      { "Lemma": "spatneslovo", "Category": "Noun", "IsConfirmed": false, "IsRejected": true }
                    ]
                    """);

                var seedPath = ProposalSeedWriter.Write(proposalsPath, root.FullName, onlyConfirmed: false);

                Assert.IsNotNull(seedPath);

                var content = File.ReadAllText(seedPath);

                StringAssert.Contains(content, "dobreslovo");
                Assert.IsFalse(content.Contains("spatneslovo"), "Zamítnuté slovo nemá být v návrhu seedu.");
            }
            finally
            {
                root.Delete(recursive: true);
            }
        }
    }
}
