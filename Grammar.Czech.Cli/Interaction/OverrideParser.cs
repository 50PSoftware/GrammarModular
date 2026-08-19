using Grammar.Core.Enums;
using Grammar.Czech.Cli.Sentence;

namespace Grammar.Czech.Cli.Interaction
{
    /// <summary>
    /// Reads a correction — from a switch or from the review — into <see cref="DraftOverrides"/>.
    /// </summary>
    /// <remarks>
    /// One parser for both, so <c>--pad kniha=dativ</c> and <c>kniha pad=dativ</c> mean the same thing
    /// and accept the same values. That is what makes a session reproducible as a command: the dialog
    /// has no vocabulary of its own.
    /// </remarks>
    public static class OverrideParser
    {
        /// <summary>
        /// Applies a correction written the way a switch writes it: a word, then a value.
        /// </summary>
        /// <param name="overrides">The record to write into.</param>
        /// <param name="property">The property the switch stands for, unaccented.</param>
        /// <param name="assignment">The argument of the switch, as <c>slovo=hodnota</c>.</param>
        /// <exception cref="CliException">Thrown when the argument or the value cannot be read.</exception>
        public static void Assign(DraftOverrides overrides, string property, string assignment)
        {
            var (target, value) = Split(assignment,
                $"Přepínač --{property} se zadává jako slovo=hodnota, třeba --{property} kniha=…");

            Set(overrides, target, property, value);
        }

        /// <summary>
        /// Applies an attachment written as one clause number against another.
        /// </summary>
        /// <param name="overrides">The record to write into.</param>
        /// <param name="assignment">The argument, as <c>klauze=klauze</c>.</param>
        /// <exception cref="CliException">Thrown when the argument cannot be read.</exception>
        public static void AssignAttachment(DraftOverrides overrides, string assignment)
        {
            var (clause, parent) = Split(assignment,
                "Přepínač --pripojit se zadává jako klauze=klauze, třeba --pripojit 3=1.");

            overrides.Attach(Number(clause), Number(parent));
        }

        /// <summary>
        /// Applies a relative-clause switch, which moves a relative clause onto another constituent.
        /// </summary>
        /// <param name="overrides">The record to write into.</param>
        /// <param name="assignment">The argument, as <c>člen=vztažná</c>.</param>
        /// <exception cref="CliException">Thrown when the argument cannot be read.</exception>
        /// <remarks>
        /// The number before the equals sign is a constituent and the one after it a relative clause,
        /// because what moves is the clause and what it moves onto is the constituent — the same shape
        /// as <c>--pripojit</c>, with the thing that receives named first.
        /// </remarks>
        public static void AssignRelative(DraftOverrides overrides, string assignment)
        {
            var (member, relative) = Split(assignment,
                "Přepínač --vztazna se zadává jako člen=vztažná věta, třeba --vztazna 4=2.");

            overrides.Hang(Number(member), Number(relative));
        }

        /// <summary>
        /// Applies a relativizer switch, which states the word a relative clause opens with.
        /// </summary>
        /// <param name="overrides">The record to write into.</param>
        /// <param name="assignment">The argument, as <c>člen=lemma</c>.</param>
        /// <exception cref="CliException">Thrown when the argument cannot be read.</exception>
        public static void AssignRelativizer(DraftOverrides overrides, string assignment)
        {
            var (member, relativizer) = Split(assignment,
                "Přepínač --relativizator se zadává jako člen=slovo, třeba --relativizator 4=jenž.");

            if (string.IsNullOrWhiteSpace(relativizer))
            {
                throw new CliException(
                    "Přepínač --relativizator potřebuje slovo, kterým se vztažná věta uvozuje.");
            }

            overrides.Introduce(Number(member), relativizer.Trim());
        }

        private static int Number(string text) => int.TryParse(text, out var value) && value > 0
            ? value
            : throw new CliException($"'{text}' není číslo klauze. Klauze se číslují od jedné.");

        /// <summary>
        /// Applies a predicate switch, which speaks for the whole sentence unless it names a clause.
        /// </summary>
        /// <param name="overrides">The record to write into.</param>
        /// <param name="property">The property the switch stands for, unaccented.</param>
        /// <param name="argument">The argument: a bare value, or <c>klauze=hodnota</c>.</param>
        /// <exception cref="CliException">Thrown when the argument or the value cannot be read.</exception>
        /// <remarks>
        /// The number before the equals sign is a clause, not a word — unlike on the switches that
        /// address a constituent. Each switch belongs to one of the two, so there is nothing to tell
        /// apart: a predicate has no position of its own that anyone would want to name.
        /// </remarks>
        public static void AssignPredicate(DraftOverrides overrides, string property, string argument)
        {
            var separator = argument.IndexOf('=');

            if (separator < 0)
            {
                PreparePredicate(overrides, overrides.Predicate, property, argument.Trim())();

                return;
            }

            var clause = Number(argument[..separator].Trim());

            PreparePredicate(
                overrides, overrides.PredicateOf(clause), property, argument[(separator + 1)..].Trim())();
        }

        /// <summary>
        /// Applies a correction written the way the review takes it: a target, then one or more pairs.
        /// </summary>
        /// <param name="line">The line the user typed.</param>
        /// <param name="lemmas">The lemmas of the clause, for resolving a target by name.</param>
        /// <param name="overrides">The record to write into.</param>
        /// <param name="predicatePositions">The positions of the predicates, which take corrections of their own.</param>
        /// <exception cref="CliException">Thrown when the line cannot be read.</exception>
        /// <remarks>
        /// Nothing is written until the whole line reads, so a refused correction leaves the state it
        /// found. The review rebuilds the draft after every accepted line and could not undo a half
        /// applied one.
        /// </remarks>
        public static void Apply(
            string line,
            IReadOnlyList<string> lemmas,
            DraftOverrides overrides,
            IReadOnlyList<int>? predicatePositions = null)
        {
            var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            if (parts.Length < 2)
            {
                throw new CliException("Oprava se píše jako: <cíl> <co>=<jak>, třeba 2 role=ADDR. Nápověda je '?'.");
            }

            // Připojení je jediná oprava, která nemluví o slově ani o přísudku, ale o celé klauzi, takže
            // se i adresuje jinak: číslem klauze, ne pořadím slova.
            if (parts[0] is "k" or "K" or "klauze")
            {
                foreach (var pair in parts.Skip(1))
                {
                    var (clause, parent) = Split(pair, $"'{pair}' není dvojice klauze=klauze.");

                    overrides.Attach(Number(clause), Number(parent));
                }

                return;
            }

            // Vztažná věta se adresuje stejně jako klauze — číslem, ne slovem — jenže dvěma čísly
            // různého druhu: vlevo člen, na který se má pověsit, vpravo ona sama.
            if (parts[0] is "v" or "V" or "vztazna" or "vztažná")
            {
                foreach (var pair in parts.Skip(1))
                {
                    var (member, relative) = Split(pair, $"'{pair}' není dvojice člen=vztažná věta.");

                    if (int.TryParse(relative, out _))
                    {
                        overrides.Hang(Number(member), Number(relative));

                        continue;
                    }

                    overrides.Introduce(Number(member), relative.Trim());
                }

                return;
            }

            var target = Resolve(parts[0], lemmas);

            if (target != "p" && predicatePositions?.Any(position => target == position.ToString()) == true)
            {
                throw new CliException(
                    $"'{parts[0]}' je přísudek — ten se opravuje přes 'p', třeba p cas=minuly.");
            }

            // Nejdřív se přečte celý řádek, teprve pak se zapisuje: kdyby druhá dvojice neprošla,
            // s první už by se nedalo nic dělat — návrh se po každé přijaté opravě staví znovu.
            var writes = parts
                .Skip(1)
                .Select(pair =>
                {
                    var (property, value) = Split(pair, $"'{pair}' není dvojice <co>=<jak>.");

                    return Prepare(overrides, target, Terms.Plain(property), value);
                })
                .ToList();

            foreach (var write in writes)
            {
                write();
            }
        }

        // Lemma se překládá na pořadí, protože jen to je jednoznačné: stejné slovo může ve větě stát
        // dvakrát a pak se na ně jménem ukázat nedá.
        private static string Resolve(string target, IReadOnlyList<string> lemmas)
        {
            // 'p' je přísudek celé věty, 'p2' přísudek druhé klauze. Číslo za p je číslo klauze, ne
            // pořadí slova — přísudek se adresuje klauzí, protože každá má právě jeden.
            if (target.Length > 0 && target[0] is 'p' or 'P')
            {
                if (target.Length == 1)
                {
                    return "p";
                }

                if (int.TryParse(target[1..], out var clause) && clause > 0)
                {
                    return "p" + clause;
                }
            }

            if (int.TryParse(target, out var position))
            {
                return position >= 1 && position <= lemmas.Count
                    ? position.ToString()
                    : throw new CliException($"Slovo číslo {position} ve větě není; zadaných je {lemmas.Count}.");
            }

            var matches = lemmas
                .Select((lemma, index) => (lemma, position: index + 1))
                .Where(candidate => string.Equals(candidate.lemma, target, StringComparison.OrdinalIgnoreCase))
                .ToList();

            return matches.Count switch
            {
                1 => matches[0].position.ToString(),
                0 => throw new CliException($"Slovo '{target}' ve větě není."),
                _ => throw new CliException(
                    $"Slovo '{target}' je ve větě víckrát ({string.Join(", ", matches.Select(match => match.position))}) "
                    + "— ukaž na ně pořadovým číslem."),
            };
        }

        private static void Set(DraftOverrides overrides, string target, string property, string value) =>
            Prepare(overrides, target, property, value)();

        // Hodnota se přečte hned a zapíše se až vrácenou akcí, aby šlo celý řádek nejdřív ověřit.
        //
        // 'p'/'p2' je přísudek, ale libovolné lemma začínající na 'p' (pokuta, práce, pes, …) přísudek
        // není — bez ověření, že za 'p' následuje číslo klauze, by se sem propadlo i takové lemma a
        // int.Parse na jeho zbytku (např. "okuta") by shodil proces nezachycenou výjimkou.
        private static Action Prepare(DraftOverrides overrides, string target, string property, string value)
        {
            if (target is "p" or "P")
            {
                return PreparePredicate(overrides, overrides.Predicate, property, value);
            }

            if (target.Length > 1 && target[0] is 'p' or 'P' && int.TryParse(target[1..], out var clause) && clause > 0)
            {
                return PreparePredicate(overrides, overrides.PredicateOf(clause), property, value);
            }

            var word = overrides.For(target);

            return property switch
            {
                "role" => Assigning(Terms.ParseFunctor(value), parsed => word.Functor = parsed),
                "cleneni" => Assigning(Terms.ParseStatus(value), parsed => word.Status = parsed),
                "pad" => Assigning(Terms.ParseCase(value), parsed => word.Case = parsed),
                "rod" => Assigning(Terms.ParseGender(value), parsed => word.Gender = parsed),
                "cislo" => Assigning(Terms.ParseNumber(value), parsed => word.Number = parsed),
                "vzor" => Assigning(value, parsed => word.Pattern = parsed),
                "zivotne" => Assigning(Terms.ParseYesNo(value), parsed => word.IsAnimate = parsed),
                "druh" => Assigning(Terms.ParseWordCategory(value), parsed => word.WordCategory = parsed),
                "stupen" => Assigning(Terms.ParseDegree(value), parsed => word.Degree = parsed),
                "predlozka" => Assigning(value, parsed => word.Preposition = parsed),
                "privlastek" => Assigning(value, word.Modifiers.Add),
                _ => throw new CliException(
                    $"U slova neznám '{property}'. Jde: role, cleneni, pad, rod, cislo, vzor, "
                    + "zivotne, druh, stupen, predlozka, privlastek."),
            };
        }

        private static Action PreparePredicate(
            DraftOverrides overrides, PredicateOverride predicate, string property, string value) => property switch
        {
            "cas" => Assigning(Terms.ParseTense(value), parsed => predicate.Tense = parsed),
            "zpusob" => Assigning(Terms.ParseMood(value), parsed => predicate.Mood = parsed),
            "rod" => Assigning(Terms.ParseVoice(value), parsed => predicate.Voice = parsed),
            "vid" => Assigning(Terms.ParseAspect(value), parsed => predicate.Aspect = parsed),
            "osoba" => Assigning(Terms.ParsePerson(value), parsed => predicate.Person = parsed),
            "cislo" => Assigning(Terms.ParseNumber(value), parsed => predicate.Number = parsed),
            "zapor" => Assigning(Terms.ParseYesNo(value), parsed => predicate.IsNegative = parsed),
            "podmet" => Assigning(ParseSubjectDrop(value), parsed => predicate.DropSubject = parsed),
            "zvratne" => Assigning(ParseReflexive(value), parsed => predicate.ReflexiveType = parsed),
            "ramec" => Assigning(value, parsed => predicate.FrameLabel = parsed),

            // Tyhle tři nejsou o přísudku, ale o celé větě: sloveso vybírá, co je přísudek, a druh
            // věty s koncovým znaménkem patří souvětí jako celku, ne jedné jeho klauzi.
            "sloveso" => Assigning(value, parsed => overrides.PredicateLemma = parsed),
            "typ" => Assigning(Terms.ParseSentenceType(value), parsed => overrides.SentenceType = parsed),
            "konec" => Assigning(value, parsed => overrides.Terminator = parsed),
            _ => throw new CliException(
                $"U přísudku neznám '{property}'. Jde: cas, zpusob, rod, vid, osoba, cislo, "
                + "zapor, podmet, zvratne, ramec, sloveso, typ, konec."),
        };

        // Vypuštění podmětu je vlastnost věty, ne slova: 'čtu' proti 'já čtu' je táž věta jednou
        // nepříznaková a jednou důrazová.
        private static bool ParseSubjectDrop(string value) => Terms.Plain(value) switch
        {
            "vypustit" or "vypusteny" or "ano" => true,
            "ponechat" or "vyjadreny" or "ne" => false,
            _ => throw new CliException(
                $"Z '{value}' nepoznám, co s podmětem. Jde: vypustit, ponechat."),
        };

        private static Action Assigning<TValue>(TValue parsed, Action<TValue> write) => () => write(parsed);

        /// <summary>
        /// Reads the reflexive particle written as a Czech word.
        /// </summary>
        /// <param name="value">The text to read.</param>
        /// <returns>The reflexive type.</returns>
        /// <exception cref="CliException">Thrown when the text names no reflexive type.</exception>
        /// <remarks>
        /// Derived reflexivity is all a switch can state. Whether a verb is a reflexivum tantum or a
        /// deagentive does not follow from the particle, and that is what the dictionary entry is for.
        /// </remarks>
        public static ReflexiveType ParseReflexive(string value) => Terms.Plain(value) switch
        {
            "se" => ReflexiveType.DerivedReflexive_Se,
            "si" => ReflexiveType.DerivedBenefactive_Si,
            "ne" or "zadne" => ReflexiveType.None,
            _ => Enum.TryParse<ReflexiveType>(value, ignoreCase: true, out var parsed) && Enum.IsDefined(parsed)
                ? parsed
                : throw new CliException($"Z '{value}' nepoznám zvratnost. Jde: se, si, ne."),
        };

        private static (string Target, string Value) Split(string text, string complaint)
        {
            var separator = text.IndexOf('=');

            if (separator <= 0 || separator == text.Length - 1)
            {
                throw new CliException(complaint);
            }

            return (text[..separator].Trim(), text[(separator + 1)..].Trim());
        }
    }
}
