namespace Grammar.Core.Enums
{
    /// <summary>
    /// Specifies the reflexive type of a Czech verb, determining which particle (se/si) is required.
    /// </summary>
    public enum ReflexiveType
    {
        /// <summary>No reflexive particle.</summary>
        None,

        /// <summary>Reflexivum tantum — accusative se; verb exists only in reflexive form (bát se, smát se, dívat se).</summary>
        ReflexivumTantum_Se,

        /// <summary>Reflexivum tantum — dative si; verb exists only in reflexive form (stěžovat si, přát si, myslet si).</summary>
        ReflexivumTantum_Si,

        /// <summary>Derived reflexive — se; accusative PAT is reflexivised and drops from the frame (mýt → mýt se).</summary>
        DerivedReflexive_Se,

        /// <summary>Derived benefactive — si; dative benefactive slot is added while PAT remains (koupit → koupit si auto).</summary>
        DerivedBenefactive_Si,

        /// <summary>Reciprocal — se; mutual action between two ACT participants (potkat → potkat se).</summary>
        Reciprocal_Se,

        /// <summary>Deagentive passive — se; impersonal passive construction (tady se mluví česky).</summary>
        DeagentivePassive_Se
    }
}
