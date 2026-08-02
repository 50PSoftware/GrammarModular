<?php

declare(strict_types=1);

defined('LEXICON_ADMIN') || exit('Tenhle soubor se nespouští přímo.');

/**
 * Lexém: jeho hesla, významy a rámce.
 *
 * Lexém je abstraktní slovo — vidová dvojice je jeden lexém se dvěma hesly. Význam (lexical_unit) je
 * to, čemu se ve starém JSONu říkalo frameLabel, a rámec visí na významu, jeden pro každou diatezi.
 */

$id = (int) ($_GET['id'] ?? 0);
$lexeme = admin_one('SELECT * FROM lexeme WHERE lexeme_id = ?', [$id]);

if ($lexeme === null) {
    admin_flash('Lexém neexistuje.', 'err');
    admin_redirect(['p' => 'list']);
}

if ($_SERVER['REQUEST_METHOD'] === 'POST') {
    switch ((string) ($_POST['action'] ?? '')) {
        case 'lexeme':
            admin_run(
                'UPDATE lexeme SET primary_lemma = ?, note = ? WHERE lexeme_id = ?',
                [admin_text('primary_lemma') ?? $lexeme['primary_lemma'], admin_text('note'), $id]
            );
            admin_flash('Uloženo.');
            break;

        case 'sense':
            admin_run(
                'INSERT INTO lexical_unit (lexeme_id, sense_label, gloss) VALUES (?, ?, ?)',
                [$id, admin_text('sense_label'), admin_text('gloss')]
            );
            admin_flash('Význam přidán. Teď mu dej rámec.');
            break;

        case 'sense_delete':
            // Rámce, sloty a realizace pod významem musí padnout s ním; MySQL by jinak cizí klíč
            // odmítl a smazání by z pohledu uživatele prostě nefungovalo.
            $luId = (int) ($_POST['lu_id'] ?? 0);
            admin_run(
                'DELETE r FROM slot_realization r
                   JOIN valency_slot s ON s.slot_id = r.slot_id
                   JOIN valency_frame f ON f.frame_id = s.frame_id
                  WHERE f.lu_id = ?',
                [$luId]
            );
            admin_run(
                'DELETE s FROM valency_slot s
                   JOIN valency_frame f ON f.frame_id = s.frame_id
                  WHERE f.lu_id = ?',
                [$luId]
            );
            admin_run('DELETE FROM valency_frame WHERE lu_id = ?', [$luId]);
            admin_run('DELETE FROM lexical_unit WHERE lu_id = ?', [$luId]);
            admin_flash('Význam i jeho rámce smazány.');
            break;

        case 'frame':
            try {
                admin_run(
                    'INSERT INTO valency_frame (lu_id, kind, diathesis, is_default) VALUES (?, ?, ?, ?)',
                    [
                        (int) ($_POST['lu_id'] ?? 0),
                        admin_enum('kind', 'kind') ?? 'Verbal',
                        admin_enum('diathesis', 'diathesis') ?? 'Active',
                        admin_flag('is_default') === 1 ? 1 : 0,
                    ]
                );
                admin_flash('Rámec založen. Přidej mu sloty — bez ACT neprojde kontrolou.');
            } catch (PDOException $exception) {
                if ($exception->getCode() === '23000') {
                    admin_flash('Ten význam už rámec pro tuhle diatezi má. Jeden rámec na diatezi.', 'err');
                    break;
                }

                throw $exception;
            }
            break;
    }

    admin_redirect(['p' => 'lexeme', 'id' => $id]);
}

$entries = admin_all(
    'SELECT lemma_entry_id, lemma, category, aspect FROM lemma_entry WHERE lexeme_id = ? ORDER BY lemma_key',
    [$id]
);

$senses = admin_all('SELECT * FROM lexical_unit WHERE lexeme_id = ? ORDER BY lu_id', [$id]);

$frames = admin_all(
    'SELECT f.*, u.lu_id AS owner,
            (SELECT COUNT(*) FROM valency_slot s WHERE s.frame_id = f.frame_id) AS slots
       FROM valency_frame f
       JOIN lexical_unit u ON u.lu_id = f.lu_id
      WHERE u.lexeme_id = ?
      ORDER BY f.frame_id',
    [$id]
);
?>

<p class="crumbs"><a href="<?= h(admin_url(['p' => 'list'])) ?>">Hesla</a> / lexém <?= h((string) $lexeme['primary_lemma']) ?></p>

<form method="post" class="card">
    <input type="hidden" name="csrf" value="<?= h(admin_csrf_token()) ?>">
    <input type="hidden" name="action" value="lexeme">
    <h2>Lexém</h2>
    <div class="grid">
        <p class="field">
            <label for="primary_lemma">Hlavní lemma</label>
            <input type="text" id="primary_lemma" name="primary_lemma" value="<?= h((string) $lexeme['primary_lemma']) ?>">
            <small>U vidové dvojice zvykově nedokonavé.</small>
        </p>
        <p class="field">
            <label for="note">Poznámka</label>
            <input type="text" id="note" name="note" value="<?= h((string) $lexeme['note']) ?>">
        </p>
    </div>
    <div class="actions"><button type="submit">Uložit</button></div>
</form>

<section class="card">
    <h2>Hesla na tomhle lexému</h2>
    <?php if ($entries === []): ?>
        <p class="empty">Žádné. Přiřaď lexém heslu v jeho formuláři.</p>
    <?php else: ?>
        <ul class="chips">
        <?php foreach ($entries as $entry): ?>
            <li>
                <a href="<?= h(admin_url(['p' => 'lemma', 'id' => (int) $entry['lemma_entry_id']])) ?>">
                    <?= h((string) $entry['lemma']) ?>
                </a>
                <?php if ($entry['aspect'] !== null): ?>
                    <span class="muted"><?= h(LEXICON_ENUMS['aspect'][$entry['aspect']] ?? '') ?></span>
                <?php endif; ?>
            </li>
        <?php endforeach; ?>
        </ul>
        <p class="hint">Všechna sdílejí rámce níž. Právě proto je vidová dvojice jeden lexém.</p>
    <?php endif; ?>
</section>

<section class="card">
    <h2>Významy a rámce</h2>

    <?php if ($senses === []): ?>
        <p class="empty">Zatím žádný význam.</p>
    <?php endif; ?>

    <?php foreach ($senses as $sense): ?>
        <?php $own = array_values(array_filter($frames, static fn (array $f): bool => (int) $f['lu_id'] === (int) $sense['lu_id'])); ?>
        <div class="sense">
            <h3><?= h((string) ($sense['sense_label'] ?? '(bez názvu)')) ?></h3>
            <?php if ($sense['gloss'] !== null): ?><p class="gloss"><?= h((string) $sense['gloss']) ?></p><?php endif; ?>

            <?php if ($own === []): ?>
                <p class="empty">Bez rámce.</p>
            <?php else: ?>
                <ul class="frames">
                <?php foreach ($own as $frame): ?>
                    <li>
                        <a href="<?= h(admin_url(['p' => 'frame', 'id' => (int) $frame['frame_id']])) ?>">
                            <?= h(LEXICON_ENUMS['diathesis'][$frame['diathesis']] ?? (string) $frame['diathesis']) ?>
                        </a>
                        <span class="muted"><?= h(LEXICON_ENUMS['kind'][$frame['kind']] ?? '') ?></span>
                        <span class="badge<?= (int) $frame['slots'] === 0 ? ' warn' : '' ?>"><?= (int) $frame['slots'] ?> slotů</span>
                        <?php if ((int) $frame['is_default'] === 1): ?><span class="badge">výchozí</span><?php endif; ?>
                    </li>
                <?php endforeach; ?>
                </ul>
            <?php endif; ?>

            <form method="post" class="inline">
                <input type="hidden" name="csrf" value="<?= h(admin_csrf_token()) ?>">
                <input type="hidden" name="action" value="frame">
                <input type="hidden" name="lu_id" value="<?= (int) $sense['lu_id'] ?>">
                <?= admin_select('kind', 'kind', 'Verbal', allowEmpty: false) ?>
                <?= admin_select('diathesis', 'diathesis', 'Active', allowEmpty: false) ?>
                <label class="check"><input type="checkbox" name="is_default" value="1"> výchozí</label>
                <button type="submit">Přidat rámec</button>
            </form>

            <form method="post" class="inline" onsubmit="return confirm('Smazat význam i s rámci?');">
                <input type="hidden" name="csrf" value="<?= h(admin_csrf_token()) ?>">
                <input type="hidden" name="action" value="sense_delete">
                <input type="hidden" name="lu_id" value="<?= (int) $sense['lu_id'] ?>">
                <button type="submit" class="del small">Smazat význam</button>
            </form>
        </div>
    <?php endforeach; ?>

    <form method="post" class="inline top">
        <input type="hidden" name="csrf" value="<?= h(admin_csrf_token()) ?>">
        <input type="hidden" name="action" value="sense">
        <label for="sense_label" class="sr">Název významu</label>
        <input type="text" id="sense_label" name="sense_label" placeholder="transfer, motion, perception…">
        <label for="gloss" class="sr">Popis</label>
        <input type="text" id="gloss" name="gloss" placeholder="stručný popis významu">
        <button type="submit">Přidat význam</button>
    </form>
</section>
