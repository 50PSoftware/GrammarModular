using Grammar.Core.Enums;
using Grammar.Core.Models.Valency;

namespace Grammar.Czech.Interfaces
{
    /// <summary>
    /// Defines operations for reading a verb's valency frame.
    /// </summary>
    public interface ICzechValencyService
    {
        /// <summary>
        /// Gets the valency frame for the supplied verb.
        /// </summary>
        /// <param name="verbLemma">The verb lemma.</param>
        /// <param name="frameLabel">The frame to pick when the verb has several, or null for the only one.</param>
        /// <returns>The frame, or <see langword="null"/> when the verb has none registered.</returns>
        /// <exception cref="System.InvalidOperationException">
        /// Thrown when the verb has several frames and the label does not identify one of them.
        /// </exception>
        ValencyFrame? GetFrame(string verbLemma, string? frameLabel);

        /// <summary>
        /// Gets the slot filling the supplied functor in the frame.
        /// </summary>
        /// <param name="frame">The frame to read.</param>
        /// <param name="functor">The functor to look for.</param>
        /// <returns>The slot, or <see langword="null"/> when the frame has none for that functor.</returns>
        ValencySlot? GetSlot(ValencyFrame frame, FgdFunctor functor);

        /// <summary>
        /// Determines whether the functor is an inner participant, which only a frame can license.
        /// </summary>
        /// <param name="functor">The functor to classify.</param>
        /// <returns><see langword="true"/> for an inner participant; otherwise, <see langword="false"/>.</returns>
        /// <remarks>
        /// The five inner participants are the aktanty of the Functional Generative Description, which NESČ
        /// lists as konatel, patiens, adresát, původ and výsledek — ACT, PAT, ADDR, ORIG, EFF. They belong
        /// to the verb, so a verb that has no slot for one cannot take it. Free modifications — time, place,
        /// manner and the rest — attach to any verb and are never licensed by the frame.
        /// <para>
        /// The other half of the FGD criterion, that an inner participant occurs at most once with a given
        /// verb while a free modification may repeat, is not enforced here: nothing stops a caller from
        /// passing two PAT elements.
        /// </para>
        /// </remarks>
        bool IsInnerParticipant(FgdFunctor functor);

        /// <summary>
        /// Determines whether the frame licenses the periphrastic passive.
        /// </summary>
        /// <param name="frame">The frame to judge.</param>
        /// <returns><see langword="true"/> when the verb can be passivized in this sense; otherwise, <see langword="false"/>.</returns>
        /// <remarks>
        /// NESČ states the condition on the -n-/-t- participle as a valency one — it is formed from a stem
        /// "s agentem a aspoň jedním pravým doplněním" — and the aktanty are exactly what
        /// <see cref="IsInnerParticipant"/> already recognizes. A verb with an agent and nothing else is a
        /// neergativum, which NESČ says does not passivize at all: <c>*Je běženo (Petrem)</c>.
        /// <para>
        /// This judges the construction, not the word. The participle of jít exists — IJP and Wikislovník
        /// both give <c>jit</c> — while the clause built on it does not, because the frame of jít holds a
        /// direction and no complement. That is why the answer lives here and not in the conjugation
        /// service, which only ever produces a form.
        /// </para>
        /// </remarks>
        bool LicensesPeriphrasticPassive(ValencyFrame frame);
    }
}
