<?php

declare(strict_types=1);

defined('LEXICON_ADMIN') || exit('Tenhle soubor se nespouští přímo.');

/**
 * Smazání hesla i s tím, co po něm zůstane rozbité.
 *
 * @var int $id
 * @var \Lexicon\Admin\Entity\LemmaEntry $entry
 * @var list<\Lexicon\Admin\Read\DeleteWarning> $deleteWarnings
 * @var \Lexicon\Admin\View\Url $url
 * @var \Lexicon\Admin\View\FormHelper $form
 */

// I do confirmu: kdo maže, klikne na tlačítko a dialog přečte, zatímco text nad ním přeskočil.
$confirm = 'Opravdu smazat heslo ' . $entry->lemma . '?';

foreach ($deleteWarnings as $warning) {
    $confirm .= "\n\n" . $warning->text;
}

// json_encode kvůli uvozovkám a zalomením v textu; h() proto, že výsledek jde do atributu.
$confirmLiteral = (string) json_encode($confirm, JSON_UNESCAPED_UNICODE);
?>
<form method="post" action="<?= h($url->deleteEntry($id)) ?>" class="card danger"
      onsubmit="return confirm(<?= h($confirmLiteral) ?>);">
    <?= $form->csrf() ?>
    <h2>Smazat heslo</h2>

    <?php if ($deleteWarnings === []): ?>
        <p>Nic dalšího na tohle heslo neukazuje.<?= $entry->lexemeId === null ? '' : ' Lexém a jeho rámce zůstanou — patří i druhému členu vidové dvojice.' ?></p>
    <?php else: ?>
        <p><strong>Po smazání zůstane rozbité:</strong></p>
        <ul class="warnings">
            <?php foreach ($deleteWarnings as $warning): ?>
                <li>
                    <?= h($warning->text) ?>
                    <a href="<?= h($warning->link) ?>"><?= h($warning->linkText) ?></a>
                </li>
            <?php endforeach; ?>
        </ul>
        <p class="hint">Smazat to jde i tak — jen to ve slovníku nechá data, ke kterým nevede cesta.
            Nástroj lexikonu na ně upozorní při každém <code>validate</code>.</p>
    <?php endif; ?>

    <button type="submit" class="del">Smazat</button>
</form>
