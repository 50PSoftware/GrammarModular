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
        'aspect', 'aspect_counterpart', 'reflexive_type', 'base_verb_lemma', 'stem', 'present_stem',
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

    <h2>Hláskové a tvarové příznaky</h2>
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

    <h2>Sloveso</h2>

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

    <h2>Kmeny</h2>
    <p class="hint">Prázdné je běžný stav — kmen si určí vzor. Vyplňuje se jen to, co vzor netrefí:
        říct se časuje podle 1. třídy a minulý čas přesto tvoří na <code>řek-</code>. Píše se bez
        koncovky a bez pomlčky, a vždycky celý za tohle heslo — u odvozeného slovesa tedy i s
        předponou (<code>odnes</code>, ne <code>nes</code>).</p>

    <div class="grid">
        <p class="field">
            <label for="stem">Kmen</label>
            <input type="text" id="stem" name="stem" value="<?= h((string) $value('stem')) ?>" class="mono">
            <small>Obecný kmen, ze kterého se odvozují ostatní — nes, ber.</small>
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
