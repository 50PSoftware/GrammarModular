using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Grammar.Czech.Interfaces
{
    public interface ICzechOrtographyService
    {
        /// <summary>
        /// Ortografická konverze výsledku jotace: e→ě v koncovce.
        /// Zápis morfonologického procesu vložení /j/ po labiálách (pje→pě, bje→bě...).
        /// </summary>
        string ApplyJotationOrthography(string ending);

        /// <summary>
        /// Normalizace ě→e v koncovce kde ě ortograficky nedává smysl
        /// (non-DTN a non-labiální konsonant).
        /// </summary>
        string NormalizeEndingOrthography(string stem, string ending);

        /// <summary>
        /// Ortografická konverze e→ě po DTN konsonantech (d, t, n).
        /// Grafém ě po DTN signalizuje palatalizaci: ně=[ňe], dě=[ďe], tě=[ťe].
        /// Příklad: stem="kon" + "-e" → "-ě" → "koně" ✓
        /// Vokativ se neaplikuje — guard zajišťuje caller (ShouldApplyJotation vrátí false).
        /// </summary>
        string ApplyDTNEndingOrthography(string stem, string ending);
    }
}
