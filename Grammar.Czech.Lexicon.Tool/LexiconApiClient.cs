using System.Net.Http.Headers;
using System.Text.Json;

namespace Grammar.Czech.Lexicon.Tool
{
    /// <summary>
    /// Fetches the lexicon from the API, a page at a time.
    /// </summary>
    public sealed class LexiconApiClient : IDisposable
    {
        private readonly HttpClient _client;
        private readonly Uri _endpoint;
        private readonly int _pageSize;

        /// <summary>
        /// Initializes a new instance of the <see cref="LexiconApiClient"/> type.
        /// </summary>
        /// <param name="endpoint">The API endpoint serving the lexicon.</param>
        /// <param name="token">The bearer token, or <see langword="null"/> when the API is open.</param>
        /// <param name="pageSize">How many rows to ask for at a time.</param>
        /// <param name="handler">
        /// The transport to use, or <see langword="null"/> for the default. Supplied by the tests, which
        /// have to reach the paging loop and its guards without opening a socket.
        /// </param>
        public LexiconApiClient(Uri endpoint, string? token, int pageSize, HttpMessageHandler? handler = null)
        {
            _client = handler is null ? new HttpClient() : new HttpClient(handler);
            _endpoint = endpoint;
            _pageSize = pageSize;

            // A dictionary is large and the server has to read it out of MySQL, so the default hundred
            // seconds is short rather than generous here.
            _client.Timeout = TimeSpan.FromMinutes(10);
            _client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("GrammarModular-LexiconTool", "1.0"));

            if (!string.IsNullOrEmpty(token))
            {
                _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
        }

        /// <summary>
        /// Fetches every table of the lexicon, in dependency order.
        /// </summary>
        /// <returns>The pages, as they arrive.</returns>
        public IEnumerable<LexiconPage> Fetch()
        {
            foreach (var table in LexiconSchema.Tables)
            {
                string? after = null;

                do
                {
                    var page = FetchPage(table, after);

                    // A page that names a different table than the one asked for means the request was
                    // routed somewhere unexpected, and importing it would file rows under the wrong table.
                    if (page.Table != table.Name)
                    {
                        throw new InvalidOperationException(
                            $"Server vrátil tabulku '{page.Table}', ptali jsme se na '{table.Name}'.");
                    }

                    // A server that keeps answering with the same next_after would otherwise pull
                    // forever, reinserting the same rows until the primary key rejected them.
                    if (page.NextAfter is not null && page.NextAfter == after)
                    {
                        throw new InvalidOperationException(
                            $"Tabulka '{table.Name}': server vrací pořád stejné next_after ('{after}'). "
                            + "Stránkování na serveru je rozbité.");
                    }

                    after = page.NextAfter;

                    yield return page;
                }
                while (after is not null);
            }
        }

        /// <summary>
        /// Releases the underlying client.
        /// </summary>
        public void Dispose() => _client.Dispose();

        private LexiconPage FetchPage(LexiconTable table, string? after)
        {
            // Appended rather than replaced, so an endpoint that already carries a query — a front
            // controller taking ?route=lexicon, say — keeps it.
            var separator = string.IsNullOrEmpty(_endpoint.Query) ? "?" : "&";
            var query = $"{separator}table={Uri.EscapeDataString(table.Name)}&limit={_pageSize}";

            if (after is not null)
            {
                query += $"&after={Uri.EscapeDataString(after)}";
            }

            var url = new Uri(_endpoint.AbsoluteUri + query);

            using var response = _client.GetAsync(url).GetAwaiter().GetResult();

            if (!response.IsSuccessStatusCode)
            {
                var body = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

                throw new InvalidOperationException(
                    $"{url} odpovědělo {(int)response.StatusCode} {response.ReasonPhrase}. {body}".Trim());
            }

            using var stream = response.Content.ReadAsStreamAsync().GetAwaiter().GetResult();

            return JsonSerializer.Deserialize<LexiconPage>(stream, LexiconPage.SerializerOptions)
                ?? throw new InvalidOperationException($"{url} vrátilo prázdnou odpověď.");
        }
    }
}
