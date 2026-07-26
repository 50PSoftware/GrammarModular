using Grammar.Core.Enums;
using Grammar.Czech.Interfaces;
using Grammar.Czech.Models;

namespace Grammar.Czech.Services
{
    /// <summary>
    /// Provides Czech conjunction lookup: how a conjunction joins clauses, whether a comma precedes it,
    /// and whether it takes the first position of the clause it introduces.
    /// </summary>
    public class CzechConjunctionService : ICzechConjunctionService
    {
        private readonly Dictionary<string, ConjunctionData> _conjunctions;

        /// <summary>
        /// Initializes a new instance of the <see cref="CzechConjunctionService"/> type.
        /// </summary>
        public CzechConjunctionService(IConjunctionDataProvider dataProvider)
        {
            _conjunctions = dataProvider.GetConjunctions();
        }

        /// <summary>
        /// Gets how the supplied conjunction joins clauses.
        /// </summary>
        /// <param name="conjunction">The conjunction text to look up.</param>
        /// <returns>The conjunction type.</returns>
        public ConjunctionType GetType(string conjunction) => Lookup(conjunction).Type;

        /// <summary>
        /// Determines whether a comma is written before the supplied conjunction.
        /// </summary>
        /// <param name="conjunction">The conjunction text to look up.</param>
        /// <returns><see langword="true"/> when a comma precedes the conjunction; otherwise, <see langword="false"/>.</returns>
        public bool RequiresComma(string conjunction) => Lookup(conjunction).RequiresComma;

        /// <summary>
        /// Determines whether the conjunction occupies the first position of the clause it introduces,
        /// which is what the clitic cluster attaches after.
        /// </summary>
        /// <param name="conjunction">The conjunction text to look up.</param>
        /// <returns><see langword="true"/> for a subordinating conjunction; otherwise, <see langword="false"/>.</returns>
        /// <remarks>
        /// A subordinating conjunction belongs to its clause and fills first position, so the cluster follows
        /// it directly: "protože se Petr umyl". A coordinating one stands outside the clause and leaves first
        /// position to whatever comes next: "a Petr se umyl".
        /// </remarks>
        public bool OccupiesFirstPosition(string conjunction) => GetType(conjunction) == ConjunctionType.Subordinating;

        // Conjunctions are a closed class, so an unknown one is a mistake worth reporting rather than a
        // gap to paper over — the punctuation and the clitic position both depend on knowing which it is.
        private ConjunctionData Lookup(string conjunction)
        {
            if (_conjunctions.TryGetValue(conjunction, out var data))
            {
                return data;
            }

            throw new InvalidOperationException(
                $"Neznámá spojka '{conjunction}'. Doplň ji do conjunctions.json. "
                + "Spojky aby a kdyby zatím podporované nejsou — splývají s kondicionálovým pomocným slovesem "
                + "a časují se podle osoby (abych, abys, abychom).");
        }
    }
}
