using Grammar.Core.Interfaces;
using Grammar.Czech.Interfaces;
using Grammar.Czech.Models;
using Grammar.Czech.Providers;
using Grammar.Czech.Providers.JsonProviders;
using Grammar.Czech.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Grammar.Czech
{
    public static class CzechGrammarServiceFactory
    {
        /// <summary>
        /// Registruje všechny datové providery a služby pro českou gramatiku.
        /// </summary>
        /// <param name="services">Service collection (např. z Program.cs).</param>
        /// <param name="dataPath">Cesta ke složce s daty (např. "Data/").</param>
        /// <returns>ServiceCollection s přidanými službami.</returns>
        public static IServiceCollection AddCzechGrammarServices(this IServiceCollection services)
        {

            // Providers
            services.AddSingleton<IVerbDataProvider>(new JsonVerbDataProvider());
            services.AddSingleton<INounDataProvider>(new JsonNounDataProvider());
            services.AddSingleton<IAdjectiveDataProvider>(new JsonAdjectiveDataProvider());
            services.AddSingleton<IPrefixDataProvider>(new JsonPrefixDataProvider());
            services.AddSingleton<IParticleDataProvider>(new JsonParticlesDataProvider());
            services.AddSingleton<IPrepositionDataProvider>(new JsonPrepositionsDataProvider());
            services.AddSingleton<IPronounDataProvider>(new JsonPronounDataProvider());

            // Services
            services.AddSingleton<IPhonemeRegistry, CzechPhonemeRegistry>();
            services.AddSingleton<ICzechPhonologyService, CzechPhonologyService>();
            services.AddSingleton<IPhonologyService<CzechWordRequest>>(sp => sp.GetRequiredService<ICzechPhonologyService>());
            services.AddSingleton<IWordStructureResolver<CzechWordRequest>, CzechWordStructureResolver>();
            services.AddSingleton<ISofteningRuleEvaluator<CzechWordRequest>, CzechSofteningRuleEvaluator>();
            services.AddSingleton<IEpenthesisRuleEvaluator<CzechWordRequest>, CzechEpenthesisRuleEvaluator>();
            services.AddSingleton<CzechVerbConjugationService>();
            services.AddSingleton<CzechNounDeclensionService>();
            services.AddSingleton<CzechAdjectiveDeclensionService>();
            services.AddSingleton<CzechPronounService>();
            services.AddSingleton<ICzechPronounService>(sp => sp.GetRequiredService<CzechPronounService>());
            services.AddSingleton<IInflectionService<CzechWordRequest>>(sp => sp.GetRequiredService<CzechPronounService>());

            services.AddSingleton<CzechPrefixService>();
            services.AddSingleton<ICzechPrefixService>(sp => sp.GetRequiredService<CzechPrefixService>());

            services.AddSingleton<CzechParticleService>();
            services.AddSingleton<ICzechParticleService>(sp => sp.GetRequiredService<CzechParticleService>());

            services.AddSingleton<ICzechPrepositionService, CzechPrepositionService>();

            services.AddSingleton<CzechAuxiliaryVerbService>();
            services.AddSingleton<CzechVerbPhraseBuilderService>();
            services.AddSingleton<INegationService<CzechWordRequest>, CzechNegationService>();

            services.AddSingleton<MorphologyEngine>();
            services.AddSingleton<CzechWordFormComposer>();

            return services;
        }
    }
}
