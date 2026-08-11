using Grammar.Core.Enums;
using Grammar.Core.Interfaces;
using Grammar.Czech.Interfaces;
using Grammar.Czech.Models;
using Grammar.Czech.Models.Syntax;
using Grammar.Czech.Services;

namespace Grammar.Czech.Cli.Sentence
{
    /// <summary>
    /// Reads a bare list of lemmas as a sentence plan: which word is the predicate, which words hang
    /// off which, and what the lexicon already knows about each of them.
    /// </summary>
    /// <remarks>
    /// What is left here is the part that is about a command line rather than about Czech: turning a
    /// flat list of words into participants, guessing the metadata of a lemma the dictionary does not
    /// hold, and applying what the user overruled. Everything grammatical — which participant fills
    /// which role, which sense of the verb, what case follows — belongs to
    /// <see cref="CzechRoleResolver"/> and <see cref="CzechSentencePlanner"/> and is asked of them, so
    /// that the tool and a library consumer get the same answers from the same code.
    /// </remarks>
    public sealed class DraftBuilder
    {
        private readonly IValencyProvider<CzechLexicalEntry> _lexicon;
        private readonly CzechLexiconEnricher _enricher;
        private readonly ICzechPrepositionService _prepositions;
        private readonly ICzechPronounService _pronouns;
        private readonly ICzechConjunctionService _conjunctions;
        private readonly ICzechNumeralService _numerals;
        private readonly IAdverbDataProvider _adverbs;
        private readonly IParticleDataProvider _particles;
        private readonly IInterjectionDataProvider _interjections;
        private readonly CzechFrameSelector _frames;
        private readonly CzechRoleResolver _roles;
        private readonly CzechSentencePlanner _planner;
        private readonly LemmaGuess _guess;
        private readonly LemmaLookup _lookup;
        private readonly RoleGuess _rolesWithoutFrame;
        private readonly FormLookup _forms;
        private readonly WordProposals _proposals;

        /// <summary>
        /// Initializes a new instance of the <see cref="DraftBuilder"/> type.
        /// </summary>
        /// <param name="lexicon">The dictionary to read entries from.</param>
        /// <param name="enricher">The service that fills a request from a dictionary entry.</param>
        /// <param name="prepositions">The preposition service, for recognizing one in the word list.</param>
        /// <param name="pronouns">The pronoun service, for recognizing one in the word list.</param>
        /// <param name="conjunctions">The conjunction service, for finding where one clause ends.</param>
        /// <param name="numerals">The numeral service, for recognizing one in the word list.</param>
        /// <param name="adverbs">The adverb data, for recognizing one in the word list.</param>
        /// <param name="particles">The particle data, for recognizing one in the word list.</param>
        /// <param name="interjections">The interjection data, for recognizing one in the word list.</param>
        /// <param name="frames">The frame selector, for the sense of the verb.</param>
        /// <param name="roles">The role resolver, which works out the functors.</param>
        /// <param name="planner">The sentence planner, for the values the plan leaves unsaid.</param>
        /// <param name="guess">The fallback for lemmas the dictionary does not hold.</param>
        /// <param name="lookup">The lookup that completes a spelling from the dictionary.</param>
        /// <param name="rolesWithoutFrame">The fallback for verbs the dictionary has no frame for.</param>
        /// <param name="forms">The index that tells an unknown word from a form of a known one.</param>
        /// <param name="proposals">The file unknown words are collected in.</param>
        public DraftBuilder(
            IValencyProvider<CzechLexicalEntry> lexicon,
            CzechLexiconEnricher enricher,
            ICzechPrepositionService prepositions,
            ICzechPronounService pronouns,
            ICzechConjunctionService conjunctions,
            ICzechNumeralService numerals,
            IAdverbDataProvider adverbs,
            IParticleDataProvider particles,
            IInterjectionDataProvider interjections,
            CzechFrameSelector frames,
            CzechRoleResolver roles,
            CzechSentencePlanner planner,
            LemmaGuess guess,
            LemmaLookup lookup,
            RoleGuess rolesWithoutFrame,
            FormLookup forms,
            WordProposals proposals)
        {
            _lexicon = lexicon;
            _enricher = enricher;
            _prepositions = prepositions;
            _pronouns = pronouns;
            _conjunctions = conjunctions;
            _numerals = numerals;
            _adverbs = adverbs;
            _particles = particles;
            _interjections = interjections;
            _frames = frames;
            _roles = roles;
            _planner = planner;
            _guess = guess;
            _lookup = lookup;
            _rolesWithoutFrame = rolesWithoutFrame;
            _forms = forms;
            _proposals = proposals;
        }

        /// <summary>
        /// Reads the lemmas as a clause and returns what the tool made of them.
        /// </summary>
        /// <param name="lemmas">The lemmas, in the order they were entered.</param>
        /// <param name="overrides">What the user has stated so far.</param>
        /// <returns>The draft, complete or with its open questions recorded.</returns>
        /// <exception cref="CliException">Thrown when the lemmas cannot form a clause at all.</exception>
        public SentenceDraft Build(IReadOnlyList<string> lemmas, DraftOverrides overrides)
        {
            if (lemmas.Count == 0)
            {
                throw new CliException("Nezadal jsi žádné lemma. Zkus třeba: gramatika veta student cist kniha");
            }

            foreach (var lemma in lemmas.Where(lemma => lemma.AsSpan().ContainsAny(' ', '	')))
            {
                throw new CliException(
                    $"""
                    '{lemma}' není jedno lemma, ale několik slov v jednom argumentu.

                    Lemmata se zadávají zvlášť, každé jako vlastní argument a v základním tvaru:

                      gramatika veta {lemma.Trim()}

                    Nástroj větu skládá, nerozebírá ji — celá věta v uvozovkách pro něj není vstup.
                    """);
            }

            foreach (var key in overrides.UnmatchedKeys(lemmas))
            {
                throw new CliException(
                    $"Přepínač se odkazuje na '{key}', ale takové slovo ve větě není. Použij lemma, "
                    + $"jak jsi ho zadal, nebo jeho pořadí (1 až {lemmas.Count}).");
            }

            var words = lemmas
                .Select((lemma, index) => Resolve(lemma, index + 1, overrides))
                .ToList();

            var sentence = new SentenceDraft();

            // Spojka je předěl mezi klauzemi. Pořadová čísla slov přitom zůstávají globální přes celý
            // zadaný seznam, aby '--role kniha=PAT' i '4 pad=dativ' ukazovaly pořád na totéž slovo.
            foreach (var segment in Split(words))
            {
                var clause = BuildClause(
                    segment.Conjunction, segment.Words, overrides, sentence.Clauses.Count + 1);

                // Nezadáno visí klauze na té bezprostředně předchozí. Tak to čte i člověk: v 'čte,
                // protože píše a zpívá' patří zpívání dovnitř toho protože, ne vedle celé věty.
                clause.ParentOrdinal = clause.Ordinal == 1
                    ? null
                    : overrides.Attachments.GetValueOrDefault(clause.Ordinal, clause.Ordinal - 1);

                sentence.Clauses.Add(clause);
            }

            foreach (var (clause, parent) in overrides.Attachments)
            {
                if (clause > sentence.Clauses.Count || parent > sentence.Clauses.Count)
                {
                    throw new CliException(
                        $"Připojení {clause}={parent} ukazuje na klauzi, která ve větě není; "
                        + $"klauzí je {sentence.Clauses.Count}.");
                }
            }

            foreach (var singled in overrides.SingledOutClauses.Where(number => number > sentence.Clauses.Count))
            {
                throw new CliException(
                    $"Přísudek klauze {singled} nastavit nejde — tolik klauzí ve větě není, "
                    + $"je jich {sentence.Clauses.Count}.");
            }

            // Výchozí hodnoty se doplňují až nad celým stromem: co spojka řídí, není klauze sama o sobě
            // schopná rozhodnout — klauze souřadná uvnitř 'aby' je v kondicionálu kvůli spojce o dvě
            // úrovně výš. Doplnit to po klauzích znamenalo, že si věta odporovala sama se sebou.
            sentence.Distribute(_planner.Complete(sentence.Assemble()));

            foreach (var clause in sentence.Clauses)
            {
                Absorb(clause);
                ResolveFrame(clause, overrides);
                ApplyGovernment(clause);
                Report(clause);
            }

            return sentence;
        }

        // A conjunction between two verbs is what makes this a complex sentence. It is recognized from
        // the rule data rather than from a switch, the same as a preposition or a pronoun — conjunctions
        // are a closed class and the file that lists them is also the file that says how each one joins.
        private IEnumerable<(string? Conjunction, List<ResolvedWord> Words)> Split(List<ResolvedWord> words)
        {
            string? conjunction = null;
            var current = new List<ResolvedWord>();

            foreach (var word in words)
            {
                if (word.Request.WordCategory == WordCategory.Conjunction)
                {
                    if (current.Count == 0)
                    {
                        throw new CliException(
                            $"Spojka '{word.Lemma}' stojí na začátku, ale spojuje se s tím, co je před ní.");
                    }

                    yield return (conjunction, current);

                    conjunction = word.Lemma;
                    current = [];

                    continue;
                }

                current.Add(word);
            }

            if (current.Count == 0)
            {
                throw new CliException(
                    $"Za spojkou '{conjunction}' už žádná slova nejsou, takže není co připojit.");
            }

            yield return (conjunction, current);
        }

        private ClauseDraft BuildClause(
            string? conjunction, List<ResolvedWord> words, DraftOverrides overrides, int ordinal)
        {
            var draft = new ClauseDraft { Conjunction = conjunction, Ordinal = ordinal };

            // Ještě než se slova rozeberou na přísudek a členy, protože potom už není odkud vzít, jak je
            // uživatel napsal — a věta bude obsahovat slova, která nenapsal.
            var completed = words
                .Where(word => word.CompletedSpelling is not null)
                .Select(word => $"{word.CompletedSpelling} → {word.Lemma}")
                .ToList();

            if (completed.Count > 0)
            {
                draft.Notes.Add($"Doplnil jsem diakritiku podle slovníku: {string.Join(", ", completed)}.");
            }

            ReportUnknown(draft, words);

            // Stupeň na slově, které se nestupňuje, se nikde neprojeví. Mlčet o tom by znamenalo přepínač,
            // který nic neudělá a netvrdí to — a to je horší než chyba. Poznámka, ne výjimka: věta je
            // jinak v pořádku a shodit ji kvůli přepínači navíc by bylo neúměrné.
            foreach (var word in words.Where(word => word.Stated?.Degree is not null
                && word.Request.WordCategory is not (WordCategory.Adjective or WordCategory.Adverb)))
            {
                draft.Notes.Add(
                    $"Stupeň jsem u slova '{word.Lemma}' nechal být — stupňuje se přídavné jméno a "
                    + $"příslovce, a tohle je {Terms.Name(word.Request.WordCategory ?? WordCategory.Noun)}. "
                    + "Když je to jinak, řekni to přepínačem --druh.");
            }

            AttachPredicate(draft, words, overrides);
            AttachConstituents(draft, words, overrides);

            // Až po členech a před rolemi: role rozdává rámec, a když žádný není, nemá knihovna co
            // rozdávat a všechno by zůstalo bez role. Zapsané role tenhle odhad nepřepisuje, takže
            // '--role zahrada=LOC' vyhraje, a knihovna pak dopočítá jen to, co zbylo.
            if (_rolesWithoutFrame.IsNeeded(draft.PredicateLemma))
            {
                var invented = _rolesWithoutFrame.Assign(draft.Constituents);

                if (invented.Count > 0)
                {
                    draft.Notes.Add(
                        $"Sloveso '{draft.PredicateLemma}' slovník nevede, takže nevím, jaké argumenty "
                        + $"váže. Role jsem rozdal podle pořadí: "
                        + string.Join(", ", invented.Select(item => $"{item.Lemma} = {item.Functor}"))
                        + ". Když sedí jinak, oprav to přepínačem --role.");
                }
            }

            // Odtud rozhoduje knihovna: role se dají odvodit z jedné klauze, takže to jde hned. Co je
            // řízené zvenčí — a to jsou výchozí hodnoty — počká, až bude stát celý strom.
            draft.Resolved = _roles.Resolve(ToPlan(draft, overrides));

            return draft;
        }

        // Neznámé slovo je buď tvar něčeho známého, nebo opravdu nové slovo, a to jsou dvě různé
        // situace s dvěma různými odpověďmi. Dokud se nerozlišovaly, splývaly do jednoho tichého
        // odhadu: z 'učitele' se stalo ženské jméno vzoru růže a věta vypadala skoro dobře.
        private void ReportUnknown(ClauseDraft draft, List<ResolvedWord> words)
        {
            foreach (var word in words.Where(word => word.Origin == MetadataOrigin.Guess))
            {
                if (_forms.LemmasBehind(word.Lemma) is { Count: > 0 } behind)
                {
                    draft.Notes.Add(
                        $"'{word.Lemma}' slovník nezná, ale vypadá jako tvar slova "
                        + $"{string.Join(" nebo ", behind.Select(lemma => $"'{lemma}'"))}, které zná. "
                        + "Nástroj skládá větu ze základních tvarů, takže zadej lemma.");

                    continue;
                }

                // Nové slovo se zapíše jednou a mlčky. Do slovníku to nezapisuje a zapsat nemůže —
                // lokální .db je kopie, kterou další pull přepíše — takže je to jen seznam k projití.
                if (_proposals.Add(word.Lemma, word.Request))
                {
                    draft.Notes.Add(
                        $"'{word.Lemma}' slovník nezná a není to ani tvar ničeho, co zná. "
                        + "Zapsal jsem ho mezi návrhy na doplnění slovníku.");
                }
            }
        }

        private ResolvedWord Resolve(string lemma, int position, DraftOverrides overrides)
        {
            var stated = overrides.Find(lemma, position);
            var match = _lookup.Resolve(lemma);

            if (match.Candidates.Count > 0)
            {
                throw new CliException(
                    $"'{lemma}' takhle napsané sedí na víc hesel: {string.Join(", ", match.Candidates)}. "
                    + "Napiš ho s diakritikou, ať je jasné, které z nich myslíš.");
            }

            var word = new CzechWordRequest { Lemma = match.Lemma };

            // Nejdřív to, co řekl uživatel — enricher i odhad píšou jen do prázdného, takže tímhle
            // pořadím zadané vždycky vyhraje nad slovníkem.
            if (stated is not null)
            {
                word.Gender = stated.Gender;
                word.Number = stated.Number;
                word.Case = stated.Case;
                word.Pattern = stated.Pattern;
                word.IsAnimate = stated.IsAnimate;
                word.WordCategory = stated.WordCategory;
                word.Degree = stated.Degree;
            }

            var known = _lexicon.HasEntry(match.Lemma);
            var enriched = _enricher.Enrich(word);

            // Předložky, zájmena ani spojky slovník nevede — jsou to uzavřené třídy a bydlí
            // v pravidlech, ne v hesláři — takže se poznají podle toho, že o nich ta pravidla něco
            // vědí. Bez toho by z 'já' bylo podstatné jméno vzoru hrad.
            if (enriched.WordCategory is null && _pronouns.GetPronounType(lemma) is not null)
            {
                enriched.WordCategory = WordCategory.Pronoun;
            }

            if (enriched.WordCategory is null && _prepositions.GetAllowedCases(lemma).Any())
            {
                enriched.WordCategory = WordCategory.Preposition;
            }

            if (enriched.WordCategory is null && IsConjunction(lemma))
            {
                enriched.WordCategory = WordCategory.Conjunction;
            }

            // Zbylé čtyři uzavřené třídy, ve stejném duchu a záměrně až za předchozími třemi: 'vedle'
            // je předložka i příslovce, 'tak' spojka i příslovce, 'na' předložka i citoslovce. Kdo
            // dosud fungoval, funguje dál.
            //
            // Mezi těmihle čtyřmi je pořadí volba, ne fakt: 49 slov je zároveň v příslovcích i
            // v částicích ('dobře', 'jistě', 'asi', 'prý'). Vyhrává příslovce, protože příslovce může
            // být větný člen, a udělat z 'dobře' částici by ho z věty vyřadilo — kdežto 'asi' jako
            // příslovce se chová stejně jako částice, obojí je neohebné a nic se neskloní. Rozhodnout
            // to lépe by chtělo výčet slov v kódu, což do kódu nepatří; od toho je '--druh'.
            if (enriched.WordCategory is null && _numerals.IsNumeral(lemma))
            {
                enriched.WordCategory = WordCategory.Numerale;
            }

            if (enriched.WordCategory is null && _adverbs.GetAdverbs().ContainsKey(lemma))
            {
                enriched.WordCategory = WordCategory.Adverb;
            }

            if (enriched.WordCategory is null && _particles.GetParticles().ContainsKey(lemma))
            {
                enriched.WordCategory = WordCategory.Particle;
            }

            if (enriched.WordCategory is null && _interjections.GetInterjections().ContainsKey(lemma))
            {
                enriched.WordCategory = WordCategory.Interjection;
            }

            var origin = stated?.StatesMorphology == true
                ? MetadataOrigin.User
                : known ? MetadataOrigin.Lexicon
                : enriched.WordCategory is WordCategory.Pronoun
                    or WordCategory.Preposition
                    or WordCategory.Conjunction
                    or WordCategory.Numerale
                    or WordCategory.Adverb
                    or WordCategory.Particle
                    or WordCategory.Interjection
                    ? MetadataOrigin.Rules
                    : MetadataOrigin.Guess;

            var completed = _guess.Complete(enriched);

            // Číslo neplyne ani ze slovníku, ani ze zakončení — jméno bez čísla je nedořečený request,
            // ne jednotné číslo. Pomnožné slovo si plurál nese v hesle.
            if (completed.WordCategory is not WordCategory.Verb && completed.Number is null)
            {
                completed.Number = completed.IsPluralOnly == true ? Number.Plural : Number.Singular;
            }

            return new ResolvedWord(position, match.Lemma, completed, origin, stated)
            {
                CompletedSpelling = match.Completed ? lemma : null,
            };
        }

        private static void AttachPredicate(ClauseDraft draft, List<ResolvedWord> words, DraftOverrides overrides)
        {
            var verbs = words
                .Where(word => word.Request.WordCategory == WordCategory.Verb)
                .ToList();

            var predicate = overrides.PredicateLemma is { } named
                ? verbs.FirstOrDefault(word => Terms.LemmaComparer.Equals(word.Lemma, named))
                    ?? throw new CliException($"Slovo '{named}' mezi zadanými slovesy není.")
                : verbs.Count switch
                {
                    1 => verbs[0],
                    0 => throw new CliException(
                        "Ve větě není sloveso, takže není z čeho udělat přísudek. Přidej ho v infinitivu."),
                    _ => throw new CliException(
                        $"Sloves je víc ({string.Join(", ", verbs.Select(verb => verb.Lemma))}). "
                        + "Řekni přepínačem --sloveso, které z nich je přísudek; souvětí zatím neskládám."),
                };

            // Přísudek není větný člen v tomhle smyslu, takže role ani členění na něm nedávají smysl.
            // Bez téhle hlášky by 'sloveso cleneni=nove' beze slova zmizelo.
            if (predicate.Stated is { } stated
                && (stated.Functor is not null || stated.Status is not null || stated.Preposition is not null))
            {
                throw new CliException(
                    $"'{predicate.Lemma}' je přísudek — role, členění ani předložka se na něj nevztahují. "
                    + "Čas, způsob a osobu mu nastav přes 'p' (v dialogu) nebo přepínači --cas, --zpusob, --osoba.");
            }

            words.Remove(predicate);

            draft.PredicateLemma = predicate.Lemma;
            draft.PredicatePosition = predicate.Position;
            draft.Predicate = predicate.Request;
            draft.PredicateOrigin = predicate.Origin;
        }

        private void AttachConstituents(ClauseDraft draft, List<ResolvedWord> words, DraftOverrides overrides)
        {
            string? pendingPreposition = null;
            var pendingModifiers = new List<CzechWordRequest>();

            foreach (var word in words)
            {
                if (word.Request.WordCategory == WordCategory.Preposition)
                {
                    if (pendingPreposition is not null)
                    {
                        throw new CliException(
                            $"Předložky '{pendingPreposition}' a '{word.Lemma}' stojí za sebou a nemají "
                            + "u sebe jméno.");
                    }

                    pendingPreposition = word.Lemma;

                    continue;
                }

                // Shodný přívlastek stojí před svým jménem, takže se drží stranou, dokud nepřijde hlava,
                // ke které patří. Rod, číslo a pád si doplní builder shodou.
                if (word.Request.WordCategory is WordCategory.Adjective or WordCategory.Numerale)
                {
                    pendingModifiers.Add(word.Request);

                    continue;
                }

                var constituent = new ConstituentDraft(word.Position, word.Lemma, word.Request, word.Origin)
                {
                    Preposition = word.Stated?.Preposition ?? pendingPreposition,
                    PrepositionCases = [.. _prepositions.GetAllowedCases(
                        word.Stated?.Preposition ?? pendingPreposition ?? string.Empty)],
                    Functor = word.Stated?.Functor,
                    Status = word.Stated?.Status ?? InformationStatus.New,
                    HasStatedStatus = word.Stated?.Status is not null,
                };

                constituent.Modifiers.AddRange(pendingModifiers);
                constituent.Modifiers.AddRange(
                    (word.Stated?.Modifiers ?? []).Select(BuildModifier));

                draft.Constituents.Add(constituent);

                pendingPreposition = null;
                pendingModifiers.Clear();
            }

            if (pendingPreposition is not null)
            {
                throw new CliException(
                    $"Předložka '{pendingPreposition}' nemá u sebe jméno — za předložkou má následovat to, "
                    + "co řídí.");
            }

            if (pendingModifiers.Count > 0)
            {
                throw new CliException(
                    $"Přívlastek '{pendingModifiers[^1].Lemma}' nemá co rozvíjet — shodný přívlastek stojí "
                    + "před svým jménem.");
            }
        }

        // GetReadings vyhazuje na neznámé spojce místo prázdna, takže se to čte jako pokus.
        private bool IsConjunction(string lemma)
        {
            try
            {
                return _conjunctions.GetReadings(lemma).Count > 0;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private CzechWordRequest BuildModifier(string lemma) => _guess.Complete(
            _enricher.Enrich(new CzechWordRequest { Lemma = lemma, WordCategory = WordCategory.Adjective }));

        /// <summary>
        /// Builds the plan the library works from, out of what the user entered.
        /// </summary>
        /// <param name="draft">The draft assembled from the word list.</param>
        /// <param name="overrides">What the user has stated.</param>
        /// <returns>The plan.</returns>
        public static SentencePlan ToPlan(ClauseDraft draft, DraftOverrides overrides)
        {
            // Co bylo řečeno o téhle klauzi, jinak co bylo řečeno o celé větě. Přísudek se adresuje
            // klauzí, protože každá má právě jeden.
            var stated = overrides.PredicateFor(draft.Ordinal);
            var predicate = draft.Predicate;

            predicate.Tense = stated.Tense ?? predicate.Tense;
            predicate.Modus = stated.Mood ?? predicate.Modus;
            predicate.Voice = stated.Voice ?? predicate.Voice;
            predicate.Aspect = stated.Aspect ?? predicate.Aspect;
            predicate.Person = stated.Person ?? predicate.Person;
            predicate.Number = stated.Number ?? predicate.Number;
            predicate.Gender = stated.Gender ?? predicate.Gender;
            predicate.IsNegative = stated.IsNegative ?? predicate.IsNegative;

            if (stated.ReflexiveType is { } reflexive)
            {
                predicate.ReflexiveType = reflexive;
            }

            return new SentencePlan
            {
                Predicate = predicate,
                Participants = [.. draft.Constituents.Select(ToParticipant)],
                FrameLabel = stated.FrameLabel,
                SentenceType = overrides.SentenceType ?? draft.SentenceType,
                Terminator = overrides.Terminator
                    ?? ((overrides.SentenceType ?? draft.SentenceType) == SentenceType.Interrogative ? "?" : "."),

                // Nástroj vypisuje, co dostal; vypustit podmět, který uživatel zadal, by vypadalo, že se
                // slovo ztratilo. Knihovní konzument to má naopak zapnuté, protože staví větu.
                AllowSubjectDrop = stated.DropSubject ?? false,
            };
        }

        private static PlannedParticipant ToParticipant(ConstituentDraft constituent) => new()
        {
            Word = constituent.Word,
            Modifiers = constituent.Modifiers,
            Preposition = constituent.Preposition,
            Functor = constituent.Functor,
            Status = constituent.HasStatedStatus ? constituent.Status : null,
        };

        // Zpátky do návrhu, aby přehled ukazoval přesně to, z čeho se bude stavět — role i členění tak,
        // jak je doplnila knihovna, ne jak by si je nástroj domyslel podruhé.
        private static void Absorb(ClauseDraft draft)
        {
            var plan = draft.ToPlan();

            draft.Predicate = plan.Predicate;

            foreach (var (constituent, participant) in draft.Constituents.Zip(plan.Participants))
            {
                constituent.Functor = participant.Functor;
                constituent.Status = participant.Status ?? constituent.Status;
                constituent.Word = participant.Word;
            }
        }

        private void ResolveFrame(ClauseDraft draft, DraftOverrides overrides)
        {
            var diathesis = draft.Predicate.Voice == Voice.Passive
                ? Diathesis.PassivePeriphrastic
                : Diathesis.Active;

            // Se slovy kolem slovesa, protože mít zájem je jiný rámec než mít — a přehled má ukazovat
            // ten, ze kterého se věta staví.
            var selection = _frames.Select(
                draft.PredicateLemma,
                overrides.PredicateFor(draft.Ordinal).FrameLabel,
                draft.Constituents.Select(constituent => constituent.Lemma),
                diathesis);

            draft.Frame = selection.Frame;
            draft.FrameChoices = selection.Choices;
        }

        // Pád, který doplní builder z rámce, si návrh nese jen pro výpis — request ho nemá a mít nemá,
        // protože smysl rámce je právě v tom, že ho volající neuvádí.
        private static void ApplyGovernment(ClauseDraft draft)
        {
            foreach (var constituent in draft.Constituents)
            {
                var slot = draft.Frame?.Slots.FirstOrDefault(item => item.Functor == constituent.Functor);
                var realization = slot?.Realizations
                    .Where(item => item.Case is not null)
                    .Where(item => constituent.Preposition is null
                        || string.Equals(item.Preposition, constituent.Preposition, StringComparison.OrdinalIgnoreCase))
                    .MinBy(item => item.Preference);

                constituent.FrameCase = realization?.Case;
                constituent.FramePreposition = realization?.Preposition;

                // Bezpředložkový člen, kterému pád neurčuje ani rámec, by skončil v nominativu — to je
                // skoro jistě špatně, tak ať se to řekne nahlas. Neohebné slovo je výjimka: příslovce
                // ani částice pád nemají a rada 'doplň pád' by u nich posílala za něčím, co neexistuje.
                if (constituent.EffectiveCase is null
                    && constituent.Functor is not null
                    && constituent.Word.WordCategory is not (WordCategory.Adverb
                        or WordCategory.Particle
                        or WordCategory.Interjection
                        or WordCategory.Conjunction))
                {
                    draft.Notes.Add(
                        $"U slova '{constituent.Lemma}' neurčuje pád ani rámec, ani předložka — doplň ho "
                        + "přepínačem --pad.");
                }
            }
        }

        private void Report(ClauseDraft draft)
        {
            var guessed = draft.Constituents
                .Where(constituent => constituent.Origin == MetadataOrigin.Guess)
                .Select(constituent => constituent.Lemma)
                .ToList();

            if (draft.PredicateOrigin == MetadataOrigin.Guess)
            {
                guessed.Insert(0, draft.PredicateLemma);
            }

            if (guessed.Count > 0)
            {
                draft.Notes.Add(
                    $"Slovník nezná: {string.Join(", ", guessed)}. Vzor a rod jsou odhadnuté ze zakončení.");
            }

            if (draft.Frame is null && draft.FrameChoices.Count > 1)
            {
                draft.Notes.Add(
                    $"Sloveso '{draft.PredicateLemma}' má víc významů a žádný z nich není výchozí.");
            }
        }

        private sealed record ResolvedWord(
            int Position,
            string Lemma,
            CzechWordRequest Request,
            MetadataOrigin Origin,
            WordOverride? Stated)
        {
            // Jak to napsal uživatel, když se to od slovníku lišilo — jinak null. Věta bude obsahovat
            // slovo, které nenapsal, a to se má říct.
            public string? CompletedSpelling { get; init; }
        }
    }
}
