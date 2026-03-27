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
    /// Provides the <see cref="AddCzechGrammarServices"/> extension method for registering
    /// all Czech grammar services into an <see cref="IServiceCollection"/>.
    /// </summary>
    public static class CzechGrammarServiceFactory
    {
        /// <summary>
        /// Registers all data providers and services for Czech grammar.
        /// Call this once from <c>Program.cs</c> or your DI bootstrap.
        /// </summary>
        /// <param name="services">The service collection to register into.</param>
        /// <returns>The same <paramref name="services"/> for chaining.</returns>
        public static IServiceCollection AddCzechGrammarServices(this IServiceCollection services)
        {
            // ── Morphological data providers ────────────────────────────────────────
            services.AddSingleton<IVerbDataProvider>(new JsonVerbDataProvider());
            services.AddSingleton<INounDataProvider>(new JsonNounDataProvider());
            services.AddSingleton<IAdjectiveDataProvider>(new JsonAdjectiveDataProvider());
            services.AddSingleton<IPrefixDataProvider>(new JsonPrefixDataProvider());
            services.AddSingleton<IParticleDataProvider>(new JsonParticlesDataProvider());
            services.AddSingleton<IPrepositionDataProvider>(new JsonPrepositionsDataProvider());
            services.AddSingleton<IPronounDataProvider>(new JsonPronounDataProvider());

            // ── Root lexicon ─────────────────────────────────────────────────────────
            // ICzechRootProvider is the typed interface used within Grammar.Czech.
            // IRootProvider is the language-agnostic alias used by Grammar.Core consumers.
            // Both resolve to the same singleton instance.
            services.AddSingleton<ICzechRootProvider, JsonRootProvider>();
            services.AddSingleton<IRootProvider>(sp =>
                sp.GetRequiredService<ICzechRootProvider>());

            // ── Valency & lexical dictionary ─────────────────────────────────────────
            services.AddSingleton<IValencyProvider, JsonValencyProvider>();

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

            services.AddSingleton<CzechAuxiliaryVerbService>();
            services.AddSingleton<CzechVerbPhraseBuilderService>();
            services.AddSingleton<INegationService<CzechWordRequest>, CzechNegationService>();

            // ── Top-level entry points ───────────────────────────────────────────────
            services.AddSingleton<MorphologyEngine>();
            services.AddSingleton<CzechWordFormComposer>();

            return services;
        }
    }
}
