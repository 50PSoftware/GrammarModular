<?php

declare(strict_types=1);

defined('LEXICON_ADMIN') || exit('Tenhle soubor se nespouští přímo.');

/**
 * Založení a úprava jednoho hesla.
 *
 * Formulář je na slovo, ne na tabulku: heslo se ukládá do lemma_entry, ale valence visí na lexému,
 * a ten se odsud dá založit nebo připojit. Vidová dvojice sdílí jeden lexém, takže dát a dávat mají
 * jeden rámec místo dvou kopií, které se rozejdou.
 */

$id = (string) ($_GET['id'] ?? 'new');
$isNew = $id === 'new';
$entry = null;

if (!$isNew) {
    $entry = admin_one('SELECT * FROM lemma_entry WHERE lemma_entry_id = ?', [(int) $id]);

    if ($entry === null) {
        admin_flash('Heslo neexistuje.', 'err');
        admin_redirect(['p' => 'list']);
    }
}

if ($_SERVER['REQUEST_METHOD'] === 'POST') {
    if (($_POST['action'] ?? '') === 'delete' && !$isNew) {
        admin_run('DELETE FROM lemma_entry WHERE lemma_entry_id = ?', [(int) $id]);
        admin_flash('Heslo „' . $entry['lemma'] . '“ smazáno.');
        admin_redirect(['p' => 'list']);
    }

    // Dubleta a způsob děje po významech visí na existujícím heslu a se samotným formulářem nemají
    // nic společného — vlastní akce, vlastní návrat. Kdyby propadly níž, přepsalo by uložení dublety
    // heslo hodnotami, které v jejím formuláři nejsou, tedy prázdnem.
    $action = (string) ($_POST['action'] ?? '');

    if (!$isNew && in_array($action, ['variant', 'variant_delete', 'sense_aktionsart'], true)) {
        switch ($action) {
            case 'variant':
                $variant = admin_text('variant_lemma');

                if ($variant === null) {
                    admin_flash('Podoba nesmí být prázdná.', 'err');
                    break;
                }

                try {
                    admin_run(
                        'INSERT INTO lemma_variant (lemma_entry_id, lemma, lemma_key, note) VALUES (?, ?, ?, ?)',
                        [(int) $id, $variant, admin_lemma_key($variant), admin_text('variant_note')]
                    );
                    admin_flash('Podoba přidána.');
                } catch (PDOException $exception) {
                    // UNIQUE na lemma_key. Buď je ta podoba dubletou jiného hesla, nebo tohohle —
                    // obojí znamená, že se pod tím klíčem už hledá něco jiného.
                    if ($exception->getCode() === '23000') {
                        admin_flash(
                            'Podoba „' . $variant . '“ už je vedená jinde. Jedna podoba, jedno heslo.',
                            'err'
                        );
                        break;
                    }

                    throw $exception;
                }
                break;

            case 'variant_delete':
                // lemma_entry_id v podmínce ze stejného důvodu jako u významů na lexému: číslo přišlo
                // z formuláře a bez něj by podvržené smazalo dubletu cizího hesla.
                admin_run(
                    'DELETE FROM lemma_variant WHERE variant_id = ? AND lemma_entry_id = ?',
                    [(int) ($_POST['variant_id'] ?? 0), (int) $id]
                );
                admin_flash('Podoba smazána.');
                break;

            case 'sense_aktionsart':
                $luId = (int) ($_POST['lu_id'] ?? 0);
                $group = admin_enum('aktionsart', 'aktionsart');

                // Prázdno neznamená „žádná skupina“, ale „tenhle význam k heslu nic nepřidává“, a to
                // se zapisuje nepřítomností řádku. Uložený NULL by vypadal stejně a znamenal jiné.
                admin_run(
                    'DELETE FROM lemma_sense WHERE lemma_entry_id = ? AND lu_id = ?',
                    [(int) $id, $luId]
                );

                if ($group !== null) {
                    admin_run(
                        'INSERT INTO lemma_sense (lemma_entry_id, lu_id, aktionsart, note) VALUES (?, ?, ?, ?)',
                        [(int) $id, $luId, $group, admin_text('sense_note')]
                    );
                }

                admin_flash('Uloženo.');
                break;
        }

        admin_redirect(['p' => 'lemma', 'id' => $id]);
    }

    $lemma = admin_text('lemma');

    if ($lemma === null) {
        admin_flash('Lemma nesmí být prázdné.', 'err');
        admin_redirect(['p' => 'lemma', 'id' => $id]);
    }

    $category = admin_enum('category', 'category') ?? 'Noun';

    // Vzor se kontroluje ještě před založením lexému níž — jinak by odmítnuté uložení nechalo
    // v tabulce prázdný lexém, na který už nic neukáže.
    $patternError = null;
    $pattern = admin_pattern('pattern', $category, $patternError);

    if ($patternError !== null) {
        admin_flash($patternError, 'err');
        admin_redirect(['p' => 'lemma', 'id' => $id]);
    }

    $verbClass = admin_enum('verb_class', 'verb_class');

    // Třída doplní vzor, když žádný není, a vyplněný nepřepíše: psát ani moci do třídy zapsat nejdou a
    // přepsat psát na trida1 by je časovalo bez alternace kmene. Stejné priority má
    // CzechVerbConjugationService. Tady, a ne jen v JavaScriptu, protože uložit se dá i bez něj.
    if ($category === 'Verb' && $pattern === null && $verbClass !== null) {
        $pattern = LEXICON_VERB_CLASSES[$verbClass]['pattern'];
    }

    // Lexém: buď existující, nebo nový, nebo žádný. Slova bez valence — většina substantiv — ho
    // nepotřebují a NULL je u nich správná hodnota, ne mezera.
    $lexemeId = admin_text('lexeme_id');

    if ($lexemeId === 'new') {
        admin_run('INSERT INTO lexeme (primary_lemma) VALUES (?)', [$lemma]);
        $lexemeId = (int) admin_db()->lastInsertId();
    } else {
        $lexemeId = $lexemeId === null ? null : (int) $lexemeId;
    }

    $values = [
        $lemma,
        admin_lemma_key($lemma),
        admin_int('homonym_index', 1),
        $category,
        admin_enum('gender', 'gender'),
        $pattern,
        admin_flag('is_animate'),
        admin_flag('has_mobile_e'),
        admin_flag('has_genitive_plural_shortening'),
        admin_flag('has_epenthesis_in_genitive_plural'),
        admin_flag('is_indeclinable'),
        admin_flag('is_plural_only'),
        admin_flag('is_countable'),
        admin_flag('prefers_short_form'),
        $verbClass,
        admin_enum('aspect', 'aspect'),
        admin_text('aspect_counterpart'),
        admin_enum('aktionsart', 'aktionsart'),
        admin_enum('reflexive_type', 'reflexive_type') ?? 'None',
        admin_text('base_verb_lemma'),
        admin_text('stem'),
        admin_text('present_stem'),
        admin_text('past_stem'),
        admin_text('future_stem'),
        admin_text('imperative_stem'),
        admin_text('passive_stem'),
        admin_text('infinitive'),
        admin_flag('forms_passive'),
        $lexemeId,
        admin_text('source'),
        admin_flag('is_verified') === 1 ? 1 : 0,
        admin_text('note'),
    ];

    $columns = [
        'lemma', 'lemma_key', 'homonym_index', 'category', 'gender', 'pattern', 'is_animate',
        'has_mobile_e', 'has_genitive_plural_shortening', 'has_epenthesis_in_genitive_plural',
        'is_indeclinable', 'is_plural_only', 'is_countable', 'prefers_short_form', 'verb_class',
        'aspect', 'aspect_counterpart', 'aktionsart', 'reflexive_type', 'base_verb_lemma', 'stem',
        'present_stem',
        'past_stem', 'future_stem', 'imperative_stem', 'passive_stem', 'infinitive', 'forms_passive',
        'lexeme_id', 'source', 'is_verified', 'note',
    ];

    try {
        if ($isNew) {
            $placeholders = implode(', ', array_fill(0, count($columns), '?'));
            admin_run(
                'INSERT INTO lemma_entry (' . implode(', ', $columns) . ") VALUES ($placeholders)",
                $values
            );
            $id = (int) admin_db()->lastInsertId();
            admin_flash('Heslo „' . $lemma . '“ založeno.');
        } else {
            $assignments = implode(' = ?, ', $columns) . ' = ?';
            $values[] = (int) $id;
            admin_run("UPDATE lemma_entry SET $assignments WHERE lemma_entry_id = ?", $values);
            admin_flash('Uloženo.');
        }
    } catch (PDOException $exception) {
        // 23000 je porušení integrity. Prakticky vždy to tady znamená UNIQUE na
        // (lemma_key, category, homonym_index) — tedy homonymum, kterému nikdo nedal pořadové číslo.
        if ($exception->getCode() === '23000') {
            admin_flash(
                'Heslo „' . $lemma . '“ s tímhle slovním druhem už existuje. Jde-li o homonymum '
                . '(stát jako budova a stát jako země), dej mu jiné pořadí homonyma.',
                'err'
            );
            admin_redirect(['p' => 'lemma', 'id' => $id]);
        }

        throw $exception;
    }

    admin_redirect(['p' => 'lemma', 'id' => $id]);
}

$value = static fn (string $column, mixed $default = null): mixed => $entry[$column] ?? $default;
$lexemes = admin_all('SELECT lexeme_id, primary_lemma FROM lexeme ORDER BY primary_lemma');

$variants = $isNew ? [] : admin_all(
    'SELECT * FROM lemma_variant WHERE lemma_entry_id = ? ORDER BY lemma_key',
    [(int) $id]
);

// Významy lexému spolu s tím, co o nich tohle heslo říká. LEFT JOIN, protože řádek je výjimka:
// naprostá většina dvojic heslo–význam žádný nemá a stránka je má stejně ukázat.
$senses = ($isNew || $value('lexeme_id') === null) ? [] : admin_all(
    'SELECT u.lu_id, u.sense_label, u.gloss, ls.aktionsart, ls.note AS sense_note
       FROM lexical_unit u
       LEFT JOIN lemma_sense ls ON ls.lu_id = u.lu_id AND ls.lemma_entry_id = ?
      WHERE u.lexeme_id = ?
      ORDER BY u.lu_id',
    [(int) $id, (int) $value('lexeme_id')]
);

// Skládací sekce. Formulář pokrývá všechny slovní druhy, takže u každého hesla jsou dvě třetiny
// políček bezpředmětné — u substantiva vid a kmeny, u slovesa pomnožnost. Sekce, ve které heslo něco
// má, se otevře sama; zbytek se dá rozbalit. Pole zůstávají ve formuláři i složená a odesílají se.
$foldColumns = [
    'flags' => [
        'has_mobile_e', 'has_genitive_plural_shortening', 'has_epenthesis_in_genitive_plural',
        'is_indeclinable', 'is_plural_only', 'is_countable', 'prefers_short_form',
    ],
    'verb' => ['verb_class', 'aspect', 'aspect_counterpart', 'aktionsart', 'base_verb_lemma'],
    'stems' => [
        'stem', 'present_stem', 'past_stem', 'future_stem', 'imperative_stem', 'passive_stem',
        'infinitive', 'forms_passive',
    ],
];

$foldFilled = array_map(
    static fn (array $columns): int => admin_filled_count($entry, $columns),
    $foldColumns
);

// reflexive_type je NOT NULL s výchozím 'None', takže by sekci Sloveso otvíral i u substantiva.
// Počítá se, jen když opravdu něco říká.
if ($value('reflexive_type', 'None') !== 'None') {
    $foldFilled['verb']++;
}

// Co po smazání zůstane rozbité. Neblokuje to — heslo založené omylem je důvod ho smazat — ale ani
// jedno není z formuláře vidět: cizí klíč vede od hesla k lexému a ne zpátky, takže databáze mlčí.
$deleteWarnings = [];

if (!$isNew) {
    $orphanLexeme = $value('lexeme_id') === null ? null : admin_one(
        'SELECT x.lexeme_id, x.primary_lemma,
                (SELECT COUNT(*) FROM lexical_unit u WHERE u.lexeme_id = x.lexeme_id) AS senses
         FROM lexeme x
         WHERE x.lexeme_id = ?
           AND NOT EXISTS (SELECT 1 FROM lemma_entry e
                           WHERE e.lexeme_id = x.lexeme_id AND e.lemma_entry_id <> ?)',
        [(int) $value('lexeme_id'), (int) $id]
    );

    if ($orphanLexeme !== null) {
        $deleteWarnings[] = [
            'text' => 'Na lexém „' . $orphanLexeme['primary_lemma'] . '“ pak neukáže žádné heslo. '
                . 'Jeho významy (' . (int) $orphanLexeme['senses'] . ') a jejich rámce zůstanou '
                . 'v databázi, ale nepůjde se k nim dostat.',
            'link' => admin_url(['p' => 'lexeme', 'id' => (int) $orphanLexeme['lexeme_id']]),
            'linkText' => 'Otevřít lexém',
        ];
    }

    // aspect_counterpart a base_verb_lemma nesou lemma, ne cizí klíč — heslo se dá smazat i zpod nich.
    // Který sloupec to je, se rozhoduje v PHP: CASE s parametry v THEN i ELSE si každý ovladač otypuje
    // po svém.
    $referrers = admin_all(
        'SELECT lemma, lemma_entry_id, aspect_counterpart, base_verb_lemma
         FROM lemma_entry
         WHERE (aspect_counterpart = ? OR base_verb_lemma = ?) AND lemma_entry_id <> ?',
        [$value('lemma'), $value('lemma'), (int) $id]
    );

    foreach ($referrers as $referrer) {
        $via = $referrer['aspect_counterpart'] === $value('lemma')
            ? 'vidový protějšek'
            : 'odvozeno ze slovesa';

        $deleteWarnings[] = [
            'text' => 'Heslo „' . $referrer['lemma'] . '“ na tohle ukazuje přes „' . $via
                . '“. Odkaz zůstane viset na slovo, které ve slovníku nebude.',
            'link' => admin_url(['p' => 'lemma', 'id' => (int) $referrer['lemma_entry_id']]),
            'linkText' => 'Otevřít heslo',
        ];
    }
}
?>

<p class="crumbs"><a href="<?= h(admin_url(['p' => 'list'])) ?>">Hesla</a> / <?= $isNew ? 'nové' : h((string) $value('lemma')) ?></p>

<form method="post" class="card">
    <input type="hidden" name="csrf" value="<?= h(admin_csrf_token()) ?>">

    <h2>Heslo</h2>

    <div class="grid">
        <p class="field">
            <label for="lemma">Lemma</label>
            <input type="text" id="lemma" name="lemma" value="<?= h((string) $value('lemma')) ?>" required autofocus>
            <small>Klíč pro vyhledávání se z něj spočítá sám.</small>
        </p>
        <p class="field">
            <label for="category">Slovní druh</label>
            <?= admin_select('category', 'category', (string) $value('category', 'Noun'), allowEmpty: false) ?>
        </p>
        <p class="field">
            <label for="homonym_index">Pořadí homonyma</label>
            <input type="number" id="homonym_index" name="homonym_index" min="1" value="<?= (int) $value('homonym_index', 1) ?>">
            <small>1, pokud lemma není homonymní.</small>
        </p>
        <p class="field">
            <label for="pattern">Vzor</label>
            <input type="text" id="pattern" name="pattern" value="<?= h((string) $value('pattern')) ?>" class="mono" list="patterns">
            <?php /* Nabídka, ne výběr: vzor závisí na slovním druhu, což je jiné pole. Co se opravdu
                     uloží, rozhoduje admin_pattern() při ukládání, ne tenhle seznam. */ ?>
            <datalist id="patterns">
                <?php foreach (LEXICON_PATTERNS as $patternCategory => $patterns): ?>
                    <?php foreach ($patterns as $pattern): ?>
                        <option value="<?= h($pattern) ?>" label="<?= h(LEXICON_ENUMS['category'][$patternCategory] ?? $patternCategory) ?>"></option>
                    <?php endforeach; ?>
                <?php endforeach; ?>
            </datalist>
            <small>Prázdné u slov, která se podle vzoru neskloňují.</small>
        </p>
        <p class="field">
            <label for="gender">Rod</label>
            <?= admin_select('gender', 'gender', $value('gender')) ?>
        </p>
        <p class="field">
            <label>Životnost</label>
            <?= admin_flag_field('is_animate', $value('is_animate') === null ? null : (int) $value('is_animate')) ?>
        </p>
    </div>

    <?= admin_fold_open('Hláskové a tvarové příznaky', $foldFilled['flags'], count($foldColumns['flags'])) ?>
    <p class="hint">Neuvedeno není totéž co „ne“ — resolvery s tím počítají jinak.</p>

    <div class="grid">
        <p class="field"><label>Pohybné -e</label>
            <?= admin_flag_field('has_mobile_e', $value('has_mobile_e') === null ? null : (int) $value('has_mobile_e')) ?></p>
        <p class="field"><label>Krácení v gen. pl.</label>
            <?= admin_flag_field('has_genitive_plural_shortening', $value('has_genitive_plural_shortening') === null ? null : (int) $value('has_genitive_plural_shortening')) ?></p>
        <p class="field"><label>Vkladné -e- v gen. pl.</label>
            <?= admin_flag_field('has_epenthesis_in_genitive_plural', $value('has_epenthesis_in_genitive_plural') === null ? null : (int) $value('has_epenthesis_in_genitive_plural')) ?></p>
        <p class="field"><label>Nesklonné</label>
            <?= admin_flag_field('is_indeclinable', $value('is_indeclinable') === null ? null : (int) $value('is_indeclinable')) ?></p>
        <p class="field"><label>Pomnožné</label>
            <?= admin_flag_field('is_plural_only', $value('is_plural_only') === null ? null : (int) $value('is_plural_only')) ?></p>
        <p class="field"><label>Počitatelné</label>
            <?= admin_flag_field('is_countable', $value('is_countable') === null ? null : (int) $value('is_countable')) ?></p>
        <p class="field"><label>Preferuje krátký tvar</label>
            <?= admin_flag_field('prefers_short_form', $value('prefers_short_form') === null ? null : (int) $value('prefers_short_form')) ?></p>
    </div>
    <?= admin_fold_close() ?>

    <?= admin_fold_open('Sloveso', $foldFilled['verb'], count($foldColumns['verb']) + 1) ?>

    <div class="grid">
        <p class="field">
            <label for="verb_class">Slovesná třída</label>
            <?php /* Vlastní select místo admin_select(): popiska nese vzory třídy, aby se dala vybrat
                     podle toho, jak sloveso zní, ne podle čísla, které si nikdo nepamatuje. */ ?>
            <select name="verb_class" id="verb_class">
                <option value="">— neuvedeno —</option>
                <?php foreach (LEXICON_VERB_CLASSES as $class => $info): ?>
                    <option value="<?= h($class) ?>" data-pattern="<?= h($info['pattern']) ?>"<?= $value('verb_class') === $class ? ' selected' : '' ?>>
                        <?= h(LEXICON_ENUMS['verb_class'][$class]) ?>
                        (<?= h($info['ending']) ?>) — <?= h(implode(', ', $info['examples'])) ?>
                    </option>
                <?php endforeach; ?>
            </select>
            <small>Vyplní vzor, když žádný není. Vyplněný nepřepíše — psát ani moci se do třídy zapsat nedají.</small>
        </p>
        <p class="field">
            <label for="aspect">Vid</label>
            <?= admin_select('aspect', 'aspect', $value('aspect')) ?>
        </p>
        <p class="field">
            <label for="aspect_counterpart">Vidový protějšek</label>
            <input type="text" id="aspect_counterpart" name="aspect_counterpart" value="<?= h((string) $value('aspect_counterpart')) ?>">
            <small>Nech prázdné, když protějšek nemá — u sloves pohybu prefixace vid netvoří.</small>
        </p>
        <p class="field wide">
            <label for="aktionsart">Způsob slovesného děje</label>
            <?= admin_select('aktionsart', 'aktionsart', $value('aktionsart')) ?>
            <small>Není to jemnější vid: většina sloves do žádné skupiny nepatří a prázdno znamená
                nezařazeno, ne „žádný“. Skupina vid určuje — (a)–(r) dokonavé, (s)–(y) nedokonavé —
                a <code>validate</code> to hlídá. Když se skupina význam od významu liší, nech tohle
                prázdné a zapiš ji níž, po významech.</small>
        </p>
        <p class="field">
            <label for="reflexive_type">Reflexivita</label>
            <?= admin_select('reflexive_type', 'reflexive_type', (string) $value('reflexive_type', 'None'), allowEmpty: false) ?>
        </p>
        <p class="field">
            <label for="base_verb_lemma">Odvozeno ze slovesa</label>
            <input type="text" id="base_verb_lemma" name="base_verb_lemma" value="<?= h((string) $value('base_verb_lemma')) ?>">
            <small>U dějových substantiv — příjezd ← přijet. Rámec pak dědí.</small>
        </p>
    </div>
    <?= admin_fold_close() ?>

    <?= admin_fold_open('Kmeny', $foldFilled['stems'], count($foldColumns['stems'])) ?>
    <p class="hint">Prázdné je běžný stav — kmen si určí vzor. Vyplňuje se jen to, co vzor netrefí:
        říct se časuje podle 1. třídy a minulý čas přesto tvoří na <code>řek-</code>. Píše se bez
        koncovky a bez pomlčky, a vždycky celý za tohle heslo — u odvozeného slovesa tedy i s
        předponou (<code>odnes</code>, ne <code>nes</code>).</p>
    <p class="hint">Obecný kmen platí pro slovesa i pro podstatná jména. Zbytek políček je slovesný;
        u podstatného jména by se nikdy nepřečetl a <code>validate</code> ho hlásí jako chybu.</p>

    <div class="grid">
        <p class="field">
            <label for="stem">Kmen</label>
            <input type="text" id="stem" name="stem" value="<?= h((string) $value('stem')) ?>" class="mono">
            <small>U sloves kmen, ze kterého se odvozují ostatní — nes, ber. U podstatných jmen kmen
                po alternaci — dům → dom, nůž → nož.</small>
        </p>
        <p class="field">
            <label for="present_stem">Kmen přítomný</label>
            <input type="text" id="present_stem" name="present_stem" value="<?= h((string) $value('present_stem')) ?>" class="mono">
            <small>Jen když se liší od obecného — moci → můž.</small>
        </p>
        <p class="field">
            <label for="past_stem">Kmen minulý</label>
            <input type="text" id="past_stem" name="past_stem" value="<?= h((string) $value('past_stem')) ?>" class="mono">
            <small>říct → řek, jíst → jed. Příčestí je pak řekl, jedl.</small>
        </p>
        <p class="field">
            <label for="future_stem">Kmen budoucí</label>
            <input type="text" id="future_stem" name="future_stem" value="<?= h((string) $value('future_stem')) ?>" class="mono">
            <small>Jen u sloves s vlastním budoucím tvarem — jít → půjd.</small>
        </p>
        <p class="field">
            <label for="imperative_stem">Kmen rozkazovací</label>
            <input type="text" id="imperative_stem" name="imperative_stem" value="<?= h((string) $value('imperative_stem')) ?>" class="mono">
            <small>být → buď, jíst → jez.</small>
        </p>
        <p class="field">
            <label for="passive_stem">Kmen trpný</label>
            <input type="text" id="passive_stem" name="passive_stem" value="<?= h((string) $value('passive_stem')) ?>" class="mono">
            <small>Základ trpného příčestí — dát → dán, vzít → vzat.</small>
        </p>
        <p class="field">
            <label for="infinitive">Infinitiv</label>
            <input type="text" id="infinitive" name="infinitive" value="<?= h((string) $value('infinitive')) ?>" class="mono">
            <small>Jen když se liší od lemmatu — říct vedle říci.</small>
        </p>
        <p class="field">
            <label>Tvoří pasivum</label>
            <?= admin_flag_field('forms_passive', $value('forms_passive') === null ? null : (int) $value('forms_passive')) ?>
            <small>„Ne“ jen u sloves bez trpného příčestí — moci ho nemá, pomoci má pomožen.</small>
        </p>
    </div>
    <?= admin_fold_close() ?>

    <h2>Valence</h2>

    <div class="grid">
        <p class="field">
            <label for="lexeme_id">Lexém</label>
            <select name="lexeme_id" id="lexeme_id">
                <option value="">— bez valence —</option>
                <option value="new">+ založit nový lexém</option>
                <?php foreach ($lexemes as $lexeme): ?>
                    <option value="<?= (int) $lexeme['lexeme_id'] ?>"<?= (int) $value('lexeme_id', 0) === (int) $lexeme['lexeme_id'] ? ' selected' : '' ?>>
                        <?= h((string) $lexeme['primary_lemma']) ?>
                    </option>
                <?php endforeach; ?>
            </select>
            <small>Vidová dvojice sdílí jeden lexém, a tím i rámce.</small>
        </p>
        <?php if ($value('lexeme_id') !== null): ?>
        <p class="field">
            <label>Rámce</label>
            <a class="btn" href="<?= h(admin_url(['p' => 'lexeme', 'id' => (int) $value('lexeme_id')])) ?>">Otevřít lexém</a>
        </p>
        <?php endif; ?>
    </div>

    <h2>Původ záznamu</h2>

    <div class="grid">
        <p class="field">
            <label for="source">Zdroj</label>
            <input type="text" id="source" name="source" value="<?= h((string) $value('source', 'IJP')) ?>">
            <small>VALLEX ani PDT-Vallex sem nepatří — jsou CC BY-NC-SA.</small>
        </p>
        <p class="field">
            <label>Ověřeno</label>
            <?= admin_flag_field('is_verified', (int) $value('is_verified', 0)) ?>
        </p>
    </div>

    <p class="field wide">
        <label for="note">Poznámka</label>
        <textarea id="note" name="note" rows="2"><?= h((string) $value('note')) ?></textarea>
    </p>

    <div class="actions">
        <button type="submit">Uložit</button>
        <a href="<?= h(admin_url(['p' => 'list'])) ?>">Zpět</a>
    </div>
</form>

<?php if (!$isNew): ?>
<section class="card">
    <h2>Další spisovná podoba</h2>
    <p class="hint">Druhý pravopis téhož hesla — <code>setmět</code> vedle <code>setmít</code>. Slovník
        ho pozná, ale negeneruje: hledání pod ním vrátí tohle heslo a ven jde lemma nahoře. Není to
        druhé heslo, protože obě podoby mají tytéž kmeny, týž vzor i tytéž rámce; a není to ani
        <code>infinitiv</code>, který drží infinitiv lišící se od lemmatu (<code>říct</code> vedle
        <code>říci</code>), ne rovnocennou dubletu.</p>

    <?php if ($variants === []): ?>
        <p class="empty">Žádná. To je běžný stav.</p>
    <?php else: ?>
        <ul class="chips">
        <?php foreach ($variants as $variant): ?>
            <li>
                <strong><?= h((string) $variant['lemma']) ?></strong>
                <?php if ($variant['note'] !== null): ?>
                    <span class="muted"><?= h((string) $variant['note']) ?></span>
                <?php endif; ?>
                <form method="post" class="inline" onsubmit="return confirm('Smazat podobu?');">
                    <input type="hidden" name="csrf" value="<?= h(admin_csrf_token()) ?>">
                    <input type="hidden" name="action" value="variant_delete">
                    <input type="hidden" name="variant_id" value="<?= (int) $variant['variant_id'] ?>">
                    <button type="submit" class="del small">Smazat</button>
                </form>
            </li>
        <?php endforeach; ?>
        </ul>
    <?php endif; ?>

    <form method="post" class="inline top">
        <input type="hidden" name="csrf" value="<?= h(admin_csrf_token()) ?>">
        <input type="hidden" name="action" value="variant">
        <label for="variant_lemma" class="sr">Podoba</label>
        <input type="text" id="variant_lemma" name="variant_lemma" placeholder="setmět">
        <label for="variant_note" class="sr">Poznámka</label>
        <input type="text" id="variant_note" name="variant_note" class="grow"
               placeholder="odkud je — třeba „IJP: lze i“">
        <button type="submit">Přidat podobu</button>
    </form>
</section>

<?php if ($senses !== []): ?>
<section class="card">
    <h2>Způsob děje po významech</h2>
    <p class="hint">Jen když se skupina význam od významu liší. <em>Mrzne</em> je stav vzduchu a
        <em>voda mrzne</em> postupná změna vody — heslo pak nahoře zůstane prázdné a odpovídá se tady.
        Prázdné pole znamená, že ten význam k heslu nic nepřidává, ne že sloveso do žádné skupiny
        nepatří; to se říká nahoře.</p>
    <p class="hint">Zapisuje se to na dvojici heslo–význam, ne na význam samotný: význam patří lexému a
        lexém je vidová dvojice, takže hodnota u významu by dopadla i na dokonavý protějšek. <em>Zmrzlo</em>
        je rezultativní v obou významech, kdežto <em>mrzne</em> stavové — jeden řádek by je nerozlišil.</p>

    <?php foreach ($senses as $sense): ?>
        <form method="post" class="inline">
            <input type="hidden" name="csrf" value="<?= h(admin_csrf_token()) ?>">
            <input type="hidden" name="action" value="sense_aktionsart">
            <input type="hidden" name="lu_id" value="<?= (int) $sense['lu_id'] ?>">
            <strong><?= h((string) ($sense['sense_label'] ?? '(bez názvu)')) ?></strong>
            <?php if ($sense['gloss'] !== null): ?>
                <span class="muted"><?= h((string) $sense['gloss']) ?></span>
            <?php endif; ?>
            <label class="sr" for="aktionsart_<?= (int) $sense['lu_id'] ?>">Způsob děje</label>
            <?= admin_select(
                'aktionsart',
                'aktionsart',
                $sense['aktionsart'] === null ? null : (string) $sense['aktionsart'],
                id: 'aktionsart_' . (int) $sense['lu_id']
            ) ?>
            <label class="sr" for="sense_note_<?= (int) $sense['lu_id'] ?>">Poznámka</label>
            <input type="text" id="sense_note_<?= (int) $sense['lu_id'] ?>" name="sense_note" class="grow"
                   value="<?= h((string) $sense['sense_note']) ?>" placeholder="čím to je">
            <button type="submit">Uložit</button>
        </form>
    <?php endforeach; ?>
</section>
<?php endif; ?>
<?php endif; ?>

<?php if (!$isNew): ?>
<?php
// I do confirmu: kdo maže, klikne na tlačítko a dialog přečte, zatímco text nad ním přeskočil.
$confirm = 'Opravdu smazat heslo ' . $value('lemma') . '?';

foreach ($deleteWarnings as $warning) {
    $confirm .= "\n\n" . $warning['text'];
}

// json_encode kvůli uvozovkám a zalomením v textu; h() proto, že výsledek jde do atributu.
$confirmLiteral = (string) json_encode($confirm, JSON_UNESCAPED_UNICODE);
?>
<form method="post" class="card danger" onsubmit="return confirm(<?= h($confirmLiteral) ?>);">
    <input type="hidden" name="csrf" value="<?= h(admin_csrf_token()) ?>">
    <input type="hidden" name="action" value="delete">
    <h2>Smazat heslo</h2>

    <?php if ($deleteWarnings === []): ?>
        <p>Nic dalšího na tohle heslo neukazuje.<?= $value('lexeme_id') === null ? '' : ' Lexém a jeho rámce zůstanou — patří i druhému členu vidové dvojice.' ?></p>
    <?php else: ?>
        <p><strong>Po smazání zůstane rozbité:</strong></p>
        <ul class="warnings">
            <?php foreach ($deleteWarnings as $warning): ?>
                <li>
                    <?= h($warning['text']) ?>
                    <a href="<?= h($warning['link']) ?>"><?= h($warning['linkText']) ?></a>
                </li>
            <?php endforeach; ?>
        </ul>
        <p class="hint">Smazat to jde i tak — jen to ve slovníku nechá data, ke kterým nevede cesta.
            Nástroj lexikonu na ně upozorní při každém <code>validate</code>.</p>
    <?php endif; ?>

    <button type="submit" class="del">Smazat</button>
</form>
<?php endif; ?>

<script>
    // Propsání třídy do vzoru. Totéž dělá PHP při ukládání; tohle je proto, aby to bylo vidět dřív.
    (function () {
        var verbClass = document.getElementById('verb_class');
        var pattern = document.getElementById('pattern');

        if (!verbClass || !pattern) {
            return;
        }

        verbClass.addEventListener('change', function () {
            var option = verbClass.options[verbClass.selectedIndex];
            var derived = option ? option.getAttribute('data-pattern') : null;

            // Pojmenovaný vzor je rozhodnutí, které třída přebít nesmí.
            if (derived && (pattern.value === '' || /^trida[1-5]$/.test(pattern.value))) {
                pattern.value = derived;
            }
        });
    })();
</script>
