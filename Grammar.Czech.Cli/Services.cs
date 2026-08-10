using Grammar.Core.Interfaces;
using Grammar.Czech.Cli.Interaction;
using Grammar.Czech.Cli.Rendering;
using Grammar.Czech.Cli.Sentence;
using Grammar.Czech.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Grammar.Czech.Cli
{
    /// <summary>
    /// Wires the library and the tool's own services together.
    /// </summary>
    public static class Services
    {
        /// <summary>
        /// Builds the service provider for one run.
        /// </summary>
        /// <param name="lexicon">The lexicon named on the command line, or <see langword="null"/>.</param>
        /// <returns>The provider.</returns>
        /// <exception cref="CliException">Thrown when the lexicon cannot be opened.</exception>
        public static IServiceProvider Build(FileInfo? lexicon)
        {
            var services = new ServiceCollection();

            // Argument, pak lexikon.json, a teprve pak si knihovna sáhne na GRAMMAR_CZECH_LEXICON a
            // vedle sebe. Stejné pořadí jako u nástroje `lexikon`: co chceš teď, pak co chce projekt,
            // nakonec co ví tenhle stroj.
            var path = lexicon?.FullName ?? LexiconSettings.DatabasePath();

            services.AddCzechGrammarServices(path);

            services.AddSingleton<LemmaGuess>();
            services.AddSingleton<LemmaLookup>();
            services.AddSingleton<RoleGuess>();
            services.AddSingleton<FormLookup>();
            services.AddSingleton<WordProposals>();
            services.AddSingleton<DraftBuilder>();
            services.AddSingleton<DraftView>();
            services.AddSingleton<SentenceComposer>();
            services.AddSingleton(provider => new SessionLoop(
                provider.GetRequiredService<DraftBuilder>(),
                provider.GetRequiredService<DraftView>(),
                provider.GetRequiredService<SentenceComposer>(),
                provider.GetRequiredService<WordProposals>(),
                Console.In,
                Console.Out));
            services.AddSingleton(provider => new ReviewLoop(
                provider.GetRequiredService<DraftBuilder>(),
                provider.GetRequiredService<DraftView>(),
                provider.GetRequiredService<SentenceComposer>(),
                Console.In,
                Console.Out));

            var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true });

            // Slovník se otevírá až při prvním sáhnutí, takže se to musí vyprovokovat tady — jinak by
            // chybějící soubor spadl uprostřed dialogu místo na začátku.
            try
            {
                _ = provider.GetRequiredService<IValencyProvider<CzechLexicalEntry>>().HasEntry("být");
            }
            catch (FileNotFoundException)
            {
                // Hlášku z knihovny sem nepřebírám: radí volajícímu, jak zavolat AddCzechGrammarServices,
                // což je odpověď pro toho, kdo píše kód, ne pro toho, kdo spustil nástroj.
                throw new CliException($"""
                    Slovník {path ?? "grammar.czech.lexicon.db"} jsem nenašel.

                    V balíčku nástroje se nerozdává, stejně jako v balíčku knihovny. Stáhni si ho:

                      dotnet tool install -g 50PSoftware.GrammarModular.LexiconTool --prerelease
                      lexikon pull

                    …a pak na něj ukaž jedním z těchhle způsobů, v tomhle pořadí:

                      --slovnik <cesta>                    jen pro tohle spuštění
                      {LexiconSettings.FileName}, klíč "database"        pro celý projekt, i pro `lexikon`
                      GRAMMAR_CZECH_LEXICON                pro tenhle stroj

                    Nebo polož grammar.czech.lexicon.db vedle aplikace.
                    """);
            }

            return provider;
        }
    }
}
