using Grammar.Core.Interfaces;
using Grammar.Czech.Interfaces;
using Grammar.Czech.Models;
using Grammar.Czech.Providers;
using Grammar.Czech.Providers.JsonProviders;
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
        {
            // ── Morphological data providers ────────────────────────────────────────
            services.AddSingleton<IVerbDataProvider>(new JsonVerbDataProvider());
            services.AddSingleton<INounDataProvider>(new JsonNounDataProvider());
            services.AddSingleton<IAdjectiveDataProvider>(new JsonAdjectiveDataProvider());
            services.AddSingleton<IPrefixDataProvider>(new JsonPrefixDataProvider());
            services.AddSingleton<IParticleDataProvider>(new JsonParticlesDataProvider());
            services.AddSingleton<IPrepositionDataProvider>(new JsonPrepositionsDataProvider());
            services.AddSingleton<IConjunctionDataProvider>(new JsonConjunctionDataProvider());
            services.AddSingleton<IPronounDataProvider>(new JsonPronounDataProvider());

            // ── Valency & lexical dictionary ─────────────────────────────────────────
            services.AddSingleton<IValencyProvider<CzechLexicalEntry>, JsonValencyProvider>();

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
            services.AddSingleton<IJotationRuleEvaluator<CzechWordRequest>, CzechJotationRuleEvaluator>();
            services.AddSingleton<ISyncretismRuleEvaluator<CzechWordRequest>, CzechSyncretismRuleEvaluator>();
            services.AddSingleton<ICzechOrtographyService, CzechOrtographyService>();

            // ── Inflection services ──────────────────────────────────────────────────
            services.AddSingleton<CzechVerbConjugationService>();
            services.AddSingleton<CzechNounDeclensionService>();
            services.AddSingleton<CzechAdjectiveDeclensionService>();
            services.AddSingleton<CzechPronounService>();
            services.AddSingleton<ICzechPronounService>(sp =>
                sp.GetRequiredService<CzechPronounService>());
            services.AddSingleton<IInflectionService<CzechWordRequest>>(sp =>
                sp.GetRequiredService<CzechPronounService>());

            // ── Supporting services ──────────────────────────────────────────────────
            services.AddSingleton<CzechPrefixService>();
            services.AddSingleton<ICzechPrefixService>(sp =>
                sp.GetRequiredService<CzechPrefixService>());

            services.AddSingleton<CzechParticleService>();
            services.AddSingleton<ICzechParticleService>(sp =>
                sp.GetRequiredService<CzechParticleService>());

            services.AddSingleton<ICzechPrepositionService, CzechPrepositionService>();
            services.AddSingleton<ICzechConjunctionService, CzechConjunctionService>();
            services.AddSingleton<ICzechValencyService, CzechValencyService>();

            services.AddSingleton<CzechAuxiliaryVerbService>();
            services.AddSingleton<CzechVerbPhraseBuilderService>();
            services.AddSingleton<INegationService<CzechWordRequest>, CzechNegationService>();

            // ── Top-level entry points ───────────────────────────────────────────────
            services.AddSingleton<MorphologyEngine>();
            services.AddSingleton<CzechWordFormComposer>();
            services.AddSingleton<CzechSentenceBuilder>();

            return services;
        }
    }
}
