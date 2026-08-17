<?php

declare(strict_types=1);

/**
 * Administrace českého slovníku.
 *
 * Jediný vstupní bod, a leží v kořeni — obsah Php/ jde do www/, takže administrace je na / a API
 * vedle ní na /api/. Cesta určuje stránku (/hesla, /heslo/42, /ramec/7), zápisy jdou přes POST a jsou
 * chráněné tokenem proti CSRF; po každém zápisu se přesměrovává, aby se obnovením stránky formulář
 * neodeslal znovu.
 *
 * Vnitřnosti — bootstrap, třídy a šablony — jsou v admin/, který .htaccess zakazuje celý. Sem patří
 * jen to, co má vydávat web server: tenhle soubor a style.css.
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

use Lexicon\Admin\Application;
use Lexicon\Admin\Http\Request;
use Lexicon\Admin\Http\Router;
use Lexicon\Admin\Kernel;

require __DIR__ . '/admin/bootstrap.php';

$request = Request::fromGlobals();
$application = new Application($request, __DIR__ . '/admin/views');

(new Kernel($application, new Router()))->handle($request)->send();
