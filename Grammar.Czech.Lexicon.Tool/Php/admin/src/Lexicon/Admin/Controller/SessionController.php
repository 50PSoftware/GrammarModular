<?php

declare(strict_types=1);

namespace Lexicon\Admin\Controller;

defined('LEXICON_ADMIN') || exit('Tenhle soubor se nespouští přímo.');

use Lexicon\Admin\Http\Request;
use Lexicon\Admin\Http\Response;
use Lexicon\Admin\Http\RouteParity;
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
        $mail = (string) ($request->form['mail'] ?? '');
        $password = (string) ($request->form['password'] ?? '');

        if ($this->authenticator->signIn($mail, $password)) {
            $this->reportRouteParity();

            return $this->redirect($this->url->entries());
        }

        // Drobné zpomalení, aby hádání dávalo méně pokusů za minutu.
        usleep(400000);

        // Bez upřesnění, co přesně nesedělo — špatný e-mail, špatné heslo, neověřený účet nebo
        // chybějící role. Není co upřesňovat cizímu člověku před přihlášením.
        return $this->page('login', ['error' => 'Přihlášení se nezdařilo.'], signedIn: false);
    }

    /**
     * Ohlásí, jestli se tabulka tras a stavitel adres rozešly.
     *
     * Běží při přihlášení, protože na tuhle kontrolu není v projektu jiné místo: PHP půlka se nikde
     * netestuje a na serveru není composer, kterým by se test dal spustit. Přihlášení je nejbližší
     * náhrada — je vzácné, takže ta trocha reflexe nic nestojí, a je to jediný okamžik, kdy se do
     * administrace dívá právě ten člověk, který s nálezem může něco udělat.
     *
     * Nic neblokuje. Rozejít se můžou o mrtvou routu, což nikomu nevadí, a přihlásit se nepustit kvůli
     * odkazu, na který se neklikne, by bylo horší než ta chyba sama. Do logu jde totéž co na obrazovku,
     * aby to zůstalo i po tom, co se hláška odklikne.
     */
    private function reportRouteParity(): void
    {
        foreach (RouteParity::problems() as $problem) {
            error_log('lexikon admin: ' . $problem);
            $this->flash->error($problem);
        }
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
