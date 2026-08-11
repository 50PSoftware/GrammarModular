-- Grammar.Czech — lexicon seed, update 23.
--
-- Continues seed.000.sql through seed.021.sql. Last ids used there: lexeme 37, lemma_entry 302,
-- lexical_unit 44, valency_frame 45, valency_slot 82, slot_realization 89, construction 3,
-- lemma_variant 1, lemma_sense 2. Append after all twenty-two, in order.
--
-- Provenance: the lemmas and their spelling from Data/Rules/adverbs.json, which came from the
-- Internetová jazyková příručka; the functor from the Functional Generative Description, the
-- annotation scheme the valency half of this dictionary already uses.
--
-- THE REST OF THE ADVERBS. seed.021.sql put in twenty-one and said the other two hundred and seventy
-- were left out because "most would be MANN, but most is not a rule". This file does them, and the way
-- it does them is the point: 91 of them rest on a rule and the remaining 171 were gone through one at
-- a time. Two hundred and sixty-two entries, ids 303 to 564.
--
-- WHAT RESTS ON A RULE
--
--   A deadjectival adverb answers "jak" — rychlý gives rychle, pečlivý pečlivě. adverbs.json records
--   what each adverb was derived from, so those 91 are MANN by derivation and not by anybody's
--   judgement. Five of that shape are not manner and were pulled out by hand: daleko, hluboko, nízko,
--   vysoko and široko answer "kde", and často and pravidelně answer "kdy".
--
-- WHAT WAS GONE THROUGH BY HAND
--
--   TWHEN  61  včetně frekvence: denně, ročně, často, zřídka. THL by bylo přesnější u trvání, ale
--              tyhle odpovídají na kdy, ne jak dlouho.
--   EXT    26  velmi, příliš, skoro, zcela — do jaké míry.
--   LOC    19  a DIR3 19, DIR1 6, DIR2 3 — kde, kam, odkud, kudy, včetně vztažných kde/kam/odkud/kudy.
--   MOD    16  asi, prý, patrně, údajně — jistota mluvčího. Jsou to slova, která jsou zároveň částice,
--              a funktor mají po významu, ne po slovním druhu: kdo je zadá jako částici, dostane totéž.
--   RHEM   14  jen, pouze, také, zejména — ukazují na jádro výpovědi.
--   ATT     4  bohužel, naštěstí, jaksi, vlastně. PREC 1: naopak. ACMP 1: spolu. CAUS 1: proč.
--
-- WHAT IS DELIBERATELY LEFT OUT, and this one matters
--
--   Eight words from adverbs.json have no entry here: blízko, dokonce, jak, naproti, sotva, tak,
--   uvnitř a vedle. Každé z nich je zároveň předložka nebo spojka, a heslo ve slovníku by tu roli
--   přebilo — CzechLexiconEnricher dělá `word.WordCategory ??= entry.Category`, takže rozpoznání
--   předložky se na slovo s heslem už nedostane. Z 'vedle knihy' by se stalo příslovce a předložka by
--   přestala řídit genitiv. Poznají se dál jako předložka a spojka, tedy tak jako dosud.
--
--   Komparativy se sem znovu nekopírují. Zůstávají v adverbs.json, kde jsou pro všech 291.
--
-- source je 'IJP' u lemmatu, is_verified 0: funktor z IJP není a proti korpusu to nikdo neověřoval.
-- Sporných rozhodnutí je tu víc než u dvaceti jedné — 'prakticky' a 'teoreticky' jsou EXT po tom, jak
-- se používají ('prakticky hotovo'), ne MANN po tom, jak vypadají; 'konečně' a 'nakonec' jsou TWHEN,
-- i když v 'Konečně!' jsou postoj. Opravit jedno slovo je teď edit v administraci.

INSERT INTO lemma_entry (
    lemma_entry_id, lemma, lemma_key, homonym_index, category,
    is_indeclinable, inherent_functor, source, is_verified, note) VALUES
    -- LOC (19)
    (303, 'daleko', 'daleko', 1, 'Adverb', 1, 'LOC', 'IJP', 0, NULL),
    (304, 'hluboko', 'hluboko', 1, 'Adverb', 1, 'LOC', 'IJP', 0, NULL),
    (305, 'jinde', 'jinde', 1, 'Adverb', 1, 'LOC', 'IJP', 0, NULL),
    (306, 'kde', 'kde', 1, 'Adverb', 1, 'LOC', 'IJP', 0, NULL),
    (307, 'kdesi', 'kdesi', 1, 'Adverb', 1, 'LOC', 'IJP', 0, NULL),
    (308, 'leckde', 'leckde', 1, 'Adverb', 1, 'LOC', 'IJP', 0, NULL),
    (309, 'nablízku', 'nablízku', 1, 'Adverb', 1, 'LOC', 'IJP', 0, NULL),
    (310, 'nikde', 'nikde', 1, 'Adverb', 1, 'LOC', 'IJP', 0, NULL),
    (311, 'nízko', 'nízko', 1, 'Adverb', 1, 'LOC', 'IJP', 0, NULL),
    (312, 'někde', 'někde', 1, 'Adverb', 1, 'LOC', 'IJP', 0, NULL),
    (313, 'onde', 'onde', 1, 'Adverb', 1, 'LOC', 'IJP', 0, NULL),
    (314, 'opodál', 'opodál', 1, 'Adverb', 1, 'LOC', 'IJP', 0, NULL),
    (315, 'stranou', 'stranou', 1, 'Adverb', 1, 'LOC', 'IJP', 0, NULL),
    (316, 'tu', 'tu', 1, 'Adverb', 1, 'LOC', 'IJP', 0, NULL),
    (317, 'vpředu', 'vpředu', 1, 'Adverb', 1, 'LOC', 'IJP', 0, NULL),
    (318, 'vysoko', 'vysoko', 1, 'Adverb', 1, 'LOC', 'IJP', 0, NULL),
    (319, 'vzadu', 'vzadu', 1, 'Adverb', 1, 'LOC', 'IJP', 0, NULL),
    (320, 'všude', 'všude', 1, 'Adverb', 1, 'LOC', 'IJP', 0, NULL),
    (321, 'široko', 'široko', 1, 'Adverb', 1, 'LOC', 'IJP', 0, NULL),

    -- DIR1 (6)
    (322, 'odjinud', 'odjinud', 1, 'Adverb', 1, 'DIR1', 'IJP', 0, NULL),
    (323, 'odkud', 'odkud', 1, 'Adverb', 1, 'DIR1', 'IJP', 0, NULL),
    (324, 'odnikud', 'odnikud', 1, 'Adverb', 1, 'DIR1', 'IJP', 0, NULL),
    (325, 'odněkud', 'odněkud', 1, 'Adverb', 1, 'DIR1', 'IJP', 0, NULL),
    (326, 'odsud', 'odsud', 1, 'Adverb', 1, 'DIR1', 'IJP', 0, NULL),
    (327, 'odtud', 'odtud', 1, 'Adverb', 1, 'DIR1', 'IJP', 0, NULL),

    -- DIR2 (3)
    (328, 'kudy', 'kudy', 1, 'Adverb', 1, 'DIR2', 'IJP', 0, NULL),
    (329, 'nikudy', 'nikudy', 1, 'Adverb', 1, 'DIR2', 'IJP', 0, NULL),
    (330, 'tudy', 'tudy', 1, 'Adverb', 1, 'DIR2', 'IJP', 0, NULL),

    -- DIR3 (19)
    (331, 'doleva', 'doleva', 1, 'Adverb', 1, 'DIR3', 'IJP', 0, NULL),
    (332, 'dolů', 'dolů', 1, 'Adverb', 1, 'DIR3', 'IJP', 0, NULL),
    (333, 'domů', 'domů', 1, 'Adverb', 1, 'DIR3', 'IJP', 0, NULL),
    (334, 'doprava', 'doprava', 1, 'Adverb', 1, 'DIR3', 'IJP', 0, NULL),
    (335, 'dopředu', 'dopředu', 1, 'Adverb', 1, 'DIR3', 'IJP', 0, NULL),
    (336, 'dovnitř', 'dovnitř', 1, 'Adverb', 1, 'DIR3', 'IJP', 0, NULL),
    (337, 'dozadu', 'dozadu', 1, 'Adverb', 1, 'DIR3', 'IJP', 0, NULL),
    (338, 'jinam', 'jinam', 1, 'Adverb', 1, 'DIR3', 'IJP', 0, NULL),
    (339, 'kam', 'kam', 1, 'Adverb', 1, 'DIR3', 'IJP', 0, NULL),
    (340, 'kamsi', 'kamsi', 1, 'Adverb', 1, 'DIR3', 'IJP', 0, NULL),
    (341, 'nahoru', 'nahoru', 1, 'Adverb', 1, 'DIR3', 'IJP', 0, NULL),
    (342, 'nikam', 'nikam', 1, 'Adverb', 1, 'DIR3', 'IJP', 0, NULL),
    (343, 'někam', 'někam', 1, 'Adverb', 1, 'DIR3', 'IJP', 0, NULL),
    (344, 'pryč', 'pryč', 1, 'Adverb', 1, 'DIR3', 'IJP', 0, NULL),
    (345, 'sem', 'sem', 1, 'Adverb', 1, 'DIR3', 'IJP', 0, NULL),
    (346, 'tam', 'tam', 1, 'Adverb', 1, 'DIR3', 'IJP', 0, NULL),
    (347, 'ven', 'ven', 1, 'Adverb', 1, 'DIR3', 'IJP', 0, NULL),
    (348, 'zpátky', 'zpátky', 1, 'Adverb', 1, 'DIR3', 'IJP', 0, NULL),
    (349, 'zpět', 'zpět', 1, 'Adverb', 1, 'DIR3', 'IJP', 0, NULL),

    -- TWHEN (61)
    (350, 'brzo', 'brzo', 1, 'Adverb', 1, 'TWHEN', 'IJP', 0, NULL),
    (351, 'denně', 'denně', 1, 'Adverb', 1, 'TWHEN', 'IJP', 0, NULL),
    (352, 'dneska', 'dneska', 1, 'Adverb', 1, 'TWHEN', 'IJP', 0, NULL),
    (353, 'dokdy', 'dokdy', 1, 'Adverb', 1, 'TWHEN', 'IJP', 0, NULL),
    (354, 'dopoledne', 'dopoledne', 1, 'Adverb', 1, 'TWHEN', 'IJP', 0, NULL),
    (355, 'doposud', 'doposud', 1, 'Adverb', 1, 'TWHEN', 'IJP', 0, NULL),
    (356, 'dosud', 'dosud', 1, 'Adverb', 1, 'TWHEN', 'IJP', 0, NULL),
    (357, 'hned', 'hned', 1, 'Adverb', 1, 'TWHEN', 'IJP', 0, NULL),
    (358, 'ihned', 'ihned', 1, 'Adverb', 1, 'TWHEN', 'IJP', 0, NULL),
    (359, 'ještě', 'ještě', 1, 'Adverb', 1, 'TWHEN', 'IJP', 0, NULL),
    (360, 'již', 'již', 1, 'Adverb', 1, 'TWHEN', 'IJP', 0, NULL),
    (361, 'kdy', 'kdy', 1, 'Adverb', 1, 'TWHEN', 'IJP', 0, NULL),
    (362, 'kdysi', 'kdysi', 1, 'Adverb', 1, 'TWHEN', 'IJP', 0, NULL),
    (363, 'konečně', 'konečně', 1, 'Adverb', 1, 'TWHEN', 'IJP', 0, NULL),
    (364, 'leckdy', 'leckdy', 1, 'Adverb', 1, 'TWHEN', 'IJP', 0, NULL),
    (365, 'letos', 'letos', 1, 'Adverb', 1, 'TWHEN', 'IJP', 0, NULL),
    (366, 'loni', 'loni', 1, 'Adverb', 1, 'TWHEN', 'IJP', 0, NULL),
    (367, 'mezitím', 'mezitím', 1, 'Adverb', 1, 'TWHEN', 'IJP', 0, NULL),
    (368, 'málokdy', 'málokdy', 1, 'Adverb', 1, 'TWHEN', 'IJP', 0, NULL),
    (369, 'měsíčně', 'měsíčně', 1, 'Adverb', 1, 'TWHEN', 'IJP', 0, NULL),
    (370, 'najednou', 'najednou', 1, 'Adverb', 1, 'TWHEN', 'IJP', 0, NULL),
    (371, 'nakonec', 'nakonec', 1, 'Adverb', 1, 'TWHEN', 'IJP', 0, NULL),
    (372, 'napřesrok', 'napřesrok', 1, 'Adverb', 1, 'TWHEN', 'IJP', 0, NULL),
    (373, 'navždy', 'navždy', 1, 'Adverb', 1, 'TWHEN', 'IJP', 0, NULL),
    (374, 'nedávno', 'nedávno', 1, 'Adverb', 1, 'TWHEN', 'IJP', 0, NULL),
    (375, 'nejprve', 'nejprve', 1, 'Adverb', 1, 'TWHEN', 'IJP', 0, NULL),
    (376, 'nikdy', 'nikdy', 1, 'Adverb', 1, 'TWHEN', 'IJP', 0, NULL),
    (377, 'nyní', 'nyní', 1, 'Adverb', 1, 'TWHEN', 'IJP', 0, NULL),
    (378, 'náhle', 'náhle', 1, 'Adverb', 1, 'TWHEN', 'IJP', 0, NULL),
    (379, 'někdy', 'někdy', 1, 'Adverb', 1, 'TWHEN', 'IJP', 0, NULL),
    (380, 'občas', 'občas', 1, 'Adverb', 1, 'TWHEN', 'IJP', 0, NULL),
    (381, 'odkdy', 'odkdy', 1, 'Adverb', 1, 'TWHEN', 'IJP', 0, NULL),
    (382, 'odpoledne', 'odpoledne', 1, 'Adverb', 1, 'TWHEN', 'IJP', 0, NULL),
    (383, 'okamžitě', 'okamžitě', 1, 'Adverb', 1, 'TWHEN', 'IJP', 0, NULL),
    (384, 'opět', 'opět', 1, 'Adverb', 1, 'TWHEN', 'IJP', 0, NULL),
    (385, 'pak', 'pak', 1, 'Adverb', 1, 'TWHEN', 'IJP', 0, NULL),
    (386, 'postupně', 'postupně', 1, 'Adverb', 1, 'TWHEN', 'IJP', 0, NULL),
    (387, 'potom', 'potom', 1, 'Adverb', 1, 'TWHEN', 'IJP', 0, NULL),
    (388, 'poté', 'poté', 1, 'Adverb', 1, 'TWHEN', 'IJP', 0, NULL),
    (389, 'pozítří', 'pozítří', 1, 'Adverb', 1, 'TWHEN', 'IJP', 0, NULL),
    (390, 'pořád', 'pořád', 1, 'Adverb', 1, 'TWHEN', 'IJP', 0, NULL),
    (391, 'pravidelně', 'pravidelně', 1, 'Adverb', 1, 'TWHEN', 'IJP', 0, NULL),
    (392, 'průběžně', 'průběžně', 1, 'Adverb', 1, 'TWHEN', 'IJP', 0, NULL),
    (393, 'předevčírem', 'předevčírem', 1, 'Adverb', 1, 'TWHEN', 'IJP', 0, NULL),
    (394, 'předtím', 'předtím', 1, 'Adverb', 1, 'TWHEN', 'IJP', 0, NULL),
    (395, 'ročně', 'ročně', 1, 'Adverb', 1, 'TWHEN', 'IJP', 0, NULL),
    (396, 'ráno', 'ráno', 1, 'Adverb', 1, 'TWHEN', 'IJP', 0, NULL),
    (397, 'stále', 'stále', 1, 'Adverb', 1, 'TWHEN', 'IJP', 0, NULL),
    (398, 'tehdy', 'tehdy', 1, 'Adverb', 1, 'TWHEN', 'IJP', 0, NULL),
    (399, 'tenkrát', 'tenkrát', 1, 'Adverb', 1, 'TWHEN', 'IJP', 0, NULL),
    (400, 'teď', 'teď', 1, 'Adverb', 1, 'TWHEN', 'IJP', 0, NULL),
    (401, 'týdně', 'týdně', 1, 'Adverb', 1, 'TWHEN', 'IJP', 0, NULL),
    (402, 'už', 'už', 1, 'Adverb', 1, 'TWHEN', 'IJP', 0, NULL),
    (403, 'večer', 'večer', 1, 'Adverb', 1, 'TWHEN', 'IJP', 0, NULL),
    (404, 'vždy', 'vždy', 1, 'Adverb', 1, 'TWHEN', 'IJP', 0, NULL),
    (405, 'vždycky', 'vždycky', 1, 'Adverb', 1, 'TWHEN', 'IJP', 0, NULL),
    (406, 'zase', 'zase', 1, 'Adverb', 1, 'TWHEN', 'IJP', 0, NULL),
    (407, 'zatím', 'zatím', 1, 'Adverb', 1, 'TWHEN', 'IJP', 0, NULL),
    (408, 'znovu', 'znovu', 1, 'Adverb', 1, 'TWHEN', 'IJP', 0, NULL),
    (409, 'zřídka', 'zřídka', 1, 'Adverb', 1, 'TWHEN', 'IJP', 0, NULL),
    (410, 'často', 'často', 1, 'Adverb', 1, 'TWHEN', 'IJP', 0, NULL),

    -- MANN (91)
    (411, 'bezpečně', 'bezpečně', 1, 'Adverb', 1, 'MANN', 'IJP', 0, NULL),
    (412, 'bohatě', 'bohatě', 1, 'Adverb', 1, 'MANN', 'IJP', 0, NULL),
    (413, 'chladně', 'chladně', 1, 'Adverb', 1, 'MANN', 'IJP', 0, NULL),
    (414, 'chytře', 'chytře', 1, 'Adverb', 1, 'MANN', 'IJP', 0, NULL),
    (415, 'divně', 'divně', 1, 'Adverb', 1, 'MANN', 'IJP', 0, NULL),
    (416, 'dlouze', 'dlouze', 1, 'Adverb', 1, 'MANN', 'IJP', 0, NULL),
    (417, 'hezky', 'hezky', 1, 'Adverb', 1, 'MANN', 'IJP', 0, NULL),
    (418, 'hladce', 'hladce', 1, 'Adverb', 1, 'MANN', 'IJP', 0, NULL),
    (419, 'hloupě', 'hloupě', 1, 'Adverb', 1, 'MANN', 'IJP', 0, NULL),
    (420, 'hluboce', 'hluboce', 1, 'Adverb', 1, 'MANN', 'IJP', 0, NULL),
    (421, 'hrubě', 'hrubě', 1, 'Adverb', 1, 'MANN', 'IJP', 0, NULL),
    (422, 'hustě', 'hustě', 1, 'Adverb', 1, 'MANN', 'IJP', 0, NULL),
    (423, 'jasně', 'jasně', 1, 'Adverb', 1, 'MANN', 'IJP', 0, NULL),
    (424, 'jednoduše', 'jednoduše', 1, 'Adverb', 1, 'MANN', 'IJP', 0, NULL),
    (425, 'jinak', 'jinak', 1, 'Adverb', 1, 'MANN', 'IJP', 0, NULL),
    (426, 'klidně', 'klidně', 1, 'Adverb', 1, 'MANN', 'IJP', 0, NULL),
    (427, 'krásně', 'krásně', 1, 'Adverb', 1, 'MANN', 'IJP', 0, NULL),
    (428, 'kvalitně', 'kvalitně', 1, 'Adverb', 1, 'MANN', 'IJP', 0, NULL),
    (429, 'laskavě', 'laskavě', 1, 'Adverb', 1, 'MANN', 'IJP', 0, NULL),
    (430, 'lehce', 'lehce', 1, 'Adverb', 1, 'MANN', 'IJP', 0, NULL),
    (431, 'lehko', 'lehko', 1, 'Adverb', 1, 'MANN', 'IJP', 0, NULL),
    (432, 'levně', 'levně', 1, 'Adverb', 1, 'MANN', 'IJP', 0, NULL),
    (433, 'mile', 'mile', 1, 'Adverb', 1, 'MANN', 'IJP', 0, NULL),
    (434, 'moderně', 'moderně', 1, 'Adverb', 1, 'MANN', 'IJP', 0, NULL),
    (435, 'nahlas', 'nahlas', 1, 'Adverb', 1, 'MANN', 'IJP', 0, NULL),
    (436, 'naschvál', 'naschvál', 1, 'Adverb', 1, 'MANN', 'IJP', 0, NULL),
    (437, 'nebezpečně', 'nebezpečně', 1, 'Adverb', 1, 'MANN', 'IJP', 0, NULL),
    (438, 'nijak', 'nijak', 1, 'Adverb', 1, 'MANN', 'IJP', 0, NULL),
    (439, 'normálně', 'normálně', 1, 'Adverb', 1, 'MANN', 'IJP', 0, NULL),
    (440, 'náhodou', 'náhodou', 1, 'Adverb', 1, 'MANN', 'IJP', 0, NULL),
    (441, 'nějak', 'nějak', 1, 'Adverb', 1, 'MANN', 'IJP', 0, NULL),
    (442, 'obtížně', 'obtížně', 1, 'Adverb', 1, 'MANN', 'IJP', 0, NULL),
    (443, 'odborně', 'odborně', 1, 'Adverb', 1, 'MANN', 'IJP', 0, NULL),
    (444, 'opatrně', 'opatrně', 1, 'Adverb', 1, 'MANN', 'IJP', 0, NULL),
    (445, 'opačně', 'opačně', 1, 'Adverb', 1, 'MANN', 'IJP', 0, NULL),
    (446, 'ostře', 'ostře', 1, 'Adverb', 1, 'MANN', 'IJP', 0, NULL),
    (447, 'pevně', 'pevně', 1, 'Adverb', 1, 'MANN', 'IJP', 0, NULL),
    (448, 'pečlivě', 'pečlivě', 1, 'Adverb', 1, 'MANN', 'IJP', 0, NULL),
    (449, 'pilně', 'pilně', 1, 'Adverb', 1, 'MANN', 'IJP', 0, NULL),
    (450, 'podrobně', 'podrobně', 1, 'Adverb', 1, 'MANN', 'IJP', 0, NULL),
    (451, 'potichu', 'potichu', 1, 'Adverb', 1, 'MANN', 'IJP', 0, NULL),
    (452, 'prudce', 'prudce', 1, 'Adverb', 1, 'MANN', 'IJP', 0, NULL),
    (453, 'přesně', 'přesně', 1, 'Adverb', 1, 'MANN', 'IJP', 0, NULL),
    (454, 'příjemně', 'příjemně', 1, 'Adverb', 1, 'MANN', 'IJP', 0, NULL),
    (455, 'rovně', 'rovně', 1, 'Adverb', 1, 'MANN', 'IJP', 0, NULL),
    (456, 'rád', 'rád', 1, 'Adverb', 1, 'MANN', 'IJP', 0, NULL),
    (457, 'silně', 'silně', 1, 'Adverb', 1, 'MANN', 'IJP', 0, NULL),
    (458, 'slabě', 'slabě', 1, 'Adverb', 1, 'MANN', 'IJP', 0, NULL),
    (459, 'složitě', 'složitě', 1, 'Adverb', 1, 'MANN', 'IJP', 0, NULL),
    (460, 'smutně', 'smutně', 1, 'Adverb', 1, 'MANN', 'IJP', 0, NULL),
    (461, 'směle', 'směle', 1, 'Adverb', 1, 'MANN', 'IJP', 0, NULL),
    (462, 'snadno', 'snadno', 1, 'Adverb', 1, 'MANN', 'IJP', 0, NULL),
    (463, 'spolehlivě', 'spolehlivě', 1, 'Adverb', 1, 'MANN', 'IJP', 0, NULL),
    (464, 'správně', 'správně', 1, 'Adverb', 1, 'MANN', 'IJP', 0, NULL),
    (465, 'srozumitelně', 'srozumitelně', 1, 'Adverb', 1, 'MANN', 'IJP', 0, NULL),
    (466, 'statečně', 'statečně', 1, 'Adverb', 1, 'MANN', 'IJP', 0, NULL),
    (467, 'stručně', 'stručně', 1, 'Adverb', 1, 'MANN', 'IJP', 0, NULL),
    (468, 'svobodně', 'svobodně', 1, 'Adverb', 1, 'MANN', 'IJP', 0, NULL),
    (469, 'světle', 'světle', 1, 'Adverb', 1, 'MANN', 'IJP', 0, NULL),
    (470, 'takto', 'takto', 1, 'Adverb', 1, 'MANN', 'IJP', 0, NULL),
    (471, 'temně', 'temně', 1, 'Adverb', 1, 'MANN', 'IJP', 0, NULL),
    (472, 'teple', 'teple', 1, 'Adverb', 1, 'MANN', 'IJP', 0, NULL),
    (473, 'teplo', 'teplo', 1, 'Adverb', 1, 'MANN', 'IJP', 0, NULL),
    (474, 'tlustě', 'tlustě', 1, 'Adverb', 1, 'MANN', 'IJP', 0, NULL),
    (475, 'trpělivě', 'trpělivě', 1, 'Adverb', 1, 'MANN', 'IJP', 0, NULL),
    (476, 'tvrdě', 'tvrdě', 1, 'Adverb', 1, 'MANN', 'IJP', 0, NULL),
    (477, 'těsně', 'těsně', 1, 'Adverb', 1, 'MANN', 'IJP', 0, NULL),
    (478, 'těžce', 'těžce', 1, 'Adverb', 1, 'MANN', 'IJP', 0, NULL),
    (479, 'těžko', 'těžko', 1, 'Adverb', 1, 'MANN', 'IJP', 0, NULL),
    (480, 'upřímně', 'upřímně', 1, 'Adverb', 1, 'MANN', 'IJP', 0, NULL),
    (481, 'vesele', 'vesele', 1, 'Adverb', 1, 'MANN', 'IJP', 0, NULL),
    (482, 'vhodně', 'vhodně', 1, 'Adverb', 1, 'MANN', 'IJP', 0, NULL),
    (483, 'volně', 'volně', 1, 'Adverb', 1, 'MANN', 'IJP', 0, NULL),
    (484, 'vysoce', 'vysoce', 1, 'Adverb', 1, 'MANN', 'IJP', 0, NULL),
    (485, 'vážně', 'vážně', 1, 'Adverb', 1, 'MANN', 'IJP', 0, NULL),
    (486, 'výrazně', 'výrazně', 1, 'Adverb', 1, 'MANN', 'IJP', 0, NULL),
    (487, 'všelijak', 'všelijak', 1, 'Adverb', 1, 'MANN', 'IJP', 0, NULL),
    (488, 'zajímavě', 'zajímavě', 1, 'Adverb', 1, 'MANN', 'IJP', 0, NULL),
    (489, 'zdravě', 'zdravě', 1, 'Adverb', 1, 'MANN', 'IJP', 0, NULL),
    (490, 'zdvořile', 'zdvořile', 1, 'Adverb', 1, 'MANN', 'IJP', 0, NULL),
    (491, 'zle', 'zle', 1, 'Adverb', 1, 'MANN', 'IJP', 0, NULL),
    (492, 'zpaměti', 'zpaměti', 1, 'Adverb', 1, 'MANN', 'IJP', 0, NULL),
    (493, 'zvláštně', 'zvláštně', 1, 'Adverb', 1, 'MANN', 'IJP', 0, NULL),
    (494, 'úzce', 'úzce', 1, 'Adverb', 1, 'MANN', 'IJP', 0, NULL),
    (495, 'úzko', 'úzko', 1, 'Adverb', 1, 'MANN', 'IJP', 0, NULL),
    (496, 'účinně', 'účinně', 1, 'Adverb', 1, 'MANN', 'IJP', 0, NULL),
    (497, 'čistě', 'čistě', 1, 'Adverb', 1, 'MANN', 'IJP', 0, NULL),
    (498, 'široce', 'široce', 1, 'Adverb', 1, 'MANN', 'IJP', 0, NULL),
    (499, 'štědře', 'štědře', 1, 'Adverb', 1, 'MANN', 'IJP', 0, NULL),
    (500, 'šťastně', 'šťastně', 1, 'Adverb', 1, 'MANN', 'IJP', 0, NULL),
    (501, 'živě', 'živě', 1, 'Adverb', 1, 'MANN', 'IJP', 0, NULL),

    -- ACMP (1)
    (502, 'spolu', 'spolu', 1, 'Adverb', 1, 'ACMP', 'IJP', 0, NULL),

    -- CAUS (1)
    (503, 'proč', 'proč', 1, 'Adverb', 1, 'CAUS', 'IJP', 0, NULL),

    -- EXT (26)
    (504, 'celkem', 'celkem', 1, 'Adverb', 1, 'EXT', 'IJP', 0, NULL),
    (505, 'docela', 'docela', 1, 'Adverb', 1, 'EXT', 'IJP', 0, NULL),
    (506, 'dost', 'dost', 1, 'Adverb', 1, 'EXT', 'IJP', 0, NULL),
    (507, 'hodně', 'hodně', 1, 'Adverb', 1, 'EXT', 'IJP', 0, NULL),
    (508, 'maximálně', 'maximálně', 1, 'Adverb', 1, 'EXT', 'IJP', 0, NULL),
    (509, 'minimálně', 'minimálně', 1, 'Adverb', 1, 'EXT', 'IJP', 0, NULL),
    (510, 'moc', 'moc', 1, 'Adverb', 1, 'EXT', 'IJP', 0, NULL),
    (511, 'málem', 'málem', 1, 'Adverb', 1, 'EXT', 'IJP', 0, NULL),
    (512, 'málo', 'málo', 1, 'Adverb', 1, 'EXT', 'IJP', 0, NULL),
    (513, 'nanejvýš', 'nanejvýš', 1, 'Adverb', 1, 'EXT', 'IJP', 0, NULL),
    (514, 'poměrně', 'poměrně', 1, 'Adverb', 1, 'EXT', 'IJP', 0, NULL),
    (515, 'prakticky', 'prakticky', 1, 'Adverb', 1, 'EXT', 'IJP', 0, NULL),
    (516, 'přibližně', 'přibližně', 1, 'Adverb', 1, 'EXT', 'IJP', 0, NULL),
    (517, 'příliš', 'příliš', 1, 'Adverb', 1, 'EXT', 'IJP', 0, NULL),
    (518, 'relativně', 'relativně', 1, 'Adverb', 1, 'EXT', 'IJP', 0, NULL),
    (519, 'skoro', 'skoro', 1, 'Adverb', 1, 'EXT', 'IJP', 0, NULL),
    (520, 'stěží', 'stěží', 1, 'Adverb', 1, 'EXT', 'IJP', 0, NULL),
    (521, 'teoreticky', 'teoreticky', 1, 'Adverb', 1, 'EXT', 'IJP', 0, NULL),
    (522, 'trochu', 'trochu', 1, 'Adverb', 1, 'EXT', 'IJP', 0, NULL),
    (523, 'téměř', 'téměř', 1, 'Adverb', 1, 'EXT', 'IJP', 0, NULL),
    (524, 'velmi', 'velmi', 1, 'Adverb', 1, 'EXT', 'IJP', 0, NULL),
    (525, 'vesměs', 'vesměs', 1, 'Adverb', 1, 'EXT', 'IJP', 0, NULL),
    (526, 'zcela', 'zcela', 1, 'Adverb', 1, 'EXT', 'IJP', 0, NULL),
    (527, 'zhruba', 'zhruba', 1, 'Adverb', 1, 'EXT', 'IJP', 0, NULL),
    (528, 'zčásti', 'zčásti', 1, 'Adverb', 1, 'EXT', 'IJP', 0, NULL),
    (529, 'úplně', 'úplně', 1, 'Adverb', 1, 'EXT', 'IJP', 0, NULL),

    -- MOD (16)
    (530, 'asi', 'asi', 1, 'Adverb', 1, 'MOD', 'IJP', 0, NULL),
    (531, 'jistě', 'jistě', 1, 'Adverb', 1, 'MOD', 'IJP', 0, NULL),
    (532, 'možná', 'možná', 1, 'Adverb', 1, 'MOD', 'IJP', 0, NULL),
    (533, 'nepochybně', 'nepochybně', 1, 'Adverb', 1, 'MOD', 'IJP', 0, NULL),
    (534, 'opravdu', 'opravdu', 1, 'Adverb', 1, 'MOD', 'IJP', 0, NULL),
    (535, 'ovšem', 'ovšem', 1, 'Adverb', 1, 'MOD', 'IJP', 0, NULL),
    (536, 'patrně', 'patrně', 1, 'Adverb', 1, 'MOD', 'IJP', 0, NULL),
    (537, 'prý', 'prý', 1, 'Adverb', 1, 'MOD', 'IJP', 0, NULL),
    (538, 'rozhodně', 'rozhodně', 1, 'Adverb', 1, 'MOD', 'IJP', 0, NULL),
    (539, 'samozřejmě', 'samozřejmě', 1, 'Adverb', 1, 'MOD', 'IJP', 0, NULL),
    (540, 'skutečně', 'skutečně', 1, 'Adverb', 1, 'MOD', 'IJP', 0, NULL),
    (541, 'snad', 'snad', 1, 'Adverb', 1, 'MOD', 'IJP', 0, NULL),
    (542, 'určitě', 'určitě', 1, 'Adverb', 1, 'MOD', 'IJP', 0, NULL),
    (543, 'zajisté', 'zajisté', 1, 'Adverb', 1, 'MOD', 'IJP', 0, NULL),
    (544, 'zřejmě', 'zřejmě', 1, 'Adverb', 1, 'MOD', 'IJP', 0, NULL),
    (545, 'údajně', 'údajně', 1, 'Adverb', 1, 'MOD', 'IJP', 0, NULL),

    -- ATT (4)
    (546, 'bohužel', 'bohužel', 1, 'Adverb', 1, 'ATT', 'IJP', 0, NULL),
    (547, 'jaksi', 'jaksi', 1, 'Adverb', 1, 'ATT', 'IJP', 0, NULL),
    (548, 'naštěstí', 'naštěstí', 1, 'Adverb', 1, 'ATT', 'IJP', 0, NULL),
    (549, 'vlastně', 'vlastně', 1, 'Adverb', 1, 'ATT', 'IJP', 0, NULL),

    -- PREC (1)
    (550, 'naopak', 'naopak', 1, 'Adverb', 1, 'PREC', 'IJP', 0, NULL),

    -- RHEM (14)
    (551, 'alespoň', 'alespoň', 1, 'Adverb', 1, 'RHEM', 'IJP', 0, NULL),
    (552, 'aspoň', 'aspoň', 1, 'Adverb', 1, 'RHEM', 'IJP', 0, NULL),
    (553, 'hlavně', 'hlavně', 1, 'Adverb', 1, 'RHEM', 'IJP', 0, NULL),
    (554, 'jen', 'jen', 1, 'Adverb', 1, 'RHEM', 'IJP', 0, NULL),
    (555, 'jenom', 'jenom', 1, 'Adverb', 1, 'RHEM', 'IJP', 0, NULL),
    (556, 'obzvláště', 'obzvláště', 1, 'Adverb', 1, 'RHEM', 'IJP', 0, NULL),
    (557, 'obzvlášť', 'obzvlášť', 1, 'Adverb', 1, 'RHEM', 'IJP', 0, NULL),
    (558, 'pouze', 'pouze', 1, 'Adverb', 1, 'RHEM', 'IJP', 0, NULL),
    (559, 'především', 'především', 1, 'Adverb', 1, 'RHEM', 'IJP', 0, NULL),
    (560, 'přinejmenším', 'přinejmenším', 1, 'Adverb', 1, 'RHEM', 'IJP', 0, NULL),
    (561, 'taky', 'taky', 1, 'Adverb', 1, 'RHEM', 'IJP', 0, NULL),
    (562, 'také', 'také', 1, 'Adverb', 1, 'RHEM', 'IJP', 0, NULL),
    (563, 'zejména', 'zejména', 1, 'Adverb', 1, 'RHEM', 'IJP', 0, NULL),
    (564, 'zvlášť', 'zvlášť', 1, 'Adverb', 1, 'RHEM', 'IJP', 0, NULL);
