<?php

declare(strict_types=1);

namespace Lexicon\Admin\Controller;

defined('LEXICON_ADMIN') || exit('Tenhle soubor se nespouští přímo.');

use Lexicon\Admin\Http\Request;
use Lexicon\Admin\Http\Response;
use Lexicon\Admin\Input\OldInput;
use Lexicon\Admin\Schema;
use Lexicon\Admin\Security\Authenticator;
use Lexicon\Admin\View\Flash;
use Lexicon\Admin\View\Url;
use Lexicon\Admin\View\View;

/**
 * Přihlášení a odhlášení.
 */
final class SessionController extends Controller
{
    public function __construct(
        View $view,
        Url $url,
        Flash $flash,
        OldInput $old,
        Schema $schema,
        private readonly Authenticator $authenticator
    ) {
        parent::__construct($view, $url, $flash, $old, $schema);
    }

    /**
     * Přihlašovací stránka.
     */
    public function form(): Response
    {
        if ($this->authenticator->isSignedIn()) {
            return $this->redirect($this->url->entries());
        }

        return $this->page('login', ['error' => null], signedIn: false);
    }

    /**
     * Zpracuje odeslané heslo.
     */
    public function signIn(Request $request): Response
    {
        if ($this->authenticator->signIn((string) ($request->form['password'] ?? ''))) {
            return $this->redirect($this->url->entries());
        }

        // Drobné zpomalení, aby hádání dávalo méně pokusů za minutu.
        usleep(400000);

        // Bez upřesnění, jestli je špatně heslo nebo něco jiného — není co upřesňovat, uživatel je
        // jeden a heslo taky.
        return $this->page('login', ['error' => 'Špatné heslo.'], signedIn: false);
    }

    /**
     * Odhlásí a vrátí na přihlášení.
     */
    public function signOut(): Response
    {
        $this->authenticator->signOut();

        return $this->redirect($this->url->signIn());
    }
}
