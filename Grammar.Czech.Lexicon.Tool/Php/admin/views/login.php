<?php

declare(strict_types=1);

defined('LEXICON_ADMIN') || exit('Tenhle soubor se nespouští přímo.');

/**
 * Přihlášení.
 *
 * @var string|null $error
 * @var \Lexicon\Admin\View\Url $url
 * @var \Lexicon\Admin\View\FormHelper $form
 */
?>
<form method="post" action="<?= h($url->signIn()) ?>" class="card login">
    <h1>Slovník</h1>
    <?= $form->csrf() ?>
    <?php if ($error !== null): ?>
        <p class="msg err"><?= h($error) ?></p>
    <?php endif; ?>
    <label for="password">Heslo</label>
    <input type="password" id="password" name="password" autofocus autocomplete="current-password">
    <button type="submit">Přihlásit</button>
</form>
