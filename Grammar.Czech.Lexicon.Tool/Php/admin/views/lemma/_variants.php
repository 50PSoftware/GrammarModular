<?php

declare(strict_types=1);

defined('LEXICON_ADMIN') || exit('Tenhle soubor se nespouští přímo.');

/**
 * Další spisovné podoby hesla.
 *
 * @var int $id
 * @var list<\Lexicon\Admin\Entity\LemmaVariant> $variants
 * @var \Lexicon\Admin\View\Url $url
 * @var \Lexicon\Admin\View\FormHelper $form
 */
?>
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
                <strong><?= h($variant->lemma) ?></strong>
                <?php if ($variant->note !== null): ?>
                    <span class="muted"><?= h($variant->note) ?></span>
                <?php endif; ?>
                <form method="post" action="<?= h($url->deleteVariant($id, (int) $variant->id)) ?>"
                      class="inline" onsubmit="return confirm('Smazat podobu?');">
                    <?= $form->csrf() ?>
                    <button type="submit" class="del small">Smazat</button>
                </form>
            </li>
        <?php endforeach; ?>
        </ul>
    <?php endif; ?>

    <form method="post" action="<?= h($url->addVariant($id)) ?>" class="inline top">
        <?= $form->csrf() ?>
        <label for="variant_lemma" class="sr">Podoba</label>
        <input type="text" id="variant_lemma" name="variant_lemma" placeholder="setmět">
        <label for="variant_note" class="sr">Poznámka</label>
        <input type="text" id="variant_note" name="variant_note" class="grow"
               placeholder="odkud je — třeba „IJP: lze i“">
        <button type="submit">Přidat podobu</button>
    </form>
</section>
