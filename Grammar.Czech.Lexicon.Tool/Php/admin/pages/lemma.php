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
        admin_enum('category', 'category') ?? 'Noun',
        admin_enum('gender', 'gender'),
        admin_text('pattern'),
        admin_flag('is_animate'),
        admin_flag('has_mobile_e'),
        admin_flag('has_genitive_plural_shortening'),
        admin_flag('has_epenthesis_in_genitive_plural'),
        admin_flag('is_indeclinable'),
        admin_flag('is_plural_only'),
        admin_flag('is_countable'),
        admin_flag('prefers_short_form'),
        admin_enum('verb_class', 'verb_class'),
        admin_enum('aspect', 'aspect'),
        admin_text('aspect_counterpart'),
        admin_enum('reflexive_type', 'reflexive_type') ?? 'None',
        admin_text('base_verb_lemma'),
        $lexemeId,
        admin_text('source'),
        admin_flag('is_verified') === 1 ? 1 : 0,
        admin_text('note'),
    ];

    $columns = [
        'lemma', 'lemma_key', 'homonym_index', 'category', 'gender', 'pattern', 'is_animate',
        'has_mobile_e', 'has_genitive_plural_shortening', 'has_epenthesis_in_genitive_plural',
        'is_indeclinable', 'is_plural_only', 'is_countable', 'prefers_short_form', 'verb_class',
        'aspect', 'aspect_counterpart', 'reflexive_type', 'base_verb_lemma', 'lexeme_id', 'source',
        'is_verified', 'note',
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
            <input type="text" id="pattern" name="pattern" value="<?= h((string) $value('pattern')) ?>" class="mono">
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
            <?= admin_select('verb_class', 'verb_class', $value('verb_class')) ?>
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
<form method="post" class="card danger" onsubmit="return confirm('Opravdu smazat heslo <?= h((string) $value('lemma')) ?>?');">
    <input type="hidden" name="csrf" value="<?= h(admin_csrf_token()) ?>">
    <input type="hidden" name="action" value="delete">
    <h2>Smazat heslo</h2>
    <p>Lexém a jeho rámce zůstanou — patří i druhému členu vidové dvojice.</p>
    <button type="submit" class="del">Smazat</button>
</form>
<?php endif; ?>
