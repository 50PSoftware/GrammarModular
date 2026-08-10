using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Grammar.Czech.Lexicon.Tool
{
    /// <summary>
    /// Turns the words <c>gramatika</c> collected into a draft seed file.
    /// </summary>
    /// <remarks>
    /// The client application meets words the dictionary does not hold and writes them down; it cannot
    /// do anything more than that, because the local database is a read-only replica that the next pull
    /// overwrites and because identifiers come from the server. This is the other half: the tool that
    /// does own the dictionary reads the list and produces the thing new content actually enters
    /// through, which is a seed.
    /// <para>
    /// A draft, though, not a seed. Two things are deliberately left for a person. The header says what
    /// the file contains but not what was left out of it and why, which is the one thing only whoever
    /// decided can write and the one thing the existing seeds are careful about. And <c>source</c> is
    /// empty with <c>is_verified</c> at zero: the provenance columns are what the licensing of this
    /// project rests on, and a word that turned up in somebody's session has no provenance until
    /// somebody looks it up.
    /// </para>
    /// </remarks>
    public static class ProposalSeedWriter
    {
        private static readonly JsonSerializerOptions Format = new()
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter() },
        };

        /// <summary>
        /// Reads the proposals and writes the draft seed.
        /// </summary>
        /// <param name="proposalsPath">The file the client collected words in.</param>
        /// <param name="seedDirectory">The directory holding the seed files.</param>
        /// <param name="onlyConfirmed">Whether to skip proposals nobody has confirmed.</param>
        /// <returns>The path of the file written, or null when there was nothing to write.</returns>
        public static string? Write(string proposalsPath, string seedDirectory, bool onlyConfirmed)
        {
            if (!File.Exists(proposalsPath))
            {
                throw new InvalidOperationException(
                    $"Soubor návrhů {proposalsPath} neexistuje. Sbírá do něj `gramatika`, když narazí "
                    + "na slovo, které slovník nezná.");
            }

            var proposals = JsonSerializer.Deserialize<List<Proposal>>(File.ReadAllText(proposalsPath), Format)
                ?? [];

            var taken = proposals
                .Where(proposal => !onlyConfirmed || proposal.IsConfirmed)
                .Where(proposal => proposal.Lemma.Length > 0)
                .ToList();

            if (taken.Count == 0)
            {
                return null;
            }

            var number = NextSeedNumber(seedDirectory);
            var path = Path.Combine(seedDirectory, $"seed.{number:000}.sql");

            File.WriteAllText(path, Compose(taken, number, proposalsPath), new UTF8Encoding(false));

            return path;
        }

        // Číslo se bere z disku, ne z gitu: rozepsaný seed, který ještě není commitnutý, je pořád seed
        // a přepsat ho by znamenalo přijít o něj.
        private static int NextSeedNumber(string directory)
        {
            var highest = Directory.EnumerateFiles(directory, "seed.*.sql")
                .Select(Path.GetFileNameWithoutExtension)
                .Select(name => name!.Split('.'))
                .Where(parts => parts.Length == 2
                    && int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out _))
                .Select(parts => int.Parse(parts[1], CultureInfo.InvariantCulture))
                .DefaultIfEmpty(-1)
                .Max();

            return highest + 1;
        }

        private static string Compose(List<Proposal> proposals, int number, string source)
        {
            var text = new StringBuilder();

            text.Append(CultureInfo.InvariantCulture, $"""
                -- Grammar.Czech — lexicon seed, update {number + 1}. NÁVRH, NE HOTOVÝ SEED.
                --
                -- Vygenerováno z {source}, tedy ze slov, na která `gramatika` narazila a slovník je nevedl.
                -- Co je tady, je návrh: rod, vzor a životnost jsou odhadnuté ze zakončení lemmatu, pokud je
                -- někdo v sezení nepotvrdil, a ani potvrzené neznamená ověřené proti příručce.
                --
                -- NEŽ TOHLE POUŽIJEŠ:
                --
                --   1. Doplň id. Startovní čísla se berou z hlavičky nejvyššího seedu na disku ("Last ids
                --      used"); tady zůstala prázdná, protože je přiděluje server a hádat je nemá smysl.
                --   2. Doplň `source`. Je prázdný záměrně — na provenienci stojí licenční kázeň projektu
                --      a slovo, které se objevilo v něčím sezení, žádnou nemá, dokud ji někdo nedohledá.
                --      Když to je z IJP, napiš 'IJP' a `is_verified` na 1.
                --   3. Dopiš do téhle hlavičky, co jsi vynechal a proč. Je to jediné místo, kde ta
                --      rozhodnutí žijí, a vygenerovat ho nejde.
                --   4. Slovesa dostanou lexém, význam a rámec. Bez rámce heslo existuje, ale větu z něj
                --      nikdo nepostaví — role se pak jen odhadují z pořadí.
                --
                -- Slov: {proposals.Count}.


                """);

            foreach (var proposal in proposals)
            {
                text.AppendLine(Row(proposal));
            }

            return text.ToString();
        }

        private static string Row(Proposal proposal)
        {
            var note = proposal.Note is { Length: > 0 } written
                ? Literal(written)
                : proposal.IsConfirmed ? "'Potvrzeno v sezení gramatiky.'" : "'Odhad ze zakončení, nepotvrzeno.'";

            return $"""
                -- {proposal.Lemma}{(proposal.IsConfirmed ? " (potvrzeno)" : " (odhad)")}
                INSERT INTO lemma_entry (
                    lemma_entry_id, lemma, lemma_key, homonym_index, category, gender, pattern,
                    is_animate, verb_class, aspect, source, is_verified, note)
                VALUES (
                    NULL, {Literal(proposal.Lemma)}, {Literal(proposal.Lemma.ToLowerInvariant())}, 1,
                    {Literal(proposal.Category)}, {Literal(proposal.Gender)}, {Literal(proposal.Pattern)},
                    {Flag(proposal.IsAnimate)}, {Literal(proposal.VerbClass)}, {Literal(proposal.Aspect)},
                    NULL, 0, {note});

                """;
        }

        // Apostrof se v SQL zdvojuje. Lemma ho neobsahuje, ale poznámka od uživatele klidně ano, a
        // rozbitý řetězec by z návrhu udělal soubor, který se nedá spustit.
        private static string Literal(string? value) =>
            value is null ? "NULL" : "'" + value.Replace("'", "''") + "'";

        private static string Flag(bool? value) => value is null ? "NULL" : value.Value ? "1" : "0";

        private sealed class Proposal
        {
            public string Lemma { get; set; } = string.Empty;

            public string? Category { get; set; }

            public string? Gender { get; set; }

            public string? Pattern { get; set; }

            public bool? IsAnimate { get; set; }

            public string? VerbClass { get; set; }

            public string? Aspect { get; set; }

            public bool IsConfirmed { get; set; }

            public string? Note { get; set; }
        }
    }
}
