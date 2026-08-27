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

        /// <summary>
        /// Verifies that every proposal written into the draft gets marked with the seed file's own
        /// name back in <c>navrhy.json</c> — otherwise nothing would ever stop a second run from
        /// drafting the same lemma into a second seed.
        /// </summary>
        [TestMethod]
        public void ExportedProposalIsMarkedWithTheSeedFileItWentInto()
        {
            var root = Directory.CreateTempSubdirectory();

            try
            {
                var proposalsPath = Path.Combine(root.FullName, "navrhy.json");

                File.WriteAllText(proposalsPath, """
                    [
                      { "Lemma": "novinka", "Category": "Noun", "IsConfirmed": true, "IsRejected": false }
                    ]
                    """);

                var seedPath = ProposalSeedWriter.Write(proposalsPath, root.FullName, onlyConfirmed: false);

                Assert.IsNotNull(seedPath);
                StringAssert.Contains(File.ReadAllText(proposalsPath), Path.GetFileName(seedPath));
            }
            finally
            {
                root.Delete(recursive: true);
            }
        }

        /// <summary>
        /// Verifies that a proposal already marked with an <c>ExportedTo</c> seed is skipped on a later
        /// run — the whole point of marking it, since <c>navrhy.json</c> keeps the entry rather than
        /// removing it once it is drafted.
        /// </summary>
        [TestMethod]
        public void AlreadyExportedProposalIsNotDraftedAgain()
        {
            var root = Directory.CreateTempSubdirectory();

            try
            {
                var proposalsPath = Path.Combine(root.FullName, "navrhy.json");

                File.WriteAllText(proposalsPath, """
                    [
                      { "Lemma": "jizhotove", "Category": "Noun", "IsConfirmed": true, "IsRejected": false, "ExportedTo": "seed.005.sql" }
                    ]
                    """);

                var seedPath = ProposalSeedWriter.Write(proposalsPath, root.FullName, onlyConfirmed: false);

                Assert.IsNull(seedPath, "Návrh, který už jednou do seedu šel, se nemá draftovat znovu.");
            }
            finally
            {
                root.Delete(recursive: true);
            }
        }
    }
}
