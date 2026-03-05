namespace Grammar.Czech.Interfaces
{
    public interface IAlternationRuleEvaluator
    {
        bool ShouldShortenVowel(string stem);
    }
}
