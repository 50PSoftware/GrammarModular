using Grammar.Czech.Lexicon.Tool;
using System.Net;
using System.Text;
using System.Text.Json;

namespace Grammar.Czech.Test
{
    /// <summary>
    /// Checks how the pull client behaves against a server that misbehaves.
    /// </summary>
    /// <remarks>
    /// The paging loop trusts the server to tell it when to stop, so the ways a server can lie are the
    /// ways the pull can go wrong — and both of them fail quietly rather than loudly. A page answering
    /// for the wrong table would file rows under a table they do not belong to; a next_after that never
    /// advances would keep the client asking forever. Neither needs a socket to reproduce.
    /// </remarks>
    [TestClass]
    public sealed class LexiconApiClientTests
    {
        private static readonly Uri Endpoint = new("https://example.invalid/api/lexicon.php");

        /// <summary>
        /// A well-behaved server is read to the end, one table after another.
        /// </summary>
        [TestMethod]
        public void Fetch_WalksEveryTable()
        {
            using var client = new LexiconApiClient(Endpoint, "token", 100, new StubHandler(Page));

            var tables = client.Fetch().Select(page => page.Table).ToList();

            CollectionAssert.AreEqual(
                LexiconSchema.Tables.Select(table => table.Name).ToArray(),
                tables,
                "Klient neprošel tabulky v pořadí schématu.");

            static LexiconPage Page(string table, string? after) => new()
            {
                Table = table,
                Columns = LexiconSchema.Get(table).Columns,
                Rows = []
            };
        }

        /// <summary>
        /// The bearer token is sent.
        /// </summary>
        [TestMethod]
        public void Fetch_SendsTheToken()
        {
            var handler = new StubHandler((table, _) => new LexiconPage
            {
                Table = table,
                Columns = LexiconSchema.Get(table).Columns
            });

            using var client = new LexiconApiClient(Endpoint, "tajne-heslo", 100, handler);

            _ = client.Fetch().First();

            Assert.AreEqual("Bearer tajne-heslo", handler.LastAuthorization);
        }

        /// <summary>
        /// A page answering for a different table than the one requested is refused.
        /// </summary>
        [TestMethod]
        public void Fetch_PageForTheWrongTable_Throws()
        {
            var handler = new StubHandler((_, _) => new LexiconPage
            {
                Table = "valency_slot",
                Columns = LexiconSchema.Get("valency_slot").Columns
            });

            using var client = new LexiconApiClient(Endpoint, "token", 100, handler);

            var exception = Assert.ThrowsException<InvalidOperationException>(() => client.Fetch().ToList());

            StringAssert.Contains(exception.Message, "valency_slot");
        }

        /// <summary>
        /// A server whose next_after never advances is stopped rather than followed forever.
        /// </summary>
        [TestMethod]
        public void Fetch_StuckPaging_Throws()
        {
            var handler = new StubHandler((table, _) => new LexiconPage
            {
                Table = table,
                Columns = LexiconSchema.Get(table).Columns,
                Rows = [],

                // Always the same key, as an endpoint ignoring the after parameter would answer.
                NextAfter = "1"
            });

            using var client = new LexiconApiClient(Endpoint, "token", 100, handler);

            var exception = Assert.ThrowsException<InvalidOperationException>(() => client.Fetch().ToList());

            StringAssert.Contains(exception.Message, "next_after");
        }

        /// <summary>
        /// A failing response is reported with its status and body rather than as an empty pull.
        /// </summary>
        [TestMethod]
        public void Fetch_ErrorResponse_Throws()
        {
            var handler = new StubHandler((_, _) => null!, HttpStatusCode.Unauthorized, """{"error":"Neplatný token."}""");

            using var client = new LexiconApiClient(Endpoint, "spatny", 100, handler);

            var exception = Assert.ThrowsException<InvalidOperationException>(() => client.Fetch().ToList());

            StringAssert.Contains(exception.Message, "401");
            StringAssert.Contains(exception.Message, "Neplatný token.");
        }

        private sealed class StubHandler : HttpMessageHandler
        {
            private readonly Func<string, string?, LexiconPage> _page;
            private readonly HttpStatusCode _status;
            private readonly string? _body;

            public StubHandler(
                Func<string, string?, LexiconPage> page,
                HttpStatusCode status = HttpStatusCode.OK,
                string? body = null)
            {
                _page = page;
                _status = status;
                _body = body;
            }

            public string? LastAuthorization { get; private set; }

            protected override Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request,
                CancellationToken cancellationToken)
            {
                LastAuthorization = request.Headers.Authorization?.ToString();

                var query = System.Web.HttpUtility.ParseQueryString(request.RequestUri!.Query);
                var content = _body ?? JsonSerializer.Serialize(
                    _page(query["table"]!, query["after"]),
                    LexiconPage.SerializerOptions);

                return Task.FromResult(new HttpResponseMessage(_status)
                {
                    Content = new StringContent(content, Encoding.UTF8, "application/json")
                });
            }
        }
    }
}
