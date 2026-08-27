using System.Net.Http;
using System.Text.Json;

namespace Grammar.Czech.Analyzer
{
    /// <summary>
    /// Fetches an article's own plain-text extract from Czech Wikipedia by title, via the public
    /// MediaWiki API.
    /// </summary>
    /// <remarks>
    /// Deliberately narrow, on purpose — this is the one source whose licence and API this project has
    /// actually worked out, not a general "fetch any URL" scraper. Wikipedia's own text is CC BY-SA:
    /// republishing it needs attribution and share-alike, but nothing here republishes it — the article
    /// is only ever the transient input <see cref="Tokenizer"/> reads through, the same way any other
    /// text file is, and only the grammatical facts <see cref="Candidates.NounMatcher"/> and its
    /// siblings derive from it (a lemma, a pattern, a gender) ever reach <c>navrhy.json</c>. Facts
    /// derived from a text are not the text itself, the way a word count or a case ending is not the
    /// prose it was read out of. This only holds as long as the fetched extract itself is never cached,
    /// committed or shipped anywhere — <see cref="FetchArticleTextAsync"/> hands it back once and that
    /// is the end of its lifetime here.
    /// </remarks>
    public static class WikipediaReader
    {
        private const string ApiUrl = "https://cs.wikipedia.org/w/api.php";

        /// <summary>
        /// Fetches the plain-text extract of a Czech Wikipedia article by its title.
        /// </summary>
        /// <param name="title">The article's title, as it appears in the Wikipedia URL or search box.</param>
        /// <param name="cancellationToken">A token to cancel the request.</param>
        /// <returns>The article's own plain text, paragraphs separated by newlines.</returns>
        public static async Task<string> FetchArticleTextAsync(string title, CancellationToken cancellationToken = default)
        {
            using var client = new HttpClient();

            // MediaWiki's own API etiquette asks for an identifiable client — a generic default (or a
            // browser string) is what gets automated traffic rate-limited or blocked outright.
            client.DefaultRequestHeaders.UserAgent.ParseAdd("GrammarModular-rozbor/1.0 (Czech grammar lexicon tooling)");

            // redirects=1 follows a moved or alternate-spelled title to the real article instead of
            // coming back empty for it.
            var url = $"{ApiUrl}?action=query&titles={Uri.EscapeDataString(title)}"
                + "&prop=extracts&explaintext=1&redirects=1&format=json";

            using var response = await client.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var extract = ParseExtract(json, title);

            Console.Error.WriteLine($"Zdroj: cs.wikipedia.org, článek „{title}“ (CC BY-SA) — text se do "
                + "slovníku nekopíruje, jen se z něj ověřují gramatické tvary.");

            return extract;
        }

        /// <summary>
        /// Pulls the plain-text extract out of a MediaWiki <c>action=query&amp;prop=extracts</c>
        /// response body.
        /// </summary>
        /// <remarks>
        /// Split out from <see cref="FetchArticleTextAsync"/> so the response-shape handling — a
        /// missing page, or a real page with no text (a disambiguation page has none) — can be tested
        /// against a canned response body, without a live network call in the test suite.
        /// </remarks>
        /// <param name="responseBody">The raw JSON body MediaWiki's API returned.</param>
        /// <param name="title">The article title that was requested, for the error message only.</param>
        /// <returns>The article's own plain text, paragraphs separated by newlines.</returns>
        public static string ParseExtract(string responseBody, string title)
        {
            using var document = JsonDocument.Parse(responseBody);

            var page = document.RootElement.GetProperty("query").GetProperty("pages")
                .EnumerateObject().First().Value;

            if (page.TryGetProperty("missing", out _))
            {
                throw new InvalidOperationException($"Článek „{title}“ na cs.wikipedia.org nenalezen.");
            }

            var extract = page.TryGetProperty("extract", out var extractProperty) ? extractProperty.GetString() : null;

            if (string.IsNullOrWhiteSpace(extract))
            {
                throw new InvalidOperationException(
                    $"Článek „{title}“ na cs.wikipedia.org nemá žádný text (může to být rozcestník).");
            }

            return extract;
        }
    }
}
