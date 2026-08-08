using Grammar.Core.Interfaces;
using Grammar.Czech.Interfaces;
using Grammar.Czech.Models;
using Grammar.Czech.Providers;
using Grammar.Czech.Providers.JsonProviders;
using Grammar.Czech.Providers.SqliteProviders;
using Grammar.Czech.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Grammar.Czech
{
    /// <summary>
    /// Registers Czech grammar services in the dependency injection container.
    /// </summary>
    public static class CzechGrammarServiceFactory
    {
        /// <summary>
        /// Registers all Czech grammar providers, services, and composers in dependency injection.
        /// </summary>
        /// <param name="services">The service collection to register Czech grammar services into.</param>
        /// <returns>The same service collection with Czech grammar services registered.</returns>
        public static IServiceCollection AddCzechGrammarServices(this IServiceCollection services)
            => services.AddCzechGrammarServices(lexiconPath: null);

        /// <summary>
        /// Registers all Czech grammar providers, services, and composers in dependency injection, reading
        /// the lexicon from the supplied file.
        /// </summary>
        /// <param name="services">The service collection to register Czech grammar services into.</param>
        /// <param name="lexiconPath">
        /// The path to the lexicon database, or <see langword="null"/> to take it from the directory the
        /// application runs out of.
        /// </param>
        /// <returns>The same service collection with Czech grammar services registered.</returns>
        public static IServiceCollection AddCzechGrammarServices(
            this IServiceCollection services,
            string? lexiconPath)
        {
            // ── Morphological data providers ────────────────────────────────────────
            services.AddSingleton<IVerbDataProvider>(new JsonVerbDataProvider());
            services.AddSingleton<INounDataProvider>(new JsonNounDataProvider());
            services.AddSingleton<IAdjectiveDataProvider>(new JsonAdjectiveDataProvider());
            services.AddSingleton<IPrefixDataProvider>(new JsonPrefixDataProvider());
            services.AddSingleton<ICliticDataProvider>(new JsonCliticsDataProvider());
            services.AddSingleton<IPrepositionDataProvider>(new JsonPrepositionsDataProvider());
            services.AddSingleton<IConjunctionDataProvider>(new JsonConjunctionDataProvider());
            services.AddSingleton<IAdverbDataProvider>(new JsonAdverbDataProvider());
            services.AddSingleton<IParticleDataProvider>(new JsonParticleDataProvider());
            services.AddSingleton<IInterjectionDataProvider>(new JsonInterjectionDataProvider());
            services.AddSingleton<IPronounDataProvider>(new JsonPronounDataProvider());
            services.AddSingleton<INumeralDataProvider>(new JsonNumeralDataProvider());

            // ── Valency & lexical dictionary ─────────────────────────────────────────
            // The one data source that is a database rather than embedded JSON, because it grows.
            services.AddSingleton<IValencyProvider<CzechLexicalEntry>>(
                _ => new SqliteValencyProvider(lexiconPath));

            // ── Phonology ────────────────────────────────────────────────────────────
            services.AddSingleton<IPhonemeRegistry, CzechPhonemeRegistry>();
            services.AddSingleton<ICzechPhonologyService, CzechPhonologyService>();
            services.AddSingleton<IPhonologyService<CzechWordRequest>>(sp =>
                sp.GetRequiredService<ICzechPhonologyService>());

            // ── Word structure ───────────────────────────────────────────────────────
            services.AddSingleton<CzechWordStructureResolver>();
            services.AddSingleton<IWordStructureResolver<CzechWordRequest>>(sp =>
                sp.GetRequiredService<CzechWordStructureResolver>());
            services.AddSingleton<IVerbStructureResolver<CzechWordRequest>>(sp =>
                sp.GetRequiredService<CzechWordStructureResolver>());

            // ── Phonological rule evaluators ─────────────────────────────────────────
            services.AddSingleton<ISofteningRuleEvaluator<CzechWordRequest>, CzechSofteningRuleEvaluator>();
            services.AddSingleton<IEpenthesisRuleEvaluator<CzechWordRequest>, CzechEpenthesisRuleEvaluator>();
            services.AddSingleton<IAlternationRuleEvaluator<CzechWordRequest>, CzechAlternationRuleEvaluator>();
            services.AddSingleton<IJotationRuleEvaluator<CzechWordRequest>, CzechJotationRuleEvaluator>();
            services.AddSingleton<ISyncretismRuleEvaluator<CzechWordRequest>, CzechSyncretismRuleEvaluator>();
            services.AddSingleton<ICzechOrthographyService, CzechOrthographyService>();

            // ── Inflection services ──────────────────────────────────────────────────
            services.AddSingleton<CzechVerbConjugationService>();
            services.AddSingleton<CzechNounDeclensionService>();
            services.AddSingleton<CzechAdjectiveDeclensionService>();
            services.AddSingleton<CzechAdverbService>();
            services.AddSingleton<ICzechAdverbService>(sp =>
                sp.GetRequiredService<CzechAdverbService>());
            services.AddSingleton<CzechPronounService>();
            services.AddSingleton<ICzechPronounService>(sp =>
                sp.GetRequiredService<CzechPronounService>());

            services.AddSingleton<CzechNumeralService>();
            services.AddSingleton<ICzechNumeralService>(sp =>
                sp.GetRequiredService<CzechNumeralService>());
            services.AddSingleton<CzechNumeralComposer>();
            services.AddSingleton<ICzechNumeralOrthographyService, CzechNumeralOrthographyService>();

            // ── Supporting services ──────────────────────────────────────────────────
            services.AddSingleton<CzechPrefixService>();
            services.AddSingleton<ICzechPrefixService>(sp =>
                sp.GetRequiredService<CzechPrefixService>());

            services.AddSingleton<CzechCliticService>();
            services.AddSingleton<ICzechCliticService>(sp =>
                sp.GetRequiredService<CzechCliticService>());

            services.AddSingleton<ICzechPrepositionService, CzechPrepositionService>();
            services.AddSingleton<ICzechConjunctionService, CzechConjunctionService>();
            services.AddSingleton<ICzechParticleService, CzechParticleService>();
            services.AddSingleton<ICzechInterjectionService, CzechInterjectionService>();
            services.AddSingleton<ICzechValencyService, CzechValencyService>();

            services.AddSingleton<CzechLexiconEnricher>();
            services.AddSingleton<CzechAuxiliaryVerbService>();
            services.AddSingleton<CzechVerbPhraseBuilderService>();
            services.AddSingleton<INegationService<CzechWordRequest>, CzechNegationService>();

            // ── Top-level entry points ───────────────────────────────────────────────
            services.AddSingleton<MorphologyEngine>();
            services.AddSingleton<CzechWordFormComposer>();
            services.AddSingleton<IConstructionProvider>(sp =>
                (SqliteValencyProvider)sp.GetRequiredService<IValencyProvider<CzechLexicalEntry>>());
            services.AddSingleton<ICzechConstructionService, CzechConstructionService>();
            services.AddSingleton<CzechFrameSelector>();
            services.AddSingleton<CzechRoleResolver>();
            services.AddSingleton<CzechSentencePlanner>();
            services.AddSingleton<CzechClausePlanner>();
            services.AddSingleton<CzechMicroplanner>();
            services.AddSingleton<CzechWordOrderResolver>();
            services.AddSingleton<CzechSentenceBuilder>();

            // Several services implement this and the last registration silently wins, so it is bound
            // here, to the engine — the only one that accepts a request of any word class.
            services.AddSingleton<IInflectionService<CzechWordRequest>>(sp =>
                sp.GetRequiredService<MorphologyEngine>());
            services.AddSingleton<IVerbInflectionService<CzechWordRequest>>(sp =>
                sp.GetRequiredService<MorphologyEngine>());

            return services;
        }
    }
}
