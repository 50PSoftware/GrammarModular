using Grammar.Core.Enums;
using Grammar.Czech.Enums;
using Grammar.Czech.Interfaces;
using Grammar.Czech.Models;
using Grammar.Czech.Models.Syntax;

namespace Grammar.Czech.Services
{
    /// <summary>
    /// Assembles a Czech clause into a surface sentence: agreement, functional sentence perspective,
    /// and Wackernagel placement of the clitic cluster.
    /// </summary>
    public class CzechSentenceBuilder
    {
        private readonly CzechWordFormComposer composer;
        private readonly ICzechParticleService particleService;

        /// <summary>
        /// Initializes a new instance of the <see cref="CzechSentenceBuilder"/> type.
        /// </summary>
        public CzechSentenceBuilder(CzechWordFormComposer composer, ICzechParticleService particleService)
        {
            this.composer = composer;
            this.particleService = particleService;
        }

        /// <summary>
        /// Builds the surface sentence for the supplied clause.
        /// </summary>
        /// <param name="clause">The clause to linearize.</param>
        /// <returns>The assembled sentence, capitalized and terminated.</returns>
        public string Build(CzechClause clause)
        {
            var predicate = ApplySubjectAgreement(clause);

            // FSP: contrastive material is fronted, given material forms the theme before the verb,
            // new material forms the rheme after it. Order inside one status is the caller's.
            var preVerbal = clause.Elements
                .Where(element => element.Status == InformationStatus.Contrastive)
                .Concat(clause.Elements.Where(element => element.Status == InformationStatus.Given))
                .Select(element => composer.GetFullForm(element.Word).Form)
                .ToList();

            var postVerbal = clause.Elements
                .Where(element => element.Status == InformationStatus.New)
                .Select(element => composer.GetFullForm(element.Word).Form)
                .ToList();

            var (verbRest, clitics) = SplitOffClitics(predicate);

            var words = BuildLinearOrder(preVerbal, verbRest, clitics, postVerbal);

            return Capitalize(string.Join(' ', words)) + clause.Terminator;
        }

        // The clitic cluster attaches to the first constituent of the clause, whatever that constituent is.
        // With no pre-verbal constituent the verb itself opens the clause and the cluster follows its first word
        // (Budu se dělat); otherwise it follows the first constituent only, not all of them — which is why
        // "Petr včera se myl" is wrong and "Petr se včera myl" is right.
        private static List<string> BuildLinearOrder(
            List<string> preVerbal, List<string> verbRest, List<string> clitics, List<string> postVerbal)
        {
            var words = new List<string>();

            if (clitics.Count == 0)
            {
                words.AddRange(preVerbal);
                words.AddRange(verbRest);
                words.AddRange(postVerbal);
                return words;
            }

            if (preVerbal.Count > 0)
            {
                words.Add(preVerbal[0]);
                words.AddRange(clitics);
                words.AddRange(preVerbal.Skip(1));
                words.AddRange(verbRest);
            }
            else
            {
                words.Add(verbRest[0]);
                words.AddRange(clitics);
                words.AddRange(verbRest.Skip(1));
            }

            words.AddRange(postVerbal);
            return words;
        }

        // The builder owns the whole cluster, so it asks the composer for a phrase without the reflexive and
        // adds the particle itself. Letting the composer place it first and lifting it back out would break on
        // the contracted forms, where the auxiliary and the reflexive fuse into a single token (jsi se → ses).
        private (List<string> VerbRest, List<string> Clitics) SplitOffClitics(CzechWordRequest predicate)
        {
            var reflexiveType = predicate.ReflexiveType;

            predicate.HasPrecedingConstituent = false;
            predicate.ReflexiveType = ReflexiveType.None;

            var verbRest = new List<string>();
            var clitics = new List<string>();

            foreach (var word in composer.GetFullForm(predicate).Form.Split(' '))
            {
                (particleService.IsCliticAuxiliary(word) ? clitics : verbRest).Add(word);
            }

            // Rank 3: the reflexive follows any auxiliary already in the cluster.
            if (reflexiveType != ReflexiveType.None)
            {
                clitics.Add(particleService.GetReflexive(reflexiveType));
            }

            return (verbRest, particleService.ContractCluster(clitics).ToList());
        }

        // Person, number and gender of the predicate follow the nominative actor. Without an actor the clause
        // is subjectless or pro-drop and whatever the caller set on the predicate stands.
        private static CzechWordRequest ApplySubjectAgreement(CzechClause clause)
        {
            var predicate = clause.Predicate;

            var subject = clause.Elements
                .Where(element => element.Functor == FgdFunctor.ACT && element.Word.Case == Case.Nominative)
                .Select(element => (ClauseElement?)element)
                .FirstOrDefault();

            if (subject is null)
            {
                // Subjectless or pro-drop: nothing to agree with, so the predicate has to carry the
                // categories itself. Say so here rather than let a null person reach the conjugator.
                if (predicate.WordCategory == WordCategory.Verb && (predicate.Person is null || predicate.Number is null))
                {
                    throw new InvalidOperationException(
                        $"Klauze bez podmětu v nominativu (funktor ACT): predikát '{predicate.Lemma}' musí mít vyplněnou osobu a číslo.");
                }

                return predicate;
            }

            predicate.Person = ResolvePerson(subject.Word);
            predicate.Number = subject.Word.Number;
            predicate.Gender = subject.Word.Gender;

            return predicate;
        }

        private static Person ResolvePerson(CzechWordRequest subject)
        {
            if (subject.WordCategory != WordCategory.Pronoun)
            {
                return Person.Third;
            }

            return subject.Lemma switch
            {
                "já" or "my" => Person.First,
                "ty" or "vy" => Person.Second,
                _ => Person.Third
            };
        }

        private static string Capitalize(string sentence) =>
            string.IsNullOrEmpty(sentence)
                ? sentence
                : char.ToUpperInvariant(sentence[0]) + sentence[1..];
    }
}
