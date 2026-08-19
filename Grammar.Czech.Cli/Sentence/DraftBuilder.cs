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
        private readonly ICzechAdverbService _adverbService;
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
        /// <param name="adverbService">The adverb service, for telling a relative adverb from any other.</param>
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
            ICzechAdverbService adverbService,
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
            _adverbService = adverbService;
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

            // Spojka je předěl mezi klauzemi, vztažné slovo taky — ale jiného druhu. Klauze za spojkou
            // je sourozenec, klauze za vztažným slovem visí na členu, takže se od ní sestupuje o patro
            // níž a všechno další patří dovnitř. Pořadová čísla slov přitom zůstávají globální přes celý
            // zadaný seznam, aby '--role kniha=PAT' i '4 pad=dativ' ukazovaly pořád na totéž slovo.
            var current = sentence;
            var ordinal = 0;
            var relativeOrdinal = 0;
            ClauseDraft? previous = null;

            foreach (var segment in Split(words))
            {
                // Předělové slovo, jehož doplněná diakritika se má ohlásit. U spojky vždycky; u vztažného
                // slova jen když ho nepřepsal '--relativizator' — přepsané se do věty nedostane a hlásit
                // u něj doplnění by mluvilo o slově, které tam není.
                var divider = segment.Conjunction;

                if (segment.Relativizer is { } relativizer)
                {
                    var inner = new SentenceDraft();
                    var host = Host(sentence, previous, relativizer, overrides, ++relativeOrdinal);
                    var opener = overrides.Relativizers.TryGetValue(host.Position, out var chosen)
                        ? Stated(chosen)
                        : relativizer.Lemma;

                    host.Relative = new RelativeDraft(
                        relativeOrdinal, opener, relativizer.Position, inner);

                    current = inner;
                    divider = string.Equals(opener, relativizer.Lemma, StringComparison.Ordinal)
                        ? relativizer
                        : null;
                }

                var clause = BuildClause(
                    segment.Conjunction?.Lemma, divider, segment.Words, overrides, ++ordinal);
                var stated = overrides.Attachments.TryGetValue(clause.Ordinal, out var parent)
                    ? parent
                    : (int?)null;

                // Prázdný seznam znamená kořen: buď začátek věty, nebo klauze, kterou právě otevřelo
                // vztažné slovo. Ta visí na členu, ne na klauzi, takže přepojit ji nejde — a dřív se to
                // tiše zahodilo, což je přepínač, který nic neudělá a netvrdí to.
                if (current.Clauses.Count == 0)
                {
                    if (stated is { } refused)
                    {
                        throw new CliException(
                            $"Klauze {clause.Ordinal} otevírá vztažnou větu, takže visí na členu, ne na "
                            + $"klauzi {refused}. Na který člen, se řekne přepínačem --vztazna.");
                    }

                    clause.ParentOrdinal = null;
                }
                else
                {
                    // Nezadáno visí klauze na té bezprostředně předchozí ve své větě. Tak to čte i člověk:
                    // v 'čte, protože píše a zpívá' patří zpívání dovnitř toho protože, ne vedle celé věty.
                    // Uvnitř vztažné věty platí totéž — 'a zpívá' za ní zůstává v ní.
                    clause.ParentOrdinal = stated ?? current.Clauses[^1].Ordinal;
                }

                // Přepojení smí sáhnout i přes hranici vztažné věty, a klauze se pak přestěhuje do té věty,
                // ve které bydlí její nový rodič — jinak by v žádném stromu nebyla. Jsou to dvě různé věty
                // a obě dávají smysl: 'a píše dopis' je jednou součást vztažné věty (píše student) a jednou
                // souřadná klauze věty hlavní (píše učitel).
                var target = stated is { } named
                    ? sentence.Holding(named) ?? throw new CliException(
                        $"Připojení {clause.Ordinal}={named} ukazuje na klauzi, která ve větě není.")
                    : current;

                target.Clauses.Add(clause);

                // Další klauze visí na téhle, ať skončila kdekoli — 'bezprostředně předchozí' je pořád ona.
                current = target;
                previous = clause;
            }

            ValidateAttachments(sentence, overrides, ordinal, relativeOrdinal);

            // Pád vztažného zájmena musí být znám dřív, než se rozdají role: knihovna podle něj rezervuje
            // slot uvnitř vztažné věty, takže po rozdání by už neměl co ovlivnit.
            foreach (var (host, relative) in sentence.AllRelatives)
            {
                SettleRelativeCase(relative, overrides);
                CheckAgreement(host, relative);
            }

            // Role i výchozí hodnoty se doplňují až nad celým stromem, a to ze dvou různých důvodů.
            // Role proto, že vztažné zájmeno drží slot ve své klauzi i ve všem, co s ní souřadí, a to
            // klauze sama o sobě nevidí. Výchozí hodnoty proto, že co spojka řídí, není klauze sama
            // o sobě schopná rozhodnout — klauze souřadná uvnitř 'aby' je v kondicionálu kvůli spojce
            // o dvě úrovně výš. Doplnit obojí po klauzích znamenalo, že si věta odporovala sama se sebou.
            var resolved = _roles.Resolve(sentence.Assemble());

            sentence.TakeResolved(resolved);

            // Až teď: přivlastňovací zájmeno pojmenovává vlastněný člen funktorem a ten se dozvíme
            // odsud. Strom se proto skládá podruhé, aby ta jedna pozdní hodnota šla toutéž cestou
            // jako všechno ostatní, místo aby se dodatečně vpisovala do hotového plánu.
            foreach (var (_, relative) in sentence.AllRelatives)
            {
                SettlePossessed(relative);
            }

            sentence.Distribute(_planner.Complete(sentence.Reassemble()));

            foreach (var clause in sentence.AllClauses)
            {
                Absorb(clause);
                ResolveFrame(clause, overrides);
                ApplyGovernment(clause);
                Report(clause);
            }

            return sentence;
        }

        // A conjunction between two verbs is what makes this a complex sentence, and a relativizer is what
        // makes one of them a relative clause. Both are recognized from the rule data rather than from a
        // switch, the same as a preposition or a pronoun — they are closed classes and the files that list
        // them are also the files that say how each one joins.
        private IEnumerable<Segment> Split(List<ResolvedWord> words)
        {
            ResolvedWord? conjunction = null;
            ResolvedWord? relativizer = null;
            var current = new List<ResolvedWord>();

            for (var index = 0; index < words.Count; index++)
            {
                var word = words[index];
                var divider = word.Request.WordCategory == WordCategory.Conjunction
                    || IsRelativizer(word, current, words.Skip(index + 1));

                if (!divider)
                {
                    current.Add(word);

                    continue;
                }

                if (current.Count == 0)
                {
                    throw new CliException(
                        word.Request.WordCategory == WordCategory.Conjunction
                            ? $"Spojka '{word.Lemma}' stojí na začátku, ale spojuje se s tím, co je před ní."
                            : $"Vztažné slovo '{word.Lemma}' stojí na začátku, ale rozvíjí jméno před sebou.");
                }

                yield return new Segment(conjunction, relativizer, current);

                if (word.Request.WordCategory == WordCategory.Conjunction)
                {
                    conjunction = word;
                    relativizer = null;
                }
                else
                {
                    conjunction = null;
                    relativizer = word;
                }

                current = [];
            }

            if (current.Count == 0)
            {
                throw new CliException(relativizer is { } dangling
                    ? $"Za vztažným slovem '{dangling.Lemma}' už žádná slova nejsou, takže vztažná věta "
                        + "nemá z čeho vzniknout."
                    : $"Za spojkou '{conjunction?.Lemma}' už žádná slova nejsou, takže není co připojit.");
            }

            if (relativizer is { } opener && current.All(word => word.Request.WordCategory != WordCategory.Verb))
            {
                throw new CliException(
                    $"Vztažná věta uvozená slovem '{opener.Lemma}' nemá sloveso. Přidej ho v infinitivu — "
                    + "vztažná věta je věta, ne přívlastek.");
            }

            yield return new Segment(conjunction, relativizer, current);
        }

        // Vztažné slovo se pozná tím, že vztažné čtení vůbec má, a tázací od vztažného rozliší pozice:
        // vztažné 'který' stojí za jménem, které rozvíjí, tázací před ním. 'Který student čte knihu?'
        // proto předěl netvoří — před ním nic není. Vztažná příslovce ('kde', 'kdy') zájmena nejsou
        // a mají vlastní příznak v datech, tak se hledají zvlášť.
        //
        // Sloveso za ním se hledá jen u slov, kterým je vztažné čtení až to druhé: 'proč' a 'odkud' jsou
        // stejně dobře příslovce jako vztažná, takže 'student čte knihu proč' je otázka po důvodu a ne
        // useknutá vztažná věta. U 'který' a 'jenž' je vztažné čtení to primární — jiné užití nemají —
        // takže chybějící sloveso je chyba a ohlásí se jako chyba, ne že se slovo tiše přeznačí.
        private bool IsRelativizer(
            ResolvedWord word, List<ResolvedWord> current, IEnumerable<ResolvedWord> rest)
        {
            if (current.Count == 0 || word.Stated?.WordCategory is not null)
            {
                return false;
            }

            var readings = _pronouns.GetReadings(word.Lemma);
            var relative = word.Request.WordCategory == WordCategory.Adverb
                ? _adverbService.IsRelative(word.Lemma)
                : word.Request.WordCategory == WordCategory.Pronoun
                    && readings.Any(reading => reading.Type == PronounType.Relative);

            if (!relative)
            {
                return false;
            }

            return readings.FirstOrDefault()?.Type == PronounType.Relative
                || rest.Any(following => following.Request.WordCategory == WordCategory.Verb);
        }

        // Nezadáno visí vztažná věta na posledním členu klauze před ní — tak ji čte i člověk, protože
        // vztažné zájmeno se váže k nejbližšímu předcházejícímu jménu. Výjimka se řekne '--vztazna',
        // a ta smí ukázat na kterýkoli člen, který už ve větě stojí: rozvíjet jde jen to, co bylo
        // řečeno dřív, jinak by zájmeno odkazovalo dopředu.
        private static ConstituentDraft Host(
            SentenceDraft sentence,
            ClauseDraft? previous,
            ResolvedWord relativizer,
            DraftOverrides overrides,
            int ordinal)
        {
            if (overrides.Relatives.FirstOrDefault(entry => entry.Value == ordinal).Key is > 0 and var stated)
            {
                return sentence.AllClauses
                    .SelectMany(clause => clause.Constituents)
                    .FirstOrDefault(constituent => constituent.Position == stated)
                    ?? throw new CliException(
                        $"Vztažná věta {ordinal} se má pověsit na člen {stated}, ale takový člen před ní "
                        + "není. Číslo je pořadí slova, jak jsi ho zadal.");
            }

            if (previous is null || previous.Constituents.Count == 0)
            {
                throw new CliException(
                    $"Vztažné slovo '{relativizer.Lemma}' nemá co rozvíjet — před ním musí stát jméno, "
                    + "ke kterému se vztahuje.");
            }

            return previous.Constituents[^1];
        }

        private static void ValidateAttachments(
            SentenceDraft sentence, DraftOverrides overrides, int clauses, int relatives)
        {
            foreach (var (member, relative) in overrides.Relatives.Where(entry => entry.Value > relatives))
            {
                throw new CliException(relatives == 0
                    ? $"Přepínač --vztazna {member}={relative} mluví o vztažné větě, ale ve větě žádná "
                        + "není. Vztažnou větu uvozuje 'který', 'jenž' nebo vztažné příslovce."
                    : $"Vztažná věta {relative} ve větě není; je jich {relatives}.");
            }

            foreach (var member in overrides.Relativizers.Keys
                .Where(member => sentence.AllRelatives.All(pair => pair.Host.Position != member)))
            {
                throw new CliException(
                    $"Přepínač --relativizator {member} mluví o členu, na kterém žádná vztažná věta "
                    + "nevisí.");
            }

            foreach (var (clause, parent) in overrides.Attachments)
            {
                if (clause > clauses || parent > clauses)
                {
                    throw new CliException(
                        $"Připojení {clause}={parent} ukazuje na klauzi, která ve větě není; "
                        + $"klauzí je {clauses}.");
                }
            }

            foreach (var singled in overrides.SingledOutClauses.Where(number => number > clauses))
            {
                throw new CliException(
                    $"Přísudek klauze {singled} nastavit nejde — tolik klauzí ve větě není, "
                    + $"je jich {clauses}.");
            }
        }

        private ClauseDraft BuildClause(
            string? conjunction,
            ResolvedWord? divider,
            List<ResolvedWord> words,
            DraftOverrides overrides,
            int ordinal)
        {
            var draft = new ClauseDraft { Conjunction = conjunction, Ordinal = ordinal };

            // Ještě než se slova rozeberou na přísudek a členy, protože potom už není odkud vzít, jak je
            // uživatel napsal — a věta bude obsahovat slova, která nenapsal. Předělové slovo se počítá
            // s nimi, i když v žádné klauzi jako člen nestojí: 'protoze' se ve větě vysloví stejně jako
            // 'cist', takže se o něm hlásí totéž.
            var completed = (divider is null ? words : words.Prepend(divider))
                .Where(word => word.CompletedSpelling is not null)
                .Select(word => $"{word.CompletedSpelling} → {word.Lemma}")
                .ToList();

            if (completed.Count > 0)
            {
                draft.Notes.Add(
                    $"Doplnil jsem diakritiku podle slovníku a pravidel: {string.Join(", ", completed)}.");
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
                var voice = overrides.PredicateFor(ordinal).Voice ?? draft.Predicate.Voice ?? Voice.Active;
                var invented = _rolesWithoutFrame.Assign(draft.Constituents, voice);

                if (invented.Count > 0)
                {
                    draft.Notes.Add(
                        $"Sloveso '{draft.PredicateLemma}' slovník nevede, takže nevím, jaké argumenty "
                        + $"váže. Role jsem rozdal podle pořadí: "
                        + string.Join(", ", invented.Select(item => $"{item.Lemma} = {item.Functor}"))
                        + ". Když sedí jinak, oprav to přepínačem --role.");
                }
            }

            // Jen sesbírané, nic rozhodnutého: role i výchozí hodnoty čekají, až bude stát celý strom.
            draft.Stated = ToPlan(draft, overrides);

            return draft;
        }

        // Slovník má poslední slovo — u příslovce je to jediný zdroj, protože 'dnes' je kdy a odvodit
        // se to nedá. Kde mlčí, rozhodne třída: částice svým typem, citoslovce tím, že je citoslovce.
        private FgdFunctor? Inherent(string lemma, CzechWordRequest word)
        {
            if (_lexicon.GetEntry(lemma, word.WordCategory ?? WordCategory.Noun)?.InherentFunctor is { } stated)
            {
                return stated;
            }

            return word.WordCategory switch
            {
                WordCategory.Particle when _particles.GetParticles().TryGetValue(lemma, out var particle)
                    => ClassFunctors.Of(particle.Type),
                WordCategory.Interjection => FgdFunctor.PARTL,
                _ => null,
            };
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

            // --zivotne na slovo, které slovník nezná, opravuje jen jeho tvar — u role se pak nesmí
            // brát jako fakt, jinak jedna oprava skloňování otočí role všem ostatním členům věty.
            if (stated?.IsAnimate is not null && !known)
            {
                word.IsAnimateAssumed = true;
            }

            var enriched = _enricher.Enrich(word);

            // Předložky, zájmena ani spojky slovník nevede — jsou to uzavřené třídy a bydlí
            // v pravidlech, ne v hesláři — takže se poznají podle toho, že o nich ta pravidla něco
            // vědí. Bez toho by z 'já' bylo podstatné jméno vzoru hrad.
            //
            // Ptá se se na 'match.Lemma', ne na to, co uživatel napsal: pravidla jsou klíčovaná
            // přesně, takže 'ktery' by v nich nikdo nenašel a stalo by se z něj odhadnuté jméno.
            if (enriched.WordCategory is null && _pronouns.GetPronounType(match.Lemma) is not null)
            {
                enriched.WordCategory = WordCategory.Pronoun;
            }

            if (enriched.WordCategory is null && _prepositions.GetAllowedCases(match.Lemma).Any())
            {
                enriched.WordCategory = WordCategory.Preposition;
            }

            if (enriched.WordCategory is null && IsConjunction(match.Lemma))
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
            if (enriched.WordCategory is null && _numerals.IsNumeral(match.Lemma))
            {
                enriched.WordCategory = WordCategory.Numerale;
            }

            if (enriched.WordCategory is null && _adverbs.GetAdverbs().ContainsKey(match.Lemma))
            {
                enriched.WordCategory = WordCategory.Adverb;
            }

            if (enriched.WordCategory is null && _particles.GetParticles().ContainsKey(match.Lemma))
            {
                enriched.WordCategory = WordCategory.Particle;
            }

            if (enriched.WordCategory is null && _interjections.GetInterjections().ContainsKey(match.Lemma))
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

            // Funktor, který si slovo nese samo, a jen tam, kde ho uživatel neurčil. U příslovce
            // a částice ho ví jedině slovník — 'dnes' je kdy, 'asi' je modalita — kdežto citoslovce
            // ho má ze své třídy: PARTL je totéž jako „stojí mimo stavbu věty“ a jiné citoslovce není.
            return new ResolvedWord(position, match.Lemma, completed, origin, stated)
            {
                CompletedSpelling = match.Completed ? lemma : null,
                InherentFunctor = stated?.Functor is not null ? null : Inherent(match.Lemma, completed),
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
                    Functor = word.Stated?.Functor ?? word.InherentFunctor,
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

        // Vztažné příslovce pád nemá — je neohebné a argumentem své klauze není — takže se u něj nic
        // neodvozuje. U zájmena rozhoduje zadané, a kde nic zadané není, rámec slovesa vztažné věty.
        private void SettleRelativeCase(RelativeDraft relative, DraftOverrides overrides)
        {
            // Přivlastňovací zájmeno není argument své věty, ale přívlastek jednoho z nich, takže si
            // žádný pád nedrží — ten má vlastněné jméno a bere si ho ze své vlastní role.
            if (_adverbService.IsRelative(relative.Relativizer)
                || _pronouns.IsPossessiveRelative(relative.Relativizer))
            {
                return;
            }

            if (overrides.Find(relative.Relativizer, relative.Position)?.Case is { } stated)
            {
                relative.Case = stated;

                return;
            }

            relative.Case = DerivedCase(relative.Clause.Main) ?? Case.Nominative;
            relative.CaseIsDerived = true;
        }

        // Vztažné zájmeno se shoduje s řídícím jménem v rodě, čísle a životnosti, a ne každé to umí:
        // 'co' a 'kdo' rod nemají a jejich paradigma má jedinou řadu, takže po jméně jiného rodu z nich
        // tvar nevyjde. Ptát se na to tady, dokud se ještě dá odpovědět jménem nebo jiným zájmenem, je
        // lepší než nechat stavbu věty spadnout na chybějícím tvaru a mluvit přitom o pádu.
        private void CheckAgreement(ConstituentDraft host, RelativeDraft relative)
        {
            if (_adverbService.IsRelative(relative.Relativizer))
            {
                return;
            }

            var reading = _pronouns.GetReadings(relative.Relativizer)
                .FirstOrDefault(reading => reading.Type == PronounType.Relative);

            // 'kdo' a 'čí' se neváží na jméno, ale na ukazovací zájmeno: 'ten, kdo přišel', 'ten, čí
            // chleba jíš'. Na jméně by prošly, protože tvar pro mužský životný rod mají — a vyšla by
            // věta, kterou NESČ mezi vztažné věty se jmennou hlavou nepočítá.
            if (reading?.RequiresPronominalHead == true
                && host.Word.WordCategory != WordCategory.Pronoun)
            {
                throw new CliException(
                    $"'{relative.Relativizer}' se neváže na jméno '{host.Lemma}' (č. {host.Position}), "
                    + $"ale na ukazovací zájmeno: 'ten {relative.Relativizer} …'. "
                    + "Na jméno patří 'který' nebo 'jenž'.");
            }

            // U přivlastňovacího zájmena s nominální hlavou neurčuje řídící jméno tvar, ale samo slovo:
            // mužský a střední rod v jednotném čísle jehož, ženský jejíž, množné číslo jejichž. Všechna
            // tři jsou platná slova, takže špatná volba by prošla až na povrch jako bezvadná věta o něčem
            // jiném. 'čí' vyjadřuje jen posesora, je jedno pro všechny rody a vybírat není z čeho.
            if (_pronouns.IsPossessiveRelative(relative.Relativizer))
            {
                var expected = host.Word.Number == Number.Plural
                    ? "jejichž"
                    : host.Word.Gender == Gender.Feminine ? "jejíž" : "jehož";

                if (reading?.RequiresPronominalHead != true
                    && !string.Equals(relative.Relativizer, expected, StringComparison.Ordinal))
                {
                    throw new CliException(
                        $"K '{host.Lemma}' (č. {host.Position}) patří '{expected}', ne "
                        + $"'{relative.Relativizer}' — které ze tří to je, rozhoduje rod a číslo "
                        + "řídícího jména.");
                }

                return;
            }

            if (relative.Case is not { } kase)
            {
                return;
            }

            // Nesklonné vztažné 'co' nese svou roli odkazovacím zájmenem uvnitř vztažné věty, a to nástroj
            // zadat neumí. V nominativu je nulové, takže tam se nic neztratí.
            if (reading?.InflectionClass == InflectionClass.Indeclinable)
            {
                if (kase != Case.Nominative)
                {
                    throw new CliException(
                        $"Vztažné '{relative.Relativizer}' se neskloňuje — svou roli nese odkazovací "
                        + "zájmeno uvnitř té věty ('člověk, co jsem ho viděl'), a to nástroj zadat neumí. "
                        + $"Pro {Terms.Name(kase)} použij 'který' nebo 'jenž'.");
                }

                return;
            }

            var form = _pronouns.TryGetForm(
                relative.Relativizer, kase, host.Word.Gender, host.Word.Number, host.Word.IsAnimate, null);

            if (form is null)
            {
                throw new CliException(
                    $"Vztažné zájmeno '{relative.Relativizer}' se se jménem '{host.Lemma}' neshodne — "
                    + $"pro {Terms.Name(kase)} a jeho rod pro něj tvar není. "
                    + "Zkus 'který' nebo 'jenž', které se skloňují podle řídícího jména.");
            }
        }

        // Lemma zadané v přepínači projde týmž skládáním diakritiky jako lemma ve vstupu — '--relativizator
        // 4=jenz' píše člověk na téže klávesnici jako 'jenz' mezi slovy a je to totéž slovo.
        private string Stated(string lemma)
        {
            var match = _lookup.Resolve(lemma);

            return match.Candidates.Count == 0
                ? match.Lemma
                : throw new CliException(
                    $"'{lemma}' takhle napsané sedí na víc hesel: {string.Join(", ", match.Candidates)}. "
                    + "Napiš ho s diakritikou, ať je jasné, které z nich myslíš.");
        }

        // Přivlastňuje jméno hned za sebou — 'žena, jejíž dům vidím' — což je ve vstupu první člen věty,
        // kterou to zájmeno otevřelo. Plán ho pojmenovává funktorem, ne pořadím, takže se to dá říct až
        // po rozdělení rolí.
        private void SettlePossessed(RelativeDraft relative)
        {
            if (!_pronouns.IsPossessiveRelative(relative.Relativizer))
            {
                return;
            }

            var main = relative.Clause.Main;
            var owner = main.Constituents.FirstOrDefault()
                ?? throw new CliException(
                    $"Vztažné zájmeno '{relative.Relativizer}' přivlastňuje, ale za ním žádné jméno "
                    + "nestojí. Napiš, čí co to je: 'žena jejíž dům vidět'.");

            // Z rozebraného plánu, ne z členu: role se do členů vrací až v Absorb, a to je o krok dál.
            relative.Possessed = main.Resolved?.Participants.FirstOrDefault()?.Functor
                ?? throw new CliException(
                    $"U slova '{owner.Lemma}' (č. {owner.Position}) není jasná role, a bez ní nejde "
                    + $"říct, co '{relative.Relativizer}' přivlastňuje. Doplň ji přepínačem --role.");
        }

        // Zájmeno si bere první slot, který mu rámec nechá volný — zrcadlí to výběr slotů
        // v CzechRoleResolver, jen opačným směrem: ten jde od pádu k funktoru, tady se hledá pád
        // k funktoru. Zadaná role člena slot obsazuje, a první nebo druhá osoba na slovese taky, protože
        // to je shoda s nevysloveným podmětem: 'dopis, který čtu' je patiens, ne konatel.
        private Case? DerivedCase(ClauseDraft inner)
        {
            var diathesis = inner.Predicate.Voice == Voice.Passive
                ? Diathesis.PassivePeriphrastic
                : Diathesis.Active;

            var frame = _frames.Select(
                inner.PredicateLemma,
                null,
                inner.Constituents.Select(constituent => constituent.Lemma),
                diathesis).Frame;

            if (frame is null)
            {
                return null;
            }

            var speaker = inner.Predicate.Person is Person.First or Person.Second;
            var taken = inner.Constituents
                .Select(constituent => constituent.Functor)
                .Where(functor => functor is not null)
                .ToHashSet();

            return frame.Slots
                .Where(slot => !speaker || slot.Functor != FgdFunctor.ACT)
                .Where(slot => !taken.Contains(slot.Functor))
                .OrderBy(slot => slot.Functor switch
                {
                    FgdFunctor.ACT => 0,
                    FgdFunctor.ADDR => 1,
                    _ => 2,
                })
                .ThenBy(slot => slot.CanonicalOrder)
                .SelectMany(slot => slot.Realizations
                    .Where(realization => realization.Case is not null && realization.Preposition is null)
                    .OrderBy(realization => realization.Preference))
                .Select(realization => realization.Case)
                .FirstOrDefault();
        }

        // Co odděluje jeden úsek slov od dalšího: spojka dělá sourozence, vztažné slovo vztažnou větu.
        // Nikdy obojí naráz — proto dvě pole a ne jedno se značkou. Celé slovo, ne jen lemma, protože
        // i předělové slovo mohlo dostat doplněnou diakritiku a to se má říct.
        private sealed record Segment(
            ResolvedWord? Conjunction,
            ResolvedWord? Relativizer,
            List<ResolvedWord> Words);

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

            // Funktor, který si slovo nese samo. Nese se sem, protože roli dostane člen až o krok dál
            // a heslo už tam po ruce není.
            public FgdFunctor? InherentFunctor { get; init; }
        }
    }
}
