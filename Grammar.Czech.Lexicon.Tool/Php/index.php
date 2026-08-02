<?php

declare(strict_types=1);

/**
 * Administrace českého slovníku.
 *
 * Jediný vstupní bod, a leží v kořeni — obsah Php/ jde do www/, takže administrace je na / a API
 * vedle ní na /api/. Stránka se vybírá parametrem p, zápisy jdou přes POST a jsou chráněné tokenem
 * proti CSRF; po každém zápisu se přesměrovává, aby se obnovením stránky formulář neodeslal znovu.
 *
 * Vnitřnosti — lib.php a pages/ — jsou v admin/, který .htaccess zakazuje celý. Sem patří jen to,
 * co má vydávat web server: tenhle soubor a style.css.
 *
 * Administrace píše do databáze přímo, ne přes /api/. To API existuje pro replikaci — vrací stránky
 * tabulek v pořadí závislostí, aby si C# klient postavil kopii — což je jiná úloha než „ulož tohle
 * jedno heslo“. Kdyby zápis chodil přes něj, přibyl by HTTP skok na týž server, druhá autentizace
 * a druhá sada endpointů, a nic by se tím nesdílelo: pravidla, která by se sdílet vyplatilo, jsou
 * v C# validátoru, ne v PHP. Sdílí se to, co sdílet dává smysl — schema-tables.php.
 *
 * Konfigurace navíc oproti API:
 *
 *   LEXICON_ADMIN_PASSWORD_HASH   výstup password_hash(), ne samotné heslo
 */

// Soubory v admin/ se nesmí dát spustit přímo, ani kdyby se .htaccess neuplatnil. Každý si na
// začátku ověří tuhle konstantu; definuje se dřív než cokoli jiného, aby ji viděl i lib.php.
define('LEXICON_ADMIN', true);

require __DIR__ . '/admin/lib.php';

admin_session_start();
admin_check_csrf();

$page = (string) ($_GET['p'] ?? 'list');

if ($page === 'logout') {
    admin_sign_out();
    admin_redirect(['p' => 'list']);
}

if (!admin_is_signed_in()) {
    admin_login_page();
    exit(0);
}

// Zápisy si zpracuje stránka sama a přesměruje; sem se dostane jen vykreslení.
$view = match ($page) {
    'lemma' => __DIR__ . '/admin/pages/lemma.php',
    'lexeme' => __DIR__ . '/admin/pages/lexeme.php',
    'frame' => __DIR__ . '/admin/pages/frame.php',
    default => __DIR__ . '/admin/pages/list.php',
};

ob_start();
require $view;
$content = ob_get_clean();

admin_layout($content);

/**
 * Vykreslí přihlašovací stránku a případně zpracuje odeslané heslo.
 */
function admin_login_page(): void
{
    $error = null;

    if ($_SERVER['REQUEST_METHOD'] === 'POST') {
        if (admin_sign_in((string) ($_POST['password'] ?? ''))) {
            admin_redirect(['p' => 'list']);
        }

        // Bez upřesnění, jestli je špatně heslo nebo něco jiného — není co upřesňovat, uživatel je
        // jeden a heslo taky.
        $error = 'Špatné heslo.';

        // Drobné zpomalení, aby hádání dávalo méně pokusů za minutu.
        usleep(400000);
    }

    ob_start(); ?>
    <form method="post" class="card login">
        <h1>Slovník</h1>
        <input type="hidden" name="csrf" value="<?= h(admin_csrf_token()) ?>">
        <?php if ($error !== null): ?>
            <p class="msg err"><?= h($error) ?></p>
        <?php endif; ?>
        <label for="password">Heslo</label>
        <input type="password" id="password" name="password" autofocus autocomplete="current-password">
        <button type="submit">Přihlásit</button>
    </form>
    <?php
    admin_layout((string) ob_get_clean(), signedIn: false);
}

/**
 * Obalí obsah stránky společným rámcem.
 */
function admin_layout(string $content, bool $signedIn = true): void
{
    $flashes = admin_take_flashes();
    ?><!doctype html>
<html lang="cs">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
<meta name="robots" content="noindex, nofollow">
<title>Slovník — administrace</title>
<link rel="stylesheet" href="style.css">
</head>
<body>
<?php if ($signedIn): ?>
<header class="bar">
    <a class="brand" href="<?= h(admin_url(['p' => 'list'])) ?>">Slovník</a>
    <nav>
        <a href="<?= h(admin_url(['p' => 'lemma', 'id' => 'new'])) ?>">Nové heslo</a>
        <a href="<?= h(admin_url(['p' => 'logout'])) ?>">Odhlásit</a>
    </nav>
</header>
<?php endif; ?>
<main>
<?php foreach ($flashes as $flash): ?>
    <p class="msg <?= h($flash['kind']) ?>"><?= h($flash['message']) ?></p>
<?php endforeach; ?>
<?= $content ?>
</main>
</body>
</html>
<?php
}
