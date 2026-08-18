<?php

declare(strict_types=1);

namespace Lexicon\Admin\Controller;

defined('LEXICON_ADMIN') || exit('Tenhle soubor se nespouští přímo.');

use Lexicon\Admin\Database\IntegrityViolation;
use Lexicon\Admin\Entity\Lexeme;
use Lexicon\Admin\Http\HttpException;
use Lexicon\Admin\Http\Request;
use Lexicon\Admin\Http\Response;
use Lexicon\Admin\Http\RouteMatch;
use Lexicon\Admin\Input\OldInput;
use Lexicon\Admin\Repository\LexemeRepository;
use Lexicon\Admin\Schema;
use Lexicon\Admin\View\Flash;
use Lexicon\Admin\View\Url;
use Lexicon\Admin\View\View;

/**
 * Lexém, jeho významy a rámce na nich.
 */
final class LexemeController extends Controller
{
    public function __construct(
        View $view,
        Url $url,
        Flash $flash,
        OldInput $old,
        Schema $schema,
        private readonly LexemeRepository $lexemes
    ) {
        parent::__construct($view, $url, $flash, $old, $schema);
    }

    /**
     * Stránka lexému.
     */
    public function show(Request $request, RouteMatch $route): Response
    {
        $id = $route->id('id');
        $lexeme = $this->lexemes->findById($id);

        if ($lexeme === null) {
            $this->flash->error('Lexém neexistuje.');

            return $this->redirect($this->url->entries());
        }

        return $this->page('lexeme/show', [
            'lexeme' => $lexeme,
            'entries' => $this->lexemes->entries($id),
            'senses' => $this->lexemes->senses($id),
            'frames' => $this->lexemes->frames($id),
        ]);
    }

    /**
     * Přepíše lexém.
     */
    public function update(Request $request, RouteMatch $route): Response
    {
        $id = $route->id('id');
        $lexeme = $this->requireLexeme($id);
        $form = $this->formData($request);

        $this->lexemes->update(
            $id,
            $form->text('primary_lemma') ?? $lexeme->primaryLemma,
            $form->text('note')
        );
        $this->flash->ok('Uloženo.');

        return $this->redirect($this->url->lexeme($id));
    }

    /**
     * Přidá lexému význam.
     */
    public function addSense(Request $request, RouteMatch $route): Response
    {
        $id = $route->id('id');
        $this->requireLexeme($id);
        $form = $this->formData($request);

        try {
            $this->lexemes->addSense($id, $form->text('sense_label'), $form->text('gloss'));
            $this->flash->ok('Význam přidán. Teď mu dej rámec.');
        } catch (IntegrityViolation) {
            return $this->refuse($this->duplicateSenseMessage(), $this->url->lexeme($id));
        }

        return $this->redirect($this->url->lexeme($id));
    }

    /**
     * Přepíše význam.
     */
    public function updateSense(Request $request, RouteMatch $route): Response
    {
        $id = $route->id('id');
        $luId = $this->requireSense($id, $route->id('luId'));
        $form = $this->formData($request);

        try {
            $this->lexemes->updateSense($luId, $id, $form->text('sense_label'), $form->text('gloss'));
            $this->flash->ok('Význam uložen.');
        } catch (IntegrityViolation) {
            // Stejné UNIQUE (lexeme_id, sense_label) jako u zakládání — jen se do něj teď dá narazit
            // přejmenováním na název, který na lexému už je.
            return $this->refuse($this->duplicateSenseMessage(), $this->url->lexeme($id));
        }

        return $this->redirect($this->url->lexeme($id));
    }

    /**
     * Smaže význam i s jeho rámci.
     */
    public function deleteSense(Request $request, RouteMatch $route): Response
    {
        $id = $route->id('id');
        $luId = $this->requireSense($id, $route->id('luId'));

        $this->lexemes->deleteSenseCascade($luId);
        $this->flash->ok('Význam i jeho rámce smazány.');

        return $this->redirect($this->url->lexeme($id));
    }

    /**
     * Založí významu rámec.
     */
    public function addFrame(Request $request, RouteMatch $route): Response
    {
        $id = $route->id('id');
        $luId = $this->requireSense($id, $route->id('luId'));
        $form = $this->formData($request);

        try {
            $this->lexemes->addFrame(
                $luId,
                $form->enumOr('kind', 'kind', 'Verbal'),
                $form->enumOr('diathesis', 'diathesis', 'Active'),
                $form->checkbox('is_default')
            );
            $this->flash->ok('Rámec založen. Přidej mu sloty — bez ACT neprojde kontrolou.');
        } catch (IntegrityViolation) {
            return $this->refuse(
                'Ten význam už rámec pro tuhle diatezi má. Jeden rámec na diatezi.',
                $this->url->lexeme($id)
            );
        }

        return $this->redirect($this->url->lexeme($id));
    }

    /**
     * Načte lexém, na který akce sahá, nebo skončí.
     */
    private function requireLexeme(int $id): Lexeme
    {
        $lexeme = $this->lexemes->findById($id);

        if ($lexeme === null) {
            throw HttpException::notFound();
        }

        return $lexeme;
    }

    /**
     * Ověří, že význam patří tomuhle lexému, a vrátí jeho číslo.
     *
     * Obě čísla přišla z cesty, a bez téhle kontroly by podvržená dvojice sáhla na význam cizího
     * lexému, aniž by to stránka dala najevo.
     */
    private function requireSense(int $lexemeId, int $luId): int
    {
        $this->requireLexeme($lexemeId);

        if (!$this->lexemes->hasSense($lexemeId, $luId)) {
            throw HttpException::notFound();
        }

        return $luId;
    }

    /**
     * Hláška o názvu významu, který na lexému už je.
     */
    private function duplicateSenseMessage(): string
    {
        return 'Význam s tímhle názvem už na lexému je. Rámce se věší na význam, takže se '
            . 'názvy musí lišit.';
    }
}
