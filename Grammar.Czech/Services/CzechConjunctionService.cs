using Grammar.Core.Enums;
using Grammar.Czech.Interfaces;
using Grammar.Czech.Models;

namespace Grammar.Czech.Services
{
    /// <summary>
    /// Provides Czech conjunction lookup: how a conjunction joins clauses, what relation it establishes,
    /// whether a comma precedes it, and where in the clause it stands.
    /// </summary>
    public class CzechConjunctionService : ICzechConjunctionService
    {
        private readonly Dictionary<string, ConjunctionData> _conjunctions;
        private readonly ICzechCliticService _cliticService;

        /// <summary>
        /// Initializes a new instance of the <see cref="CzechConjunctionService"/> type.
        /// </summary>
        public CzechConjunctionService(IConjunctionDataProvider dataProvider, ICzechCliticService cliticService)
        {
            _conjunctions = dataProvider.GetConjunctions();
            _cliticService = cliticService;
        }

        /// <summary>
        /// Gets how the supplied conjunction joins clauses.
        /// </summary>
        /// <param name="conjunction">The conjunction text to look up.</param>
        /// <returns>The conjunction type.</returns>
        public ConjunctionType GetType(string conjunction, ConjunctionType? reading = null)
            => Lookup(conjunction, reading).Type;

        /// <summary>
        /// Gets the relation the supplied conjunction establishes between what it joins.
        /// </summary>
        /// <param name="conjunction">The conjunction text to look up.</param>
        /// <returns>The semantic group of the conjunction.</returns>
        public ConjunctionSemanticGroup GetSemanticGroup(string conjunction, ConjunctionType? reading = null)
            => Lookup(conjunction, reading).SemanticGroup;

        /// <summary>
        /// Determines whether a comma is written before the supplied conjunction.
        /// </summary>
        /// <param name="conjunction">The conjunction text to look up.</param>
        /// <returns><see langword="true"/> when a comma precedes the conjunction; otherwise, <see langword="false"/>.</returns>
        public bool RequiresComma(string conjunction, ConjunctionType? reading = null)
            => Lookup(conjunction, reading).RequiresComma;

        /// <summary>
        /// Determines whether the conjunction occupies the first position of the clause it introduces,
        /// which is what the clitic cluster attaches after.
        /// </summary>
        /// <param name="conjunction">The conjunction text to look up.</param>
        /// <returns><see langword="true"/> for a subordinating conjunction; otherwise, <see langword="false"/>.</returns>
        /// <remarks>
        /// A subordinating conjunction belongs to its clause and fills first position, so the cluster follows
        /// it directly: "protože se Petr umyl". A coordinating one stands outside the clause and leaves first
        /// position to whatever comes next: "a Petr se umyl". NESČ states the same asymmetry — subordinating
        /// conjunctions bear on clitic placement, coordinating ones do not.
        /// </remarks>
        public bool OccupiesFirstPosition(string conjunction) => GetType(conjunction) == ConjunctionType.Subordinating;

        /// <summary>
        /// Determines whether the conjunction stands after the first constituent of its clause rather than
        /// in front of it.
        /// </summary>
        /// <param name="conjunction">The conjunction text to look up.</param>
        /// <returns><see langword="true"/> for však; otherwise, <see langword="false"/>.</returns>
        public bool OccupiesSecondPosition(string conjunction, ConjunctionType? reading = null)
            => Lookup(conjunction, reading).SecondPosition;

        /// <summary>
        /// Gets the second member of a paired conjunction.
        /// </summary>
        /// <param name="conjunction">The opening member to look up.</param>
        /// <returns>The second member, or <see langword="null"/> when the conjunction is not paired.</returns>
        public string? GetCorrelate(string conjunction, ConjunctionType? reading = null)
            => Lookup(conjunction, reading).Correlate;

        /// <summary>
        /// Determines whether the conjunction absorbs the conditional auxiliary and inflects with it.
        /// </summary>
        /// <param name="conjunction">The conjunction text to look up.</param>
        /// <returns><see langword="true"/> for aby and kdyby; otherwise, <see langword="false"/>.</returns>
        public bool FusesWithConditional(string conjunction) => Lookup(conjunction, null).FusesWithConditional;

        /// <summary>
        /// Builds the surface form of the conjunction for the requested grammatical number and person.
        /// </summary>
        /// <param name="conjunction">The conjunction text to look up.</param>
        /// <param name="number">The grammatical number of the dependent clause's predicate.</param>
        /// <param name="person">The grammatical person of the dependent clause's predicate.</param>
        /// <returns>The inflected form where the conjunction fuses; otherwise, the conjunction unchanged.</returns>
        /// <remarks>
        /// Composed rather than stored. aby is a + the conditional auxiliary and kdyby is kdy + the same
        /// auxiliary, so the twelve forms fall out of the five particles already in the clitic data:
        /// abych, abys, aby, abychom, abyste, aby and the kdyby row beside it. The third person takes the
        /// bare by in both numbers, which is why aby and kdyby are also their own third-person forms.
        /// <para>
        /// This is what rules out the widespread *aby jsi and *aby jste: the auxiliary that fuses here is the
        /// conditional one, and the conditional has no jsi or jste to contribute.
        /// </para>
        /// </remarks>
        public string GetForm(string conjunction, Number? number, Person? person)
        {
            var data = Lookup(conjunction, null);

            if (!data.FusesWithConditional)
            {
                return conjunction;
            }

            // Nothing to agree with leaves the third-person form, which is the lemma itself.
            if (number is null || person is null)
            {
                return conjunction;
            }

            return data.Stem + _cliticService.GetConditionalParticle(number, person);
        }

        /// <summary>
        /// Gets every reading the supplied conjunction has, the primary one first.
        /// </summary>
        /// <param name="conjunction">The conjunction text to look up.</param>
        /// <returns>The readings registered for it.</returns>
        public IReadOnlyList<ConjunctionData> GetReadings(string conjunction)
        {
            var primary = Lookup(conjunction, null);

            return new[] { primary }.Concat(primary.AlsoReads).ToList();
        }

        /// <summary>
        /// Gets the conjunctions that join clauses in the requested way, the least marked one first.
        /// </summary>
        /// <param name="type">Whether the clauses are of equal rank or one depends on the other.</param>
        /// <param name="semanticGroup">The relation between them.</param>
        /// <returns>The matching conjunctions, or an empty sequence when none is registered.</returns>
        public IReadOnlyList<string> GetConjunctionsFor(ConjunctionType type, ConjunctionSemanticGroup semanticGroup)
        {
            static bool Matches(ConjunctionData data, ConjunctionType type, ConjunctionSemanticGroup group)
                => data.Type == type && data.SemanticGroup == group;

            // Primary readings first: a conjunction that is normally this kind is a better answer than one
            // that only reads this way in a particular construction.
            var primary = _conjunctions
                .Where(entry => Matches(entry.Value, type, semanticGroup))
                .Select(entry => entry.Key);

            var secondary = _conjunctions
                .Where(entry => !Matches(entry.Value, type, semanticGroup)
                    && entry.Value.AlsoReads.Any(reading => Matches(reading, type, semanticGroup)))
                .Select(entry => entry.Key);

            return primary.Concat(secondary).ToList();
        }

        // Reported rather than papered over, though not quite because the class is closed. NESČ puts it more
        // carefully: conjunctions proper are a closed set, but they are the core of an open class of
        // connective expressions, so an unlisted word is not automatically not a conjunction. The reason to
        // refuse is narrower and holds anyway — the comma and the clitic position both follow from which
        // kind it is, and neither can be guessed from the word itself.
        //
        // A reading asked for by type is answered from the alternatives when the primary one is the other
        // type: ať subordinates a content clause and coordinates a split one, and the caller building a
        // coordination knows which it means even though the lemma does not.
        private ConjunctionData Lookup(string conjunction, ConjunctionType? reading)
        {
            if (!_conjunctions.TryGetValue(conjunction, out var data))
            {
                throw new InvalidOperationException(
                    $"Neznámá spojka '{conjunction}'. Doplň ji do conjunctions.json.");
            }

            if (reading is null || data.Type == reading)
            {
                return data;
            }

            return data.AlsoReads.FirstOrDefault(alternative => alternative.Type == reading)
                ?? throw new InvalidOperationException(
                    $"Spojka '{conjunction}' nemá {reading} čtení — je {data.Type}. "
                    + "Pokud ho má mít, doplň ho do alsoReads v conjunctions.json.");
        }
    }
}
