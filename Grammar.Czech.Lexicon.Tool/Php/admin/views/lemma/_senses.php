<?php

declare(strict_types=1);

defined('LEXICON_ADMIN') || exit('Tenhle soubor se nespouští přímo.');

/**
 * Způsob děje, který heslo má v jednotlivých významech svého lexému.
 *
 * @var int $id
 * @var list<\Lexicon\Admin\Read\EntrySense> $senses
 * @var \Lexicon\Admin\View\Url $url
 * @var \Lexicon\Admin\View\FormHelper $form
 */
?>
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
        <form method="post" action="<?= h($url->senseAktionsart($id, $sense->luId)) ?>" class="inline">
            <?= $form->csrf() ?>
            <strong><?= h($sense->displayLabel()) ?></strong>
            <?php if ($sense->gloss !== null): ?>
                <span class="muted"><?= h($sense->gloss) ?></span>
            <?php endif; ?>
            <label class="sr" for="aktionsart_<?= $sense->luId ?>">Způsob děje</label>
            <?= $form->select('aktionsart', 'aktionsart', $sense->aktionsart, id: 'aktionsart_' . $sense->luId) ?>
            <label class="sr" for="sense_note_<?= $sense->luId ?>">Poznámka</label>
            <input type="text" id="sense_note_<?= $sense->luId ?>" name="sense_note" class="grow"
                   value="<?= h((string) $sense->note) ?>" placeholder="čím to je">
            <button type="submit">Uložit</button>
        </form>
    <?php endforeach; ?>
</section>
