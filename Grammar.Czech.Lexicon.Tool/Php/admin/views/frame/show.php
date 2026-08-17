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
 *
 * @var \Lexicon\Admin\Read\FrameContext $context
 * @var list<\Lexicon\Admin\Entity\ValencySlot> $slots
 * @var array<int, list<\Lexicon\Admin\Entity\SlotRealization>> $realizations
 * @var \Lexicon\Admin\View\Url $url
 * @var \Lexicon\Admin\View\FormHelper $form
 * @var \Lexicon\Admin\Schema $schema
 */

$frame = $context->frame;
$id = (int) $frame->id;

$hasActor = array_filter(
    $slots,
    static fn (\Lexicon\Admin\Entity\ValencySlot $slot): bool => $slot->functor === 'ACT'
) !== [];
?>

<p class="crumbs">
    <a href="<?= h($url->entries()) ?>">Hesla</a> /
    <a href="<?= h($url->lexeme($context->lexemeId)) ?>">lexém</a> /
    <?php /* Diateze patří do drobečku, ne jen do formuláře: jeden význam může mít rámců víc a bez ní
             by činný a trpný byly dvě stránky se stejným nadpisem. */ ?>
    rámec <?= h($context->displaySenseLabel()) ?>
    <span class="muted"><?= h($schema->label('diathesis', $frame->diathesis)) ?></span>
</p>

<?php if (!$hasActor): ?>
    <p class="msg err">Rámec nemá slot ACT. Každý český predikát má konatele, i když se nevysloví —
        <code>validate</code> to při stahování odmítne.</p>
<?php endif; ?>

<form method="post" action="<?= h($url->frame($id)) ?>" class="card">
    <?= $form->csrf() ?>
    <h2>Rámec</h2>
    <div class="grid">
        <p class="field"><label for="kind">Druh predikátu</label>
            <?= $form->select('kind', 'kind', $frame->kind, allowEmpty: false) ?></p>
        <p class="field"><label for="diathesis">Diateze</label>
            <?= $form->select('diathesis', 'diathesis', $frame->diathesis, allowEmpty: false) ?></p>
        <p class="field"><label>Výchozí rámec</label>
            <?= $form->flagField('is_default', $frame->isDefault) ?>
            <small>Rozhoduje mezi významy, ne mezi diatezemi — o tu si generátor říká sám. Který
                význam sloveso má, když ho volající nejmenuje. Nejvýš jeden na sloveso a diatezi;
                když výchozí nemá žádný, musí volající význam jmenovat vždycky.</small></p>
        <p class="field"><label for="reflexive_type">Reflexivita významu</label>
            <?= $form->select('reflexive_type', 'reflexive_type', $frame->reflexiveType, allowEmpty: false) ?>
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
        $slotId = (int) $slot->id;
        $own = $realizations[$slotId] ?? [];
        $hasPreferred = array_filter(
            $own,
            static fn (\Lexicon\Admin\Entity\SlotRealization $realization): bool => $realization->isGenerated()
        ) !== [];
        ?>
        <div class="slot">
            <div class="slot-head">
                <strong><?= h($schema->label('functor', $slot->functor)) ?></strong>
                <span class="badge"><?= h($schema->label('obligatoriness', $slot->obligatoriness)) ?></span>
                <span class="muted">pořadí <?= $slot->canonicalOrder ?></span>
                <?php if ($slot->controlTarget !== null): ?>
                    <span class="badge">kontrola → <?= h($slot->controlTarget) ?></span>
                <?php endif; ?>
                <form method="post" action="<?= h($url->deleteSlot($id, $slotId)) ?>" class="inline right"
                      onsubmit="return confirm('Smazat slot i s realizacemi?');">
                    <?= $form->csrf() ?>
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
                        <td><?= h($schema->label('morph_case', $realization->morphCase)) ?></td>
                        <td class="mono"><?= h($realization->preposition ?? '—') ?></td>
                        <td class="mono"><?= h($realization->clauseType ?? '—') ?></td>
                        <td><?= $realization->takesInfinitive === 1 ? 'ano' : '—' ?></td>
                        <td><?= $realization->preference ?><?= $realization->isGenerated() ? ' <span class="muted">(generuje se)</span>' : '' ?></td>
                        <td>
                            <form method="post" action="<?= h($url->deleteRealization($id, (int) $realization->id)) ?>" class="inline">
                                <?= $form->csrf() ?>
                                <button type="submit" class="del small">×</button>
                            </form>
                        </td>
                    </tr>
                <?php endforeach; ?>
                </tbody>
            </table>
            </div>
            <?php endif; ?>

            <form method="post" action="<?= h($url->addRealization($id, $slotId)) ?>" class="inline">
                <?= $form->csrf() ?>
                <?= $form->select('morph_case', 'morph_case', null) ?>
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

    <form method="post" action="<?= h($url->addSlot($id)) ?>" class="inline top">
        <?= $form->csrf() ?>
        <?= $form->select('functor', 'functor', 'ACT', allowEmpty: false) ?>
        <?= $form->select('obligatoriness', 'obligatoriness', 'Optional', allowEmpty: false) ?>
        <label class="sr" for="canonical_order">Pořadí</label>
        <input type="number" id="canonical_order" name="canonical_order" value="<?= count($slots) + 1 ?>" min="1" size="3" title="kanonické pořadí">
        <label class="check"><input type="checkbox" name="can_drop_contextual" value="1"> vypustitelný kontextem</label>
        <label class="check"><input type="checkbox" name="can_drop_generic" value="1"> vypustitelný obecně</label>
        <button type="submit">Přidat slot</button>
    </form>
</section>
