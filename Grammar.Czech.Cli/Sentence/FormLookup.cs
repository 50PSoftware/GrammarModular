using Grammar.Core.Enums;
using Grammar.Core.Interfaces;
using Grammar.Czech.Models;
using Grammar.Czech.Services;

namespace Grammar.Czech.Cli.Sentence
{
    /// <summary>
    /// Answers whether a word the dictionary does not hold is a form of one it does.
    /// </summary>
    /// <remarks>
    /// The tool takes lemmas and builds sentences out of them; it does not read Czech and is not going
    /// to. So this is not a way of entering inflected words — <c>učitele</c> is still not accepted as
    /// input — but a way of telling two very different situations apart. Either the word is a form of
    /// something known, and the answer is <em>you probably meant <c>učitel</c></em>; or it is not, and
    /// the word is genuinely new and worth recording. Before this, both came out as the same silent
    /// guess: <c>učitele</c> became a feminine noun of the <em>růže</em> pattern and the sentence looked
    /// almost right.
    /// <para>
    /// The index is generated rather than stored — every form comes from the same inflection services
    /// that would produce it in a sentence, so it cannot disagree with them. It is built on the first
    /// miss and not before, because a run where every word is a lemma never needs it. At the size the
    /// dictionary is now that is milliseconds; a dictionary two orders of magnitude larger would want
    /// the candidates narrowed by prefix first rather than the whole paradigm set materialized.
    /// </para>
    /// </remarks>
    public sealed class FormLookup
    {
        private readonly IValencyProvider<CzechLexicalEntry> _lexicon;
        private readonly MorphologyEngine _morphology;
        private readonly Lazy<IReadOnlyDictionary<string, IReadOnlyList<string>>> _forms;

        /// <summary>
        /// Initializes a new instance of the <see cref="FormLookup"/> type.
        /// </summary>
        /// <param name="lexicon">The dictionary whose entries are inflected.</param>
        /// <param name="morphology">The inflection engine, which produces the forms.</param>
        public FormLookup(IValencyProvider<CzechLexicalEntry> lexicon, MorphologyEngine morphology)
        {
            _lexicon = lexicon;
            _morphology = morphology;
            _forms = new Lazy<IReadOnlyDictionary<string, IReadOnlyList<string>>>(BuildIndex);
        }

        /// <summary>
        /// Finds the lemmas the supplied word could be a form of.
        /// </summary>
        /// <param name="written">The word as the user typed it.</param>
        /// <returns>The lemmas, empty when the word is a form of nothing the dictionary holds.</returns>
        public IReadOnlyList<string> LemmasBehind(string written) =>
            _forms.Value.GetValueOrDefault(Terms.Plain(written), []);

        private IReadOnlyDictionary<string, IReadOnlyList<string>> BuildIndex()
        {
            var index = new Dictionary<string, List<string>>(StringComparer.Ordinal);

            foreach (var entry in _lexicon.GetEntries())
            {
                foreach (var form in Paradigm(entry))
                {
                    var key = Terms.Plain(form);

                    // Lemma samo do indexu nepatří: na to je LemmaLookup a odpověď „myslel jsi X?" u
                    // slova, které X je, by byla nesmysl.
                    if (Terms.LemmaComparer.Equals(form, entry.Lemma))
                    {
                        continue;
                    }

                    if (!index.TryGetValue(key, out var lemmas))
                    {
                        index[key] = lemmas = [];
                    }

                    if (!lemmas.Contains(entry.Lemma, StringComparer.Ordinal))
                    {
                        lemmas.Add(entry.Lemma);
                    }
                }
            }

            return index.ToDictionary(
                pair => pair.Key,
                pair => (IReadOnlyList<string>)pair.Value,
                StringComparer.Ordinal);
        }

        private IEnumerable<string> Paradigm(CzechLexicalEntry entry)
        {
            foreach (var request in Requests(entry))
            {
                string? form = null;

                try
                {
                    form = _morphology.GetForm(request).Form;
                }
                catch (Exception exception) when (exception is NotSupportedException
                    or InvalidOperationException or ArgumentException or KeyNotFoundException)
                {
                    // Heslo, které se neskloní, prostě do indexu nepřispěje. Spadnout na něm by
                    // znamenalo, že jedno vadné heslo zruší doptávání pro celý slovník.
                }

                if (!string.IsNullOrWhiteSpace(form))
                {
                    yield return form;
                }
            }
        }

        private static IEnumerable<CzechWordRequest> Requests(CzechLexicalEntry entry)
        {
            var word = new CzechWordRequest
            {
                Lemma = entry.Lemma,
                WordCategory = entry.Category,
                Gender = entry.Gender,
                Pattern = entry.Pattern,
                IsAnimate = entry.IsAnimate,
                VerbClass = entry.VerbClass,
                Aspect = entry.Aspect,
                IsPluralOnly = entry.IsPluralOnly,
                IsIndeclinable = entry.IsIndeclinable,
                HasMobileE = entry.HasMobileE,
            };

            if (entry.Category == WordCategory.Verb)
            {
                // Jen oznamovací způsob v obou časech a všech osobách. Rozkaz a příčestí by index
                // zvětšily o tvary, které nikdo jako lemma nezadá — kdo píše 'piš', ví, že píše tvar.
                foreach (var tense in (Tense[])[Tense.Present, Tense.Past])
                {
                    foreach (var person in (Person[])[Person.First, Person.Second, Person.Third])
                    {
                        foreach (var number in (Number[])[Number.Singular, Number.Plural])
                        {
                            yield return word with
                            {
                                Modus = Modus.Indicative,
                                Voice = Voice.Active,
                                Tense = tense,
                                Person = person,
                                Number = number,
                                Gender = entry.Gender ?? Gender.Masculine,
                            };
                        }
                    }
                }

                yield break;
            }

            foreach (var kase in Enum.GetValues<Case>())
            {
                foreach (var number in (Number[])[Number.Singular, Number.Plural])
                {
                    yield return word with { Case = kase, Number = number };
                }
            }
        }
    }
}
