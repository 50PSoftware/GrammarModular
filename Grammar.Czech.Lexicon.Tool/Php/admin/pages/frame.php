<?php

declare(strict_types=1);

defined('LEXICON_ADMIN') || exit('Tenhle soubor se nespouští přímo.');

/**
 * Rámec: jeho sloty a povrchové realizace.
 *
 * Sloty a realizace se přidávají po jednom malým formulářem, ne jedním velkým s dynamickými řádky.
 * Je to víc kliknutí a řádově míň kódu, který se může pokazit, a hlavně to nepotřebuje JavaScript.
 *
 * Dvě věci hlídá i tahle stránka, protože se projeví až daleko od místa vzniku: rámec bez slotu ACT
 * a slot bez realizace s preferencí 1. Obojí kontroluje i validate při stahování — tady jde jen o to,
 * aby se to poznalo hned.
 */

$id = (int) ($_GET['id'] ?? 0);

$frame = admin_one(
    'SELECT f.*, u.sense_label, u.lexeme_id
       FROM valency_frame f
       JOIN lexical_unit u ON u.lu_id = f.lu_id
      WHERE f.frame_id = ?',
    [$id]
);

if ($frame === null) {
    admin_flash('Rámec neexistuje.', 'err');
    admin_redirect(['p' => 'list']);
}

if ($_SERVER['REQUEST_METHOD'] === 'POST') {
    switch ((string) ($_POST['action'] ?? '')) {
        case 'frame':
            // Diateze je půlka unikátního klíče, takže ji nejde přepsat na tu, kterou význam už má.
            // Dřív to nemohlo nastat — dokud byl každý rámec Active, měl význam nejvýš jeden.
            try {
                admin_run(
                    'UPDATE valency_frame SET kind = ?, diathesis = ?, is_default = ?, reflexive_type = ?
                     WHERE frame_id = ?',
                    [
                        admin_enum('kind', 'kind') ?? 'Verbal',
                        admin_enum('diathesis', 'diathesis') ?? 'Active',
                        admin_flag('is_default') === 1 ? 1 : 0,
                        admin_enum('reflexive_type', 'reflexive_type') ?? 'None',
                        $id,
                    ]
                );
                admin_flash('Uloženo.');
            } catch (PDOException $exception) {
                if ($exception->getCode() === '23000') {
                    admin_flash('Ten význam už rámec pro tuhle diatezi má. Jeden rámec na diatezi.', 'err');
                    break;
                }

                throw $exception;
            }
            break;

        case 'slot':
            try {
                admin_run(
                    'INSERT INTO valency_slot
                        (frame_id, functor, canonical_order, obligatoriness,
                         can_drop_contextual, can_drop_generic, control_target)
                     VALUES (?, ?, ?, ?, ?, ?, ?)',
                    [
                        $id,
                        admin_enum('functor', 'functor') ?? 'ACT',
                        max(1, admin_int('canonical_order', 1) ?? 1),
                        admin_enum('obligatoriness', 'obligatoriness') ?? 'Optional',
                        admin_flag('can_drop_contextual') === 1 ? 1 : 0,
                        admin_flag('can_drop_generic') === 1 ? 1 : 0,
                        admin_enum('control_target', 'functor'),
                    ]
                );
                admin_flash('Slot přidán. Bez realizace se nemůže vyjádřit.');
            } catch (PDOException $exception) {
                if ($exception->getCode() === '23000') {
                    admin_flash('Tenhle funktor už v rámci je. Jeden slot na funktor.', 'err');
                    break;
                }

                throw $exception;
            }
            break;

        case 'slot_delete':
            $slotId = (int) ($_POST['slot_id'] ?? 0);
            admin_run('DELETE FROM slot_realization WHERE slot_id = ?', [$slotId]);
            admin_run('DELETE FROM valency_slot WHERE slot_id = ? AND frame_id = ?', [$slotId, $id]);
            admin_flash('Slot smazán.');
            break;

        case 'realization':
            $case = admin_enum('morph_case', 'morph_case');
            $clause = admin_text('clause_type');
            $infinitive = admin_flag('takes_infinitive') === 1 ? 1 : 0;

            // Realizace musí být něčím: pádem, větou, nebo infinitivem. Řádek, který není ničím,
            // by databáze odmítla kontrolou ck_slot_realization_shape, ale hláška z ní uživateli nic
            // neřekne.
            if ($case === null && $clause === null && $infinitive === 0) {
                admin_flash('Realizace musí mít pád, typ věty, nebo být infinitivní.', 'err');
                break;
            }

            if ($case === null && admin_text('preposition') !== null) {
                admin_flash('Předložka bez pádu nic neřídí. Doplň pád, nebo předložku smaž.', 'err');
                break;
            }

            admin_run(
                'INSERT INTO slot_realization
                    (slot_id, morph_case, preposition, clause_type, takes_infinitive, preference)
                 VALUES (?, ?, ?, ?, ?, ?)',
                [
                    (int) ($_POST['slot_id'] ?? 0),
                    $case,
                    admin_text('preposition'),
                    $clause,
                    $infinitive,
                    max(1, admin_int('preference', 1) ?? 1),
                ]
            );
            admin_flash('Realizace přidána.');
            break;

        case 'realization_delete':
            admin_run('DELETE FROM slot_realization WHERE realization_id = ?', [(int) ($_POST['realization_id'] ?? 0)]);
            admin_flash('Realizace smazána.');
            break;
    }

    admin_redirect(['p' => 'frame', 'id' => $id]);
}

$slots = admin_all(
    'SELECT * FROM valency_slot WHERE frame_id = ? ORDER BY canonical_order, slot_id',
    [$id]
);

$realizations = [];

foreach (admin_all(
    'SELECT r.* FROM slot_realization r
       JOIN valency_slot s ON s.slot_id = r.slot_id
      WHERE s.frame_id = ?
      ORDER BY r.preference, r.realization_id',
    [$id]
) as $row) {
    $realizations[(int) $row['slot_id']][] = $row;
}

$hasActor = array_filter($slots, static fn (array $s): bool => $s['functor'] === 'ACT') !== [];
?>

<p class="crumbs">
    <a href="<?= h(admin_url(['p' => 'list'])) ?>">Hesla</a> /
    <a href="<?= h(admin_url(['p' => 'lexeme', 'id' => (int) $frame['lexeme_id']])) ?>">lexém</a> /
    <?php /* Diateze patří do drobečku, ne jen do formuláře: jeden význam může mít rámců víc a bez ní
             by činný a trpný byly dvě stránky se stejným nadpisem. */ ?>
    rámec <?= h((string) ($frame['sense_label'] ?? '(bez názvu)')) ?>
    <span class="muted"><?= h(LEXICON_ENUMS['diathesis'][$frame['diathesis']] ?? (string) $frame['diathesis']) ?></span>
</p>

<?php if (!$hasActor): ?>
    <p class="msg err">Rámec nemá slot ACT. Každý český predikát má konatele, i když se nevysloví —
        <code>validate</code> to při stahování odmítne.</p>
<?php endif; ?>

<form method="post" class="card">
    <input type="hidden" name="csrf" value="<?= h(admin_csrf_token()) ?>">
    <input type="hidden" name="action" value="frame">
    <h2>Rámec</h2>
    <div class="grid">
        <p class="field"><label for="kind">Druh predikátu</label>
            <?= admin_select('kind', 'kind', (string) $frame['kind'], allowEmpty: false) ?></p>
        <p class="field"><label for="diathesis">Diateze</label>
            <?= admin_select('diathesis', 'diathesis', (string) $frame['diathesis'], allowEmpty: false) ?></p>
        <p class="field"><label>Výchozí rámec</label>
            <?= admin_flag_field('is_default', (int) $frame['is_default']) ?>
            <small>Rozhoduje mezi významy, ne mezi diatezemi — o tu si generátor říká sám. Který
                význam sloveso má, když ho volající nejmenuje. Nejvýš jeden na sloveso a diatezi;
                když výchozí nemá žádný, musí volající význam jmenovat vždycky.</small></p>
        <p class="field"><label for="reflexive_type">Reflexivita významu</label>
            <?= admin_select('reflexive_type', 'reflexive_type', (string) $frame['reflexive_type'], allowEmpty: false) ?>
            <small>Jen když částice patří tomuhle významu — dát si kávu, ale dát knihu ne. U reflexiva
                tantum (starat se) ji nes na hesle, tam platí pro všechny rámce.</small></p>
    </div>
    <div class="actions"><button type="submit">Uložit</button></div>
</form>

<section class="card">
    <h2>Sloty</h2>

    <?php if ($slots === []): ?>
        <p class="empty">Rámec zatím nemá žádný slot.</p>
    <?php endif; ?>

    <?php foreach ($slots as $slot): ?>
        <?php
        $slotId = (int) $slot['slot_id'];
        $own = $realizations[$slotId] ?? [];
        $hasPreferred = array_filter($own, static fn (array $r): bool => (int) $r['preference'] === 1) !== [];
        ?>
        <div class="slot">
            <div class="slot-head">
                <strong><?= h(LEXICON_ENUMS['functor'][$slot['functor']] ?? (string) $slot['functor']) ?></strong>
                <span class="badge"><?= h(LEXICON_ENUMS['obligatoriness'][$slot['obligatoriness']] ?? '') ?></span>
                <span class="muted">pořadí <?= (int) $slot['canonical_order'] ?></span>
                <?php if ($slot['control_target'] !== null): ?>
                    <span class="badge">kontrola → <?= h((string) $slot['control_target']) ?></span>
                <?php endif; ?>
                <form method="post" class="inline right" onsubmit="return confirm('Smazat slot i s realizacemi?');">
                    <input type="hidden" name="csrf" value="<?= h(admin_csrf_token()) ?>">
                    <input type="hidden" name="action" value="slot_delete">
                    <input type="hidden" name="slot_id" value="<?= $slotId ?>">
                    <button type="submit" class="del small">Smazat slot</button>
                </form>
            </div>

            <?php if ($own === []): ?>
                <p class="msg err">Slot nemá realizaci — nemá jak se vyjádřit.</p>
            <?php elseif (!$hasPreferred): ?>
                <p class="msg err">Žádná realizace nemá preferenci 1, takže se nebude generovat žádná.</p>
            <?php endif; ?>

            <?php if ($own !== []): ?>
            <div class="scroller">
            <table class="tight">
                <thead><tr><th>Pád</th><th>Předložka</th><th>Věta</th><th>Infinitiv</th><th>Preference</th><th></th></tr></thead>
                <tbody>
                <?php foreach ($own as $realization): ?>
                    <tr>
                        <td><?= h($realization['morph_case'] === null ? '—' : (LEXICON_ENUMS['morph_case'][$realization['morph_case']] ?? '')) ?></td>
                        <td class="mono"><?= h((string) ($realization['preposition'] ?? '—')) ?></td>
                        <td class="mono"><?= h((string) ($realization['clause_type'] ?? '—')) ?></td>
                        <td><?= ((int) $realization['takes_infinitive']) === 1 ? 'ano' : '—' ?></td>
                        <td><?= (int) $realization['preference'] ?><?= ((int) $realization['preference']) === 1 ? ' <span class="muted">(generuje se)</span>' : '' ?></td>
                        <td>
                            <form method="post" class="inline">
                                <input type="hidden" name="csrf" value="<?= h(admin_csrf_token()) ?>">
                                <input type="hidden" name="action" value="realization_delete">
                                <input type="hidden" name="realization_id" value="<?= (int) $realization['realization_id'] ?>">
                                <button type="submit" class="del small">×</button>
                            </form>
                        </td>
                    </tr>
                <?php endforeach; ?>
                </tbody>
            </table>
            </div>
            <?php endif; ?>

            <form method="post" class="inline">
                <input type="hidden" name="csrf" value="<?= h(admin_csrf_token()) ?>">
                <input type="hidden" name="action" value="realization">
                <input type="hidden" name="slot_id" value="<?= $slotId ?>">
                <?= admin_select('morph_case', 'morph_case', null) ?>
                <label class="sr" for="preposition_<?= $slotId ?>">Předložka</label>
                <input type="text" id="preposition_<?= $slotId ?>" name="preposition" placeholder="předložka" size="8">
                <label class="sr" for="clause_<?= $slotId ?>">Typ věty</label>
                <input type="text" id="clause_<?= $slotId ?>" name="clause_type" placeholder="že, aby, zda" size="8">
                <label class="check"><input type="checkbox" name="takes_infinitive" value="1"> infinitiv</label>
                <label class="sr" for="pref_<?= $slotId ?>">Preference</label>
                <input type="number" id="pref_<?= $slotId ?>" name="preference" value="1" min="1" size="3" title="1 = generuje se">
                <button type="submit">Přidat realizaci</button>
            </form>
        </div>
    <?php endforeach; ?>

    <form method="post" class="inline top">
        <input type="hidden" name="csrf" value="<?= h(admin_csrf_token()) ?>">
        <input type="hidden" name="action" value="slot">
        <?= admin_select('functor', 'functor', 'ACT', allowEmpty: false) ?>
        <?= admin_select('obligatoriness', 'obligatoriness', 'Optional', allowEmpty: false) ?>
        <label class="sr" for="canonical_order">Pořadí</label>
        <input type="number" id="canonical_order" name="canonical_order" value="<?= count($slots) + 1 ?>" min="1" size="3" title="kanonické pořadí">
        <label class="check"><input type="checkbox" name="can_drop_contextual" value="1"> vypustitelný kontextem</label>
        <label class="check"><input type="checkbox" name="can_drop_generic" value="1"> vypustitelný obecně</label>
        <button type="submit">Přidat slot</button>
    </form>
</section>
