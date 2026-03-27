using Grammar.Core.Enums;
using Grammar.Czech.Models;
using Grammar.Czech.Providers;
using Grammar.Czech.Providers.JsonProviders;
using Grammar.Czech.Services;

namespace Grammar.Czech.Test
{
    [TestClass]
    public sealed class PronounInflectionTests
    {
        private CzechPronounService service;

        [TestInitialize]
        public void Setup()
        {
            var registry = new CzechPhonemeRegistry();
            var phonologyService = new CzechPhonologyService(registry);
            var nounDataProvider = new JsonNounDataProvider();
            var verbDataProvider = new JsonVerbDataProvider();
            var prefixService = new CzechPrefixService(new JsonPrefixDataProvider());
            var rootProvider = new JsonRootProvider();
            var wordStructureResolver = new CzechWordStructureResolver(verbDataProvider, nounDataProvider, prefixService, phonologyService, rootProvider);
            var ortographyService = new CzechOrtographyService(registry);
            var adjectiveDataProvider = new JsonAdjectiveDataProvider();
            var adjectiveService = new CzechAdjectiveDeclensionService(adjectiveDataProvider, wordStructureResolver, phonologyService, ortographyService);
            var pronounDataProvider = new JsonPronounDataProvider();

            service = new CzechPronounService(pronounDataProvider, adjectiveService);
        }

        #region Osobní zájmena — výchozí tvary

        /// <summary>
        /// Pokrývá fixedForms osobních zájmen: já, ty, on, ona, ono, my, vy, oni, ony, ona_
        /// Testuje výchozí (default) tvar všech 7 pádů pro každé lemma.
        /// </summary>
        [DataTestMethod]
        // já
        [DataRow("já", "Nominative", null, null, null, "já", DisplayName = "já – nom")]
        [DataRow("já", "Genitive", null, null, null, "mě", DisplayName = "já – gen")]
        [DataRow("já", "Dative", null, null, null, "mně", DisplayName = "já – dat")]
        [DataRow("já", "Accusative", null, null, null, "mě", DisplayName = "já – akuz")]
        [DataRow("já", "Vocative", null, null, null, "já", DisplayName = "já – vok")]
        [DataRow("já", "Locative", null, null, null, "mně", DisplayName = "já – lok")]
        [DataRow("já", "Instrumental", null, null, null, "mnou", DisplayName = "já – ins")]
        // ty
        [DataRow("ty", "Nominative", null, null, null, "ty", DisplayName = "ty – nom")]
        [DataRow("ty", "Genitive", null, null, null, "tebe", DisplayName = "ty – gen")]
        [DataRow("ty", "Dative", null, null, null, "tobě", DisplayName = "ty – dat")]
        [DataRow("ty", "Accusative", null, null, null, "tebe", DisplayName = "ty – akuz")]
        [DataRow("ty", "Vocative", null, null, null, "ty", DisplayName = "ty – vok")]
        [DataRow("ty", "Locative", null, null, null, "tobě", DisplayName = "ty – lok")]
        [DataRow("ty", "Instrumental", null, null, null, "tebou", DisplayName = "ty – ins")]
        // on
        [DataRow("on", "Nominative", "Masculine", "Singular", null, "on", DisplayName = "on – nom")]
        [DataRow("on", "Genitive", "Masculine", "Singular", null, "jeho", DisplayName = "on – gen")]
        [DataRow("on", "Dative", "Masculine", "Singular", null, "jemu", DisplayName = "on – dat")]
        [DataRow("on", "Accusative", "Masculine", "Singular", null, "jeho", DisplayName = "on – akuz")]
        [DataRow("on", "Vocative", "Masculine", "Singular", null, "on", DisplayName = "on – vok")]
        [DataRow("on", "Locative", "Masculine", "Singular", null, "něm", DisplayName = "on – lok")]
        [DataRow("on", "Instrumental", "Masculine", "Singular", null, "jím", DisplayName = "on – ins")]
        // ona
        [DataRow("ona", "Nominative", "Feminine", "Singular", null, "ona", DisplayName = "ona – nom")]
        [DataRow("ona", "Genitive", "Feminine", "Singular", null, "jí", DisplayName = "ona – gen")]
        [DataRow("ona", "Dative", "Feminine", "Singular", null, "jí", DisplayName = "ona – dat")]
        [DataRow("ona", "Accusative", "Feminine", "Singular", null, "ji", DisplayName = "ona – akuz")]
        [DataRow("ona", "Vocative", "Feminine", "Singular", null, "ona", DisplayName = "ona – vok")]
        [DataRow("ona", "Locative", "Feminine", "Singular", null, "ní", DisplayName = "ona – lok")]
        [DataRow("ona", "Instrumental", "Feminine", "Singular", null, "jí", DisplayName = "ona – ins")]
        // ono
        [DataRow("ono", "Nominative", "Neuter", "Singular", null, "ono", DisplayName = "ono – nom")]
        [DataRow("ono", "Genitive", "Neuter", "Singular", null, "jeho", DisplayName = "ono – gen")]
        [DataRow("ono", "Dative", "Neuter", "Singular", null, "jemu", DisplayName = "ono – dat")]
        [DataRow("ono", "Accusative", "Neuter", "Singular", null, "je", DisplayName = "ono – akuz")]
        [DataRow("ono", "Vocative", "Neuter", "Singular", null, "ono", DisplayName = "ono – vok")]
        [DataRow("ono", "Locative", "Neuter", "Singular", null, "něm", DisplayName = "ono – lok")]
        [DataRow("ono", "Instrumental", "Neuter", "Singular", null, "jím", DisplayName = "ono – ins")]
        // my
        [DataRow("my", "Nominative", null, "Plural", null, "my", DisplayName = "my – nom")]
        [DataRow("my", "Genitive", null, "Plural", null, "nás", DisplayName = "my – gen")]
        [DataRow("my", "Dative", null, "Plural", null, "nám", DisplayName = "my – dat")]
        [DataRow("my", "Accusative", null, "Plural", null, "nás", DisplayName = "my – akuz")]
        [DataRow("my", "Vocative", null, "Plural", null, "my", DisplayName = "my – vok")]
        [DataRow("my", "Locative", null, "Plural", null, "nás", DisplayName = "my – lok")]
        [DataRow("my", "Instrumental", null, "Plural", null, "námi", DisplayName = "my – ins")]
        // vy
        [DataRow("vy", "Nominative", null, "Plural", null, "vy", DisplayName = "vy – nom")]
        [DataRow("vy", "Genitive", null, "Plural", null, "vás", DisplayName = "vy – gen")]
        [DataRow("vy", "Dative", null, "Plural", null, "vám", DisplayName = "vy – dat")]
        [DataRow("vy", "Accusative", null, "Plural", null, "vás", DisplayName = "vy – akuz")]
        [DataRow("vy", "Vocative", null, "Plural", null, "vy", DisplayName = "vy – vok")]
        [DataRow("vy", "Locative", null, "Plural", null, "vás", DisplayName = "vy – lok")]
        [DataRow("vy", "Instrumental", null, "Plural", null, "vámi", DisplayName = "vy – ins")]
        // oni
        [DataRow("oni", "Nominative", "Masculine", "Plural", null, "oni", DisplayName = "oni – nom")]
        [DataRow("oni", "Genitive", "Masculine", "Plural", null, "jich", DisplayName = "oni – gen")]
        [DataRow("oni", "Dative", "Masculine", "Plural", null, "jim", DisplayName = "oni – dat")]
        [DataRow("oni", "Accusative", "Masculine", "Plural", null, "je", DisplayName = "oni – akuz")]
        [DataRow("oni", "Vocative", "Masculine", "Plural", null, "oni", DisplayName = "oni – vok")]
        [DataRow("oni", "Locative", "Masculine", "Plural", null, "nich", DisplayName = "oni – lok")]
        [DataRow("oni", "Instrumental", "Masculine", "Plural", null, "jimi", DisplayName = "oni – ins")]
        // ony
        [DataRow("ony", "Nominative", "Feminine", "Plural", null, "ony", DisplayName = "ony – nom")]
        [DataRow("ony", "Genitive", "Feminine", "Plural", null, "jich", DisplayName = "ony – gen")]
        [DataRow("ony", "Dative", "Feminine", "Plural", null, "jim", DisplayName = "ony – dat")]
        [DataRow("ony", "Accusative", "Feminine", "Plural", null, "je", DisplayName = "ony – akuz")]
        [DataRow("ony", "Locative", "Feminine", "Plural", null, "nich", DisplayName = "ony – lok")]
        [DataRow("ony", "Instrumental", "Feminine", "Plural", null, "jimi", DisplayName = "ony – ins")]
        // ona_ (neutr plurál)
        [DataRow("ona_", "Nominative", "Neuter", "Plural", null, "ona", DisplayName = "ona_ – nom")]
        [DataRow("ona_", "Genitive", "Neuter", "Plural", null, "jich", DisplayName = "ona_ – gen")]
        [DataRow("ona_", "Dative", "Neuter", "Plural", null, "jim", DisplayName = "ona_ – dat")]
        [DataRow("ona_", "Accusative", "Neuter", "Plural", null, "je", DisplayName = "ona_ – akuz")]
        [DataRow("ona_", "Locative", "Neuter", "Plural", null, "nich", DisplayName = "ona_ – lok")]
        [DataRow("ona_", "Instrumental", "Neuter", "Plural", null, "jimi", DisplayName = "ona_ – ins")]
        public void GetForm_PersonalPronoun_DefaultForms(
            string lemma, string caseName, string? gender, string? number, bool? isAnimate, string expected)
        {
            var request = BuildRequest(lemma, caseName, gender, number, isAnimate);
            var result = service.GetForm(request);
            Assert.AreEqual(expected, result.Form);
        }

        #endregion Osobní zájmena — výchozí tvary

        #region Osobní zájmena — tvary po předložce

        [DataTestMethod]
        // on
        [DataRow("on", "Genitive", "Masculine", "Singular", null, "něho", DisplayName = "on – gen pp")]
        [DataRow("on", "Dative", "Masculine", "Singular", null, "němu", DisplayName = "on – dat pp")]
        [DataRow("on", "Accusative", "Masculine", "Singular", null, "něj", DisplayName = "on – akuz pp")]
        [DataRow("on", "Instrumental", "Masculine", "Singular", null, "ním", DisplayName = "on – ins pp")]
        // ona
        [DataRow("ona", "Genitive", "Feminine", "Singular", null, "ní", DisplayName = "ona – gen pp")]
        [DataRow("ona", "Accusative", "Feminine", "Singular", null, "ni", DisplayName = "ona – akuz pp")]
        [DataRow("ona", "Instrumental", "Feminine", "Singular", null, "ní", DisplayName = "ona – ins pp")]
        // ono
        [DataRow("ono", "Genitive", "Neuter", "Singular", null, "něho", DisplayName = "ono – gen pp")]
        [DataRow("ono", "Dative", "Neuter", "Singular", null, "němu", DisplayName = "ono – dat pp")]
        [DataRow("ono", "Accusative", "Neuter", "Singular", null, "ně", DisplayName = "ono – akuz pp")]
        [DataRow("ono", "Instrumental", "Neuter", "Singular", null, "ním", DisplayName = "ono – ins pp")]
        // oni
        [DataRow("oni", "Genitive", "Masculine", "Plural", null, "nich", DisplayName = "oni – gen pp")]
        [DataRow("oni", "Dative", "Masculine", "Plural", null, "nim", DisplayName = "oni – dat pp")]
        [DataRow("oni", "Accusative", "Masculine", "Plural", null, "ně", DisplayName = "oni – akuz pp")]
        [DataRow("oni", "Instrumental", "Masculine", "Plural", null, "nimi", DisplayName = "oni – ins pp")]
        // ony
        [DataRow("ony", "Genitive", "Feminine", "Plural", null, "nich", DisplayName = "ony – gen pp")]
        [DataRow("ony", "Accusative", "Feminine", "Plural", null, "ně", DisplayName = "ony – akuz pp")]
        // ona_
        [DataRow("ona_", "Genitive", "Neuter", "Plural", null, "nich", DisplayName = "ona_ – gen pp")]
        [DataRow("ona_", "Accusative", "Neuter", "Plural", null, "ně", DisplayName = "ona_ – akuz pp")]
        public void GetForm_PersonalPronoun_AfterPreposition(
            string lemma, string caseName, string? gender, string? number, bool? isAnimate, string expected)
        {
            var request = BuildRequest(lemma, caseName, gender, number, isAnimate, afterPreposition: true);
            var result = service.GetForm(request);
            Assert.AreEqual(expected, result.Form);
        }

        #endregion Osobní zájmena — tvary po předložce

        #region Zvratné zájmeno — se

        [DataTestMethod]
        [DataRow("Genitive", "sebe", DisplayName = "se – gen")]
        [DataRow("Dative", "sobě", DisplayName = "se – dat")]
        [DataRow("Accusative", "sebe", DisplayName = "se – akuz")]
        [DataRow("Locative", "sobě", DisplayName = "se – lok")]
        [DataRow("Instrumental", "sebou", DisplayName = "se – ins")]
        public void GetForm_ReflexiveSe_DefaultForms(string caseName, string expected)
        {
            var request = BuildRequest("se", caseName, null, null, null);
            var result = service.GetForm(request);
            Assert.AreEqual(expected, result.Form);
        }

        #endregion Zvratné zájmeno — se

        #region Nesklonná zájmena — jeho, jejich

        [DataTestMethod]
        [DataRow("jeho", "Nominative", DisplayName = "jeho – nom")]
        [DataRow("jeho", "Genitive", DisplayName = "jeho – gen")]
        [DataRow("jeho", "Dative", DisplayName = "jeho – dat")]
        [DataRow("jeho", "Accusative", DisplayName = "jeho – akuz")]
        [DataRow("jeho", "Locative", DisplayName = "jeho – lok")]
        [DataRow("jeho", "Instrumental", DisplayName = "jeho – ins")]
        [DataRow("jejich", "Nominative", DisplayName = "jejich – nom")]
        [DataRow("jejich", "Genitive", DisplayName = "jejich – gen")]
        [DataRow("jejich", "Dative", DisplayName = "jejich – dat")]
        [DataRow("jejich", "Accusative", DisplayName = "jejich – akuz")]
        [DataRow("jejich", "Locative", DisplayName = "jejich – lok")]
        [DataRow("jejich", "Instrumental", DisplayName = "jejich – ins")]
        public void GetForm_Indeclinable_AlwaysReturnsLemma(string lemma, string caseName)
        {
            var request = BuildRequest(lemma, caseName, null, null, null);
            var result = service.GetForm(request);
            Assert.AreEqual(lemma, result.Form);
        }

        #endregion Nesklonná zájmena — jeho, jejich

        #region Ukazovací zájmena — ten, tento, tenhle, onen, sám

        [DataTestMethod]
        // Singulár — mužský životný
        [DataRow("Nominative", "Masculine", "Singular", true, "ten", DisplayName = "ten – sg m.ž. nom")]
        [DataRow("Genitive", "Masculine", "Singular", true, "toho", DisplayName = "ten – sg m.ž. gen")]
        [DataRow("Dative", "Masculine", "Singular", true, "tomu", DisplayName = "ten – sg m.ž. dat")]
        [DataRow("Accusative", "Masculine", "Singular", true, "toho", DisplayName = "ten – sg m.ž. akuz")]
        [DataRow("Vocative", "Masculine", "Singular", true, "ten", DisplayName = "ten – sg m.ž. vok")]
        [DataRow("Locative", "Masculine", "Singular", true, "tom", DisplayName = "ten – sg m.ž. lok")]
        [DataRow("Instrumental", "Masculine", "Singular", true, "tím", DisplayName = "ten – sg m.ž. ins")]
        // Singulár — mužský neživotný
        [DataRow("Accusative", "Masculine", "Singular", false, "ten", DisplayName = "ten – sg m.n. akuz")]
        // Singulár — ženský
        [DataRow("Nominative", "Feminine", "Singular", null, "ta", DisplayName = "ten – sg f nom")]
        [DataRow("Genitive", "Feminine", "Singular", null, "té", DisplayName = "ten – sg f gen")]
        [DataRow("Dative", "Feminine", "Singular", null, "té", DisplayName = "ten – sg f dat")]
        [DataRow("Accusative", "Feminine", "Singular", null, "tu", DisplayName = "ten – sg f akuz")]
        [DataRow("Locative", "Feminine", "Singular", null, "té", DisplayName = "ten – sg f lok")]
        [DataRow("Instrumental", "Feminine", "Singular", null, "tou", DisplayName = "ten – sg f ins")]
        // Singulár — střední
        [DataRow("Nominative", "Neuter", "Singular", null, "to", DisplayName = "ten – sg n nom")]
        [DataRow("Accusative", "Neuter", "Singular", null, "to", DisplayName = "ten – sg n akuz")]
        [DataRow("Instrumental", "Neuter", "Singular", null, "tím", DisplayName = "ten – sg n ins")]
        // Plurál — mužský životný
        [DataRow("Nominative", "Masculine", "Plural", true, "ti", DisplayName = "ten – pl m.ž. nom")]
        [DataRow("Genitive", "Masculine", "Plural", true, "těch", DisplayName = "ten – pl m.ž. gen")]
        [DataRow("Dative", "Masculine", "Plural", true, "těm", DisplayName = "ten – pl m.ž. dat")]
        [DataRow("Accusative", "Masculine", "Plural", true, "ty", DisplayName = "ten – pl m.ž. akuz")]
        [DataRow("Locative", "Masculine", "Plural", true, "těch", DisplayName = "ten – pl m.ž. lok")]
        [DataRow("Instrumental", "Masculine", "Plural", true, "těmi", DisplayName = "ten – pl m.ž. ins")]
        // Plurál — ostatní
        [DataRow("Nominative", "Feminine", "Plural", null, "ty", DisplayName = "ten – pl f nom")]
        [DataRow("Nominative", "Neuter", "Plural", null, "ty", DisplayName = "ten – pl n nom")]
        [DataRow("Instrumental", "Feminine", "Plural", null, "těmi", DisplayName = "ten – pl f ins")]
        public void GetForm_TenDemonstrative(
            string caseName, string gender, string number, bool? isAnimate, string expected)
        {
            var request = BuildRequest("ten", caseName, gender, number, isAnimate);
            var result = service.GetForm(request);
            Assert.AreEqual(expected, result.Form);
        }

        [DataTestMethod]
        // Singulár — mužský životný
        [DataRow("Nominative", "Masculine", "Singular", true, "tento", DisplayName = "tento – sg m.ž. nom")]
        [DataRow("Genitive", "Masculine", "Singular", true, "tohoto", DisplayName = "tento – sg m.ž. gen")]
        [DataRow("Accusative", "Masculine", "Singular", true, "tohoto", DisplayName = "tento – sg m.ž. akuz")]
        [DataRow("Accusative", "Masculine", "Singular", false, "tento", DisplayName = "tento – sg m.n. akuz")]
        [DataRow("Locative", "Masculine", "Singular", true, "tomto", DisplayName = "tento – sg m.ž. lok")]
        [DataRow("Instrumental", "Masculine", "Singular", true, "tímto", DisplayName = "tento – sg m.ž. ins")]
        // Singulár — ženský
        [DataRow("Nominative", "Feminine", "Singular", null, "tato", DisplayName = "tento – sg f nom")]
        [DataRow("Accusative", "Feminine", "Singular", null, "tuto", DisplayName = "tento – sg f akuz")]
        [DataRow("Instrumental", "Feminine", "Singular", null, "touto", DisplayName = "tento – sg f ins")]
        // Singulár — střední
        [DataRow("Nominative", "Neuter", "Singular", null, "toto", DisplayName = "tento – sg n nom")]
        // Plurál
        [DataRow("Nominative", "Masculine", "Plural", true, "tito", DisplayName = "tento – pl m.ž. nom")]
        [DataRow("Nominative", "Feminine", "Plural", null, "tyto", DisplayName = "tento – pl f nom")]
        [DataRow("Genitive", "Masculine", "Plural", true, "těchto", DisplayName = "tento – pl gen")]
        [DataRow("Instrumental", "Masculine", "Plural", true, "těmito", DisplayName = "tento – pl ins")]
        public void GetForm_TentoDemonstrative(
            string caseName, string gender, string number, bool? isAnimate, string expected)
        {
            var request = BuildRequest("tento", caseName, gender, number, isAnimate);
            var result = service.GetForm(request);
            Assert.AreEqual(expected, result.Form);
        }

        [DataTestMethod]
        [DataRow("Nominative", "Masculine", "Singular", true, "tenhle", DisplayName = "tenhle – sg m.ž. nom")]
        [DataRow("Genitive", "Masculine", "Singular", true, "tohohle", DisplayName = "tenhle – sg m.ž. gen")]
        [DataRow("Accusative", "Masculine", "Singular", true, "tohohle", DisplayName = "tenhle – sg m.ž. akuz")]
        [DataRow("Accusative", "Masculine", "Singular", false, "tenhle", DisplayName = "tenhle – sg m.n. akuz")]
        [DataRow("Nominative", "Feminine", "Singular", null, "tahle", DisplayName = "tenhle – sg f nom")]
        [DataRow("Accusative", "Feminine", "Singular", null, "tuhle", DisplayName = "tenhle – sg f akuz")]
        [DataRow("Nominative", "Neuter", "Singular", null, "tohle", DisplayName = "tenhle – sg n nom")]
        [DataRow("Nominative", "Masculine", "Plural", true, "tihle", DisplayName = "tenhle – pl m.ž. nom")]
        [DataRow("Nominative", "Feminine", "Plural", null, "tyhle", DisplayName = "tenhle – pl f nom")]
        [DataRow("Genitive", "Masculine", "Plural", true, "těchhle", DisplayName = "tenhle – pl gen")]
        [DataRow("Instrumental", "Masculine", "Plural", true, "těmihle", DisplayName = "tenhle – pl ins")]
        public void GetForm_TenhleDemonstrative(
            string caseName, string gender, string number, bool? isAnimate, string expected)
        {
            var request = BuildRequest("tenhle", caseName, gender, number, isAnimate);
            var result = service.GetForm(request);
            Assert.AreEqual(expected, result.Form);
        }

        [DataTestMethod]
        [DataRow("Nominative", "Masculine", "Singular", true, "onen", DisplayName = "onen – sg m.ž. nom")]
        [DataRow("Genitive", "Masculine", "Singular", true, "onoho", DisplayName = "onen – sg m.ž. gen")]
        [DataRow("Accusative", "Masculine", "Singular", true, "onoho", DisplayName = "onen – sg m.ž. akuz")]
        [DataRow("Accusative", "Masculine", "Singular", false, "onen", DisplayName = "onen – sg m.n. akuz")]
        [DataRow("Nominative", "Feminine", "Singular", null, "ona", DisplayName = "onen – sg f nom")]
        [DataRow("Accusative", "Feminine", "Singular", null, "onu", DisplayName = "onen – sg f akuz")]
        [DataRow("Nominative", "Neuter", "Singular", null, "ono", DisplayName = "onen – sg n nom")]
        [DataRow("Nominative", "Masculine", "Plural", true, "oni", DisplayName = "onen – pl m.ž. nom")]
        [DataRow("Nominative", "Feminine", "Plural", null, "ony", DisplayName = "onen – pl f nom")]
        [DataRow("Genitive", "Masculine", "Plural", true, "oněch", DisplayName = "onen – pl gen")]
        [DataRow("Instrumental", "Masculine", "Plural", true, "oněmi", DisplayName = "onen – pl ins")]
        public void GetForm_OnenDemonstrative(
            string caseName, string gender, string number, bool? isAnimate, string expected)
        {
            var request = BuildRequest("onen", caseName, gender, number, isAnimate);
            var result = service.GetForm(request);
            Assert.AreEqual(expected, result.Form);
        }

        [DataTestMethod]
        [DataRow("Nominative", "Masculine", "Singular", true, "sám", DisplayName = "sám – sg m.ž. nom")]
        [DataRow("Genitive", "Masculine", "Singular", true, "samého", DisplayName = "sám – sg m.ž. gen")]
        [DataRow("Accusative", "Masculine", "Singular", true, "samého", DisplayName = "sám – sg m.ž. akuz")]
        [DataRow("Accusative", "Masculine", "Singular", false, "sám", DisplayName = "sám – sg m.n. akuz")]
        [DataRow("Nominative", "Feminine", "Singular", null, "sama", DisplayName = "sám – sg f nom")]
        [DataRow("Accusative", "Feminine", "Singular", null, "samu", DisplayName = "sám – sg f akuz")]
        [DataRow("Nominative", "Neuter", "Singular", null, "samo", DisplayName = "sám – sg n nom")]
        [DataRow("Nominative", "Masculine", "Plural", true, "sami", DisplayName = "sám – pl m.ž. nom")]
        [DataRow("Nominative", "Feminine", "Plural", null, "samy", DisplayName = "sám – pl f nom")]
        [DataRow("Instrumental", "Masculine", "Plural", true, "samými", DisplayName = "sám – pl ins")]
        public void GetForm_SamDemonstrative(
            string caseName, string gender, string number, bool? isAnimate, string expected)
        {
            var request = BuildRequest("sám", caseName, gender, number, isAnimate);
            var result = service.GetForm(request);
            Assert.AreEqual(expected, result.Form);
        }

        #endregion Ukazovací zájmena — ten, tento, tenhle, onen, sám

        #region Přivlastňovací zájmena — můj, tvůj, svůj, náš, váš, její

        [DataTestMethod]
        // Singulár — mužský životný
        [DataRow("Nominative", "Masculine", "Singular", true, "můj", DisplayName = "můj – sg m.ž. nom")]
        [DataRow("Genitive", "Masculine", "Singular", true, "mého", DisplayName = "můj – sg m.ž. gen")]
        [DataRow("Dative", "Masculine", "Singular", true, "mému", DisplayName = "můj – sg m.ž. dat")]
        [DataRow("Accusative", "Masculine", "Singular", true, "mého", DisplayName = "můj – sg m.ž. akuz")]
        [DataRow("Vocative", "Masculine", "Singular", true, "můj", DisplayName = "můj – sg m.ž. vok")]
        [DataRow("Locative", "Masculine", "Singular", true, "mém", DisplayName = "můj – sg m.ž. lok")]
        [DataRow("Instrumental", "Masculine", "Singular", true, "mým", DisplayName = "můj – sg m.ž. ins")]
        // Singulár — mužský neživotný
        [DataRow("Accusative", "Masculine", "Singular", false, "můj", DisplayName = "můj – sg m.n. akuz")]
        // Singulár — ženský
        [DataRow("Nominative", "Feminine", "Singular", null, "má", DisplayName = "můj – sg f nom")]
        [DataRow("Genitive", "Feminine", "Singular", null, "mé", DisplayName = "můj – sg f gen")]
        [DataRow("Dative", "Feminine", "Singular", null, "mé", DisplayName = "můj – sg f dat")]
        [DataRow("Accusative", "Feminine", "Singular", null, "mou", DisplayName = "můj – sg f akuz")]
        [DataRow("Locative", "Feminine", "Singular", null, "mé", DisplayName = "můj – sg f lok")]
        [DataRow("Instrumental", "Feminine", "Singular", null, "mou", DisplayName = "můj – sg f ins")]
        // Singulár — střední
        [DataRow("Nominative", "Neuter", "Singular", null, "mé", DisplayName = "můj – sg n nom")]
        [DataRow("Accusative", "Neuter", "Singular", null, "mé", DisplayName = "můj – sg n akuz")]
        [DataRow("Instrumental", "Neuter", "Singular", null, "mým", DisplayName = "můj – sg n ins")]
        // Plurál — mužský životný
        [DataRow("Nominative", "Masculine", "Plural", true, "moji", DisplayName = "můj – pl m.ž. nom")]
        [DataRow("Genitive", "Masculine", "Plural", true, "mých", DisplayName = "můj – pl m.ž. gen")]
        [DataRow("Dative", "Masculine", "Plural", true, "mým", DisplayName = "můj – pl m.ž. dat")]
        [DataRow("Accusative", "Masculine", "Plural", true, "moje", DisplayName = "můj – pl m.ž. akuz")]
        [DataRow("Locative", "Masculine", "Plural", true, "mých", DisplayName = "můj – pl m.ž. lok")]
        [DataRow("Instrumental", "Masculine", "Plural", true, "mými", DisplayName = "můj – pl m.ž. ins")]
        // Plurál — ostatní
        [DataRow("Nominative", "Feminine", "Plural", null, "moje", DisplayName = "můj – pl f nom")]
        [DataRow("Nominative", "Neuter", "Plural", null, "moje", DisplayName = "můj – pl n nom")]
        public void GetForm_MujPossessive(
            string caseName, string gender, string number, bool? isAnimate, string expected)
        {
            var request = BuildRequest("můj", caseName, gender, number, isAnimate);
            var result = service.GetForm(request);
            Assert.AreEqual(expected, result.Form);
        }

        [DataTestMethod]
        [DataRow("Nominative", "Masculine", "Singular", true, "tvůj", DisplayName = "tvůj – sg m.ž. nom")]
        [DataRow("Genitive", "Masculine", "Singular", true, "tvého", DisplayName = "tvůj – sg m.ž. gen")]
        [DataRow("Accusative", "Masculine", "Singular", true, "tvého", DisplayName = "tvůj – sg m.ž. akuz")]
        [DataRow("Accusative", "Masculine", "Singular", false, "tvůj", DisplayName = "tvůj – sg m.n. akuz")]
        [DataRow("Nominative", "Feminine", "Singular", null, "tvá", DisplayName = "tvůj – sg f nom")]
        [DataRow("Accusative", "Feminine", "Singular", null, "tvou", DisplayName = "tvůj – sg f akuz")]
        [DataRow("Nominative", "Neuter", "Singular", null, "tvé", DisplayName = "tvůj – sg n nom")]
        [DataRow("Nominative", "Masculine", "Plural", true, "tvoji", DisplayName = "tvůj – pl m.ž. nom")]
        [DataRow("Nominative", "Feminine", "Plural", null, "tvoje", DisplayName = "tvůj – pl f nom")]
        [DataRow("Genitive", "Masculine", "Plural", true, "tvých", DisplayName = "tvůj – pl gen")]
        [DataRow("Instrumental", "Masculine", "Plural", true, "tvými", DisplayName = "tvůj – pl ins")]
        public void GetForm_TvujPossessive(
            string caseName, string gender, string number, bool? isAnimate, string expected)
        {
            var request = BuildRequest("tvůj", caseName, gender, number, isAnimate);
            var result = service.GetForm(request);
            Assert.AreEqual(expected, result.Form);
        }

        [DataTestMethod]
        [DataRow("Nominative", "Masculine", "Singular", true, "svůj", DisplayName = "svůj – sg m.ž. nom")]
        [DataRow("Genitive", "Masculine", "Singular", true, "svého", DisplayName = "svůj – sg m.ž. gen")]
        [DataRow("Accusative", "Masculine", "Singular", true, "svého", DisplayName = "svůj – sg m.ž. akuz")]
        [DataRow("Accusative", "Masculine", "Singular", false, "svůj", DisplayName = "svůj – sg m.n. akuz")]
        [DataRow("Nominative", "Feminine", "Singular", null, "svá", DisplayName = "svůj – sg f nom")]
        [DataRow("Accusative", "Feminine", "Singular", null, "svou", DisplayName = "svůj – sg f akuz")]
        [DataRow("Nominative", "Neuter", "Singular", null, "své", DisplayName = "svůj – sg n nom")]
        [DataRow("Nominative", "Masculine", "Plural", true, "svoji", DisplayName = "svůj – pl m.ž. nom")]
        [DataRow("Nominative", "Feminine", "Plural", null, "svoje", DisplayName = "svůj – pl f nom")]
        [DataRow("Instrumental", "Masculine", "Plural", true, "svými", DisplayName = "svůj – pl ins")]
        public void GetForm_SvujPossessive(
            string caseName, string gender, string number, bool? isAnimate, string expected)
        {
            var request = BuildRequest("svůj", caseName, gender, number, isAnimate);
            var result = service.GetForm(request);
            Assert.AreEqual(expected, result.Form);
        }

        [DataTestMethod]
        // Singulár — mužský životný
        [DataRow("Nominative", "Masculine", "Singular", true, "náš", DisplayName = "náš – sg m.ž. nom")]
        [DataRow("Genitive", "Masculine", "Singular", true, "našeho", DisplayName = "náš – sg m.ž. gen")]
        [DataRow("Dative", "Masculine", "Singular", true, "našemu", DisplayName = "náš – sg m.ž. dat")]
        [DataRow("Accusative", "Masculine", "Singular", true, "našeho", DisplayName = "náš – sg m.ž. akuz")]
        [DataRow("Vocative", "Masculine", "Singular", true, "náš", DisplayName = "náš – sg m.ž. vok")]
        [DataRow("Locative", "Masculine", "Singular", true, "našem", DisplayName = "náš – sg m.ž. lok")]
        [DataRow("Instrumental", "Masculine", "Singular", true, "naším", DisplayName = "náš – sg m.ž. ins")]
        // Singulár — mužský neživotný
        [DataRow("Accusative", "Masculine", "Singular", false, "náš", DisplayName = "náš – sg m.n. akuz")]
        // Singulár — ženský
        [DataRow("Nominative", "Feminine", "Singular", null, "naše", DisplayName = "náš – sg f nom")]
        [DataRow("Genitive", "Feminine", "Singular", null, "naší", DisplayName = "náš – sg f gen")]
        [DataRow("Dative", "Feminine", "Singular", null, "naší", DisplayName = "náš – sg f dat")]
        [DataRow("Accusative", "Feminine", "Singular", null, "naši", DisplayName = "náš – sg f akuz")]
        [DataRow("Locative", "Feminine", "Singular", null, "naší", DisplayName = "náš – sg f lok")]
        [DataRow("Instrumental", "Feminine", "Singular", null, "naší", DisplayName = "náš – sg f ins")]
        // Singulár — střední
        [DataRow("Nominative", "Neuter", "Singular", null, "naše", DisplayName = "náš – sg n nom")]
        [DataRow("Accusative", "Neuter", "Singular", null, "naše", DisplayName = "náš – sg n akuz")]
        [DataRow("Instrumental", "Neuter", "Singular", null, "naším", DisplayName = "náš – sg n ins")]
        // Plurál — mužský životný
        [DataRow("Nominative", "Masculine", "Plural", true, "naši", DisplayName = "náš – pl m.ž. nom")]
        [DataRow("Genitive", "Masculine", "Plural", true, "našich", DisplayName = "náš – pl m.ž. gen")]
        [DataRow("Dative", "Masculine", "Plural", true, "našim", DisplayName = "náš – pl m.ž. dat")]
        [DataRow("Accusative", "Masculine", "Plural", true, "naše", DisplayName = "náš – pl m.ž. akuz")]
        [DataRow("Locative", "Masculine", "Plural", true, "našich", DisplayName = "náš – pl m.ž. lok")]
        [DataRow("Instrumental", "Masculine", "Plural", true, "našimi", DisplayName = "náš – pl m.ž. ins")]
        // Plurál — ostatní
        [DataRow("Nominative", "Feminine", "Plural", null, "naše", DisplayName = "náš – pl f nom")]
        [DataRow("Nominative", "Neuter", "Plural", null, "naše", DisplayName = "náš – pl n nom")]
        [DataRow("Instrumental", "Feminine", "Plural", null, "našimi", DisplayName = "náš – pl f ins")]
        public void GetForm_NasPossessive(
            string caseName, string gender, string number, bool? isAnimate, string expected)
        {
            var request = BuildRequest("náš", caseName, gender, number, isAnimate);
            var result = service.GetForm(request);
            Assert.AreEqual(expected, result.Form);
        }

        [DataTestMethod]
        [DataRow("Nominative", "Masculine", "Singular", true, "váš", DisplayName = "váš – sg m.ž. nom")]
        [DataRow("Genitive", "Masculine", "Singular", true, "vašeho", DisplayName = "váš – sg m.ž. gen")]
        [DataRow("Accusative", "Masculine", "Singular", true, "vašeho", DisplayName = "váš – sg m.ž. akuz")]
        [DataRow("Accusative", "Masculine", "Singular", false, "váš", DisplayName = "váš – sg m.n. akuz")]
        [DataRow("Nominative", "Feminine", "Singular", null, "vaše", DisplayName = "váš – sg f nom")]
        [DataRow("Genitive", "Feminine", "Singular", null, "vaší", DisplayName = "váš – sg f gen")]
        [DataRow("Accusative", "Feminine", "Singular", null, "vaši", DisplayName = "váš – sg f akuz")]
        [DataRow("Nominative", "Neuter", "Singular", null, "vaše", DisplayName = "váš – sg n nom")]
        [DataRow("Nominative", "Masculine", "Plural", true, "vaši", DisplayName = "váš – pl m.ž. nom")]
        [DataRow("Nominative", "Feminine", "Plural", null, "vaše", DisplayName = "váš – pl f nom")]
        [DataRow("Genitive", "Masculine", "Plural", true, "vašich", DisplayName = "váš – pl gen")]
        [DataRow("Instrumental", "Masculine", "Plural", true, "vašimi", DisplayName = "váš – pl ins")]
        public void GetForm_VasPossessive(
            string caseName, string gender, string number, bool? isAnimate, string expected)
        {
            var request = BuildRequest("váš", caseName, gender, number, isAnimate);
            var result = service.GetForm(request);
            Assert.AreEqual(expected, result.Form);
        }

        [DataTestMethod]
        // Singulár — mužský životný
        [DataRow("Nominative", "Masculine", "Singular", true, "její", DisplayName = "její – sg m.ž. nom")]
        [DataRow("Genitive", "Masculine", "Singular", true, "jejího", DisplayName = "její – sg m.ž. gen")]
        [DataRow("Dative", "Masculine", "Singular", true, "jejímu", DisplayName = "její – sg m.ž. dat")]
        [DataRow("Accusative", "Masculine", "Singular", true, "jejího", DisplayName = "její – sg m.ž. akuz")]
        [DataRow("Locative", "Masculine", "Singular", true, "jejím", DisplayName = "její – sg m.ž. lok")]
        [DataRow("Instrumental", "Masculine", "Singular", true, "jejím", DisplayName = "její – sg m.ž. ins")]
        // Singulár — mužský neživotný
        [DataRow("Accusative", "Masculine", "Singular", false, "její", DisplayName = "její – sg m.n. akuz")]
        // Singulár — ženský (invariantní)
        [DataRow("Nominative", "Feminine", "Singular", null, "její", DisplayName = "její – sg f nom")]
        [DataRow("Genitive", "Feminine", "Singular", null, "její", DisplayName = "její – sg f gen")]
        [DataRow("Accusative", "Feminine", "Singular", null, "její", DisplayName = "její – sg f akuz")]
        [DataRow("Instrumental", "Feminine", "Singular", null, "její", DisplayName = "její – sg f ins")]
        // Singulár — střední
        [DataRow("Nominative", "Neuter", "Singular", null, "její", DisplayName = "její – sg n nom")]
        [DataRow("Instrumental", "Neuter", "Singular", null, "jejím", DisplayName = "její – sg n ins")]
        // Plurál
        [DataRow("Nominative", "Masculine", "Plural", true, "její", DisplayName = "její – pl m.ž. nom")]
        [DataRow("Genitive", "Masculine", "Plural", true, "jejích", DisplayName = "její – pl gen")]
        [DataRow("Dative", "Masculine", "Plural", true, "jejím", DisplayName = "její – pl dat")]
        [DataRow("Instrumental", "Masculine", "Plural", true, "jejími", DisplayName = "její – pl ins")]
        public void GetForm_JejiPossessive(
            string caseName, string gender, string number, bool? isAnimate, string expected)
        {
            var request = BuildRequest("její", caseName, gender, number, isAnimate);
            var result = service.GetForm(request);
            Assert.AreEqual(expected, result.Form);
        }

        #endregion Přivlastňovací zájmena — můj, tvůj, svůj, náš, váš, její

        #region Tázací zájmena — kdo, co

        [DataTestMethod]
        [DataRow("kdo", "Nominative", "Masculine", "Singular", true, "kdo", DisplayName = "kdo – nom")]
        [DataRow("kdo", "Genitive", "Masculine", "Singular", true, "koho", DisplayName = "kdo – gen")]
        [DataRow("kdo", "Dative", "Masculine", "Singular", true, "komu", DisplayName = "kdo – dat")]
        [DataRow("kdo", "Accusative", "Masculine", "Singular", true, "koho", DisplayName = "kdo – akuz")]
        [DataRow("kdo", "Vocative", "Masculine", "Singular", true, "kdo", DisplayName = "kdo – vok")]
        [DataRow("kdo", "Locative", "Masculine", "Singular", true, "kom", DisplayName = "kdo – lok")]
        [DataRow("kdo", "Instrumental", "Masculine", "Singular", true, "kým", DisplayName = "kdo – ins")]
        [DataRow("co", "Nominative", "Neuter", "Singular", null, "co", DisplayName = "co – nom")]
        [DataRow("co", "Genitive", "Neuter", "Singular", null, "čeho", DisplayName = "co – gen")]
        [DataRow("co", "Dative", "Neuter", "Singular", null, "čemu", DisplayName = "co – dat")]
        [DataRow("co", "Accusative", "Neuter", "Singular", null, "co", DisplayName = "co – akuz")]
        [DataRow("co", "Locative", "Neuter", "Singular", null, "čem", DisplayName = "co – lok")]
        [DataRow("co", "Instrumental", "Neuter", "Singular", null, "čím", DisplayName = "co – ins")]
        public void GetForm_InterrogativePronoun(
            string lemma, string caseName, string gender, string number, bool? isAnimate, string expected)
        {
            var request = BuildRequest(lemma, caseName, gender, number, isAnimate);
            var result = service.GetForm(request);
            Assert.AreEqual(expected, result.Form);
        }

        #endregion Tázací zájmena — kdo, co

        #region Vztažné zájmeno — jenž

        [DataTestMethod]
        // Singulár — mužský životný
        [DataRow("Nominative", "Masculine", "Singular", true, "jenž", DisplayName = "jenž – sg m.ž. nom")]
        [DataRow("Genitive", "Masculine", "Singular", true, "jehož", DisplayName = "jenž – sg m.ž. gen")]
        [DataRow("Dative", "Masculine", "Singular", true, "jemuž", DisplayName = "jenž – sg m.ž. dat")]
        [DataRow("Accusative", "Masculine", "Singular", true, "jejž", DisplayName = "jenž – sg m.ž. akuz")]
        [DataRow("Locative", "Masculine", "Singular", true, "němž", DisplayName = "jenž – sg m.ž. lok")]
        [DataRow("Instrumental", "Masculine", "Singular", true, "jímž", DisplayName = "jenž – sg m.ž. ins")]
        // Singulár — mužský neživotný
        [DataRow("Accusative", "Masculine", "Singular", false, "jenž", DisplayName = "jenž – sg m.n. akuz")]
        // Singulár — ženský
        [DataRow("Nominative", "Feminine", "Singular", null, "jež", DisplayName = "jenž – sg f nom")]
        [DataRow("Genitive", "Feminine", "Singular", null, "jejíž", DisplayName = "jenž – sg f gen")]
        [DataRow("Instrumental", "Feminine", "Singular", null, "jíž", DisplayName = "jenž – sg f ins")]
        // Singulár — střední
        [DataRow("Nominative", "Neuter", "Singular", null, "jež", DisplayName = "jenž – sg n nom")]
        [DataRow("Genitive", "Neuter", "Singular", null, "jehož", DisplayName = "jenž – sg n gen")]
        // Plurál — mužský životný
        [DataRow("Nominative", "Masculine", "Plural", true, "již", DisplayName = "jenž – pl m.ž. nom")]
        [DataRow("Genitive", "Masculine", "Plural", true, "jejichž", DisplayName = "jenž – pl gen")]
        [DataRow("Dative", "Masculine", "Plural", true, "jimž", DisplayName = "jenž – pl dat")]
        [DataRow("Instrumental", "Masculine", "Plural", true, "jimiž", DisplayName = "jenž – pl ins")]
        // Plurál — ostatní
        [DataRow("Nominative", "Feminine", "Plural", null, "jež", DisplayName = "jenž – pl f nom")]
        public void GetForm_JenzRelative(
            string caseName, string gender, string number, bool? isAnimate, string expected)
        {
            var request = BuildRequest("jenž", caseName, gender, number, isAnimate);
            var result = service.GetForm(request);
            Assert.AreEqual(expected, result.Form);
        }

        #endregion Vztažné zájmeno — jenž

        #region Helper

        private static CzechWordRequest BuildRequest(
            string lemma,
            string caseName,
            string? gender,
            string? number,
            bool? isAnimate,
            bool afterPreposition = false) => new CzechWordRequest
            {
                Lemma = lemma,
                WordCategory = WordCategory.Pronoun,
                Case = Enum.Parse<Case>(caseName),
                Gender = gender is null ? null : Enum.Parse<Gender>(gender),
                Number = number is null ? null : Enum.Parse<Number>(number),
                IsAnimate = isAnimate,
                IsAfterPreposition = afterPreposition,
            };

        #endregion Helper
    }
}
