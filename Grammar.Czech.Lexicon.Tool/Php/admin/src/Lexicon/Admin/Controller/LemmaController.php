<?php

declare(strict_types=1);

namespace Lexicon\Admin\Controller;

defined('LEXICON_ADMIN') || exit('Tenhle soubor se nespouští přímo.');

use Lexicon\Admin\Database\Database;
use Lexicon\Admin\Database\IntegrityViolation;
use Lexicon\Admin\Entity\LemmaEntry;
use Lexicon\Admin\Http\HttpException;
use Lexicon\Admin\Http\Request;
use Lexicon\Admin\Http\Response;
use Lexicon\Admin\Http\RouteMatch;
use Lexicon\Admin\Input\FormData;
use Lexicon\Admin\Input\LemmaKey;
use Lexicon\Admin\Input\OldInput;
use Lexicon\Admin\Input\PatternValidator;
use Lexicon\Admin\Read\DeleteWarning;
use Lexicon\Admin\Repository\LemmaRepository;
use Lexicon\Admin\Repository\LexemeRepository;
use Lexicon\Admin\Schema;
use Lexicon\Admin\View\Flash;
use Lexicon\Admin\View\FormValues;
use Lexicon\Admin\View\Url;
use Lexicon\Admin\View\View;

/**
 * Jedno heslo: založení, úprava, smazání, dublety a způsob děje po významech.
 *
 * Každá z těch věcí je vlastní akce na vlastní adrese. Dřív to byl jeden soubor se switchem nad
 * skrytým polem action, což znamenalo, že každý formulář na stránce mohl spustit kteroukoli větev —
 * a uložení dublety se muselo hlídat, aby heslo nepřepsalo prázdnem, které v jeho formuláři nebylo.
 */
final class LemmaController extends Controller
{
    public function __construct(
        View $view,
        Url $url,
        Flash $flash,
        OldInput $old,
        Schema $schema,
        private readonly LemmaRepository $lemmas,
        private readonly LexemeRepository $lexemes,
        private readonly PatternValidator $patterns,
        private readonly Database $database
    ) {
        parent::__construct($view, $url, $flash, $old, $schema);
    }

    /**
     * Prázdný formulář nového hesla.
     */
    public function create(): Response
    {
        return $this->renderForm(null);
    }

    /**
     * Formulář existujícího hesla.
     */
    public function edit(Request $request, RouteMatch $route): Response
    {
        $entry = $this->lemmas->findById($route->id('id'));

        if ($entry === null) {
            $this->flash->error('Heslo neexistuje.');

            return $this->redirect($this->url->entries());
        }

        return $this->renderForm($entry);
    }

    /**
     * Založí heslo.
     */
    public function store(Request $request): Response
    {
        return $this->save($request, null);
    }

    /**
     * Přepíše heslo.
     */
    public function update(Request $request, RouteMatch $route): Response
    {
        $id = $route->id('id');
        $entry = $this->lemmas->findById($id);

        if ($entry === null) {
            $this->flash->error('Heslo neexistuje.');

            return $this->redirect($this->url->entries());
        }

        return $this->save($request, $id);
    }

    /**
     * Smaže heslo.
     */
    public function destroy(Request $request, RouteMatch $route): Response
    {
        $id = $route->id('id');
        $entry = $this->lemmas->findById($id);

        if ($entry === null) {
            $this->flash->error('Heslo neexistuje.');

            return $this->redirect($this->url->entries());
        }

        $this->lemmas->delete($id);
        $this->flash->ok('Heslo „' . $entry->lemma . '“ smazáno.');

        return $this->redirect($this->url->entries());
    }

    /**
     * Přidá heslu další spisovnou podobu.
     */
    public function addVariant(Request $request, RouteMatch $route): Response
    {
        $id = $route->id('id');
        $this->requireEntry($id);

        $form = $this->form($request);
        $variant = $form->text('variant_lemma');

        if ($variant === null) {
            return $this->refuse('Podoba nesmí být prázdná.', $this->url->entry($id));
        }

        try {
            $this->lemmas->addVariant($id, $variant, $form->text('variant_note'));
            $this->flash->ok('Podoba přidána.');
        } catch (IntegrityViolation) {
            // UNIQUE na lemma_key. Buď je ta podoba dubletou jiného hesla, nebo tohohle — obojí
            // znamená, že se pod tím klíčem už hledá něco jiného.
            return $this->refuse(
                'Podoba „' . $variant . '“ už je vedená jinde. Jedna podoba, jedno heslo.',
                $this->url->entry($id)
            );
        }

        return $this->redirect($this->url->entry($id));
    }

    /**
     * Smaže podobu hesla.
     */
    public function deleteVariant(Request $request, RouteMatch $route): Response
    {
        $id = $route->id('id');
        $this->requireEntry($id);

        $this->lemmas->deleteVariant($route->id('variantId'), $id);
        $this->flash->ok('Podoba smazána.');

        return $this->redirect($this->url->entry($id));
    }

    /**
     * Zapíše způsob děje, který má tohle heslo v jednom významu svého lexému.
     */
    public function saveSenseAktionsart(Request $request, RouteMatch $route): Response
    {
        $id = $route->id('id');
        $entry = $this->requireEntry($id);
        $luId = $route->id('luId');

        // Význam musí patřit lexému tohohle hesla — jinak by číslo z cesty zapisovalo k cizímu slovu.
        if ($entry->lexemeId === null || !$this->lexemes->hasSense($entry->lexemeId, $luId)) {
            throw HttpException::notFound();
        }

        $form = $this->form($request);
        $this->lemmas->saveSenseAktionsart(
            $id,
            $luId,
            $form->enum('aktionsart', 'aktionsart'),
            $form->text('sense_note')
        );
        $this->flash->ok('Uloženo.');

        return $this->redirect($this->url->entry($id));
    }

    /**
     * Vykreslí formulář hesla.
     */
    private function renderForm(?LemmaEntry $entry): Response
    {
        $isNew = $entry === null;
        $id = $isNew ? 0 : (int) $entry->id;
        $stored = $isNew ? [] : $entry->toRow();

        return $this->page('lemma/form', [
            'entry' => $entry,
            'isNew' => $isNew,
            'id' => $id,
            'stored' => $stored,
            'values' => new FormValues($this->old, $stored),
            'lexemes' => $this->lexemes->all(),
            'variants' => $isNew ? [] : $this->lemmas->variants($id),
            'senses' => $isNew || $entry->lexemeId === null
                ? []
                : $this->lemmas->sensesFor($id, $entry->lexemeId),
            'deleteWarnings' => $isNew ? [] : $this->deleteWarnings($entry),
        ]);
    }

    /**
     * Uloží heslo, ať už nové nebo existující.
     *
     * Lexém a heslo se zakládají v jedné transakci. Bez ní by odmítnutý INSERT hesla — typicky
     * homonymum bez pořadí — nechal v tabulce lexém, na který už nikdy nic neukáže.
     */
    private function save(Request $request, ?int $id): Response
    {
        $isNew = $id === null;
        $back = $isNew ? $this->url->newEntry() : $this->url->entry($id);
        $form = $this->form($request);

        $lemma = $form->text('lemma');

        if ($lemma === null) {
            return $this->refuseSave('Lemma nesmí být prázdné.', $back, $request);
        }

        $category = $form->enumOr('category', 'category', 'Noun');
        $pattern = $this->patterns->check($form->text('pattern'), $category);

        if (!$pattern->isAccepted()) {
            return $this->refuseSave((string) $pattern->error, $back, $request);
        }

        $verbClass = $form->enum('verb_class', 'verb_class');
        $patternValue = $pattern->value;

        // Třída doplní vzor, když žádný není, a vyplněný nepřepíše: psát ani moci do třídy zapsat
        // nejdou a přepsat psát na trida1 by je časovalo bez alternace kmene. Stejné priority má
        // CzechVerbConjugationService. Tady, a ne jen v JavaScriptu, protože uložit se dá i bez něj.
        if ($category === 'Verb' && $patternValue === null && $verbClass !== null) {
            $patternValue = $this->schema->patternForVerbClass($verbClass);
        }

        $wantsNewLexeme = $form->text('lexeme_id') === 'new';

        try {
            $saved = $this->database->transaction(function () use (
                $form,
                $id,
                $isNew,
                $lemma,
                $category,
                $patternValue,
                $verbClass,
                $wantsNewLexeme
            ): int {
                // Lexém: buď existující, nebo nový, nebo žádný. Slova bez valence — většina
                // substantiv — ho nepotřebují a NULL je u nich správná hodnota, ne mezera.
                $lexemeId = $wantsNewLexeme
                    ? $this->lexemes->create($lemma)
                    : $form->int('lexeme_id');

                $entry = $this->entryFrom($form, $lemma, $category, $patternValue, $verbClass, $lexemeId);

                if ($isNew) {
                    return $this->lemmas->insert($entry);
                }

                $this->lemmas->update($id, $entry);

                return $id;
            });
        } catch (IntegrityViolation) {
            // Prakticky vždy to tady znamená UNIQUE na (lemma_key, category, homonym_index) — tedy
            // homonymum, kterému nikdo nedal pořadové číslo.
            return $this->refuseSave(
                'Heslo „' . $lemma . '“ s tímhle slovním druhem už existuje. Jde-li o homonymum '
                . '(stát jako budova a stát jako země), dej mu jiné pořadí homonyma.',
                $back,
                $request
            );
        }

        $this->flash->ok($isNew ? 'Heslo „' . $lemma . '“ založeno.' : 'Uloženo.');

        return $this->redirect($this->url->entry($saved));
    }

    /**
     * Poskládá heslo z formuláře.
     */
    private function entryFrom(
        FormData $form,
        string $lemma,
        string $category,
        ?string $pattern,
        ?string $verbClass,
        ?int $lexemeId
    ): LemmaEntry {
        return new LemmaEntry(
            null,
            $lemma,
            LemmaKey::of($lemma),
            $form->int('homonym_index', 1),
            $category,
            $form->enum('gender', 'gender'),
            $pattern,
            $form->flag('is_animate'),
            $form->flag('has_mobile_e'),
            $form->flag('has_genitive_plural_shortening'),
            $form->flag('has_epenthesis_in_genitive_plural'),
            $form->flag('is_indeclinable'),
            $form->flag('is_plural_only'),
            $form->flag('is_countable'),
            $form->flag('prefers_short_form'),
            $verbClass,
            $form->enum('aspect', 'aspect'),
            $form->text('aspect_counterpart'),
            $form->enum('aktionsart', 'aktionsart'),
            $form->enumOr('reflexive_type', 'reflexive_type', 'None'),
            $form->text('base_verb_lemma'),
            $form->enum('inherent_functor', 'inherent_functor'),
            $form->text('stem'),
            $form->text('present_stem'),
            $form->text('past_stem'),
            $form->text('future_stem'),
            $form->text('imperative_stem'),
            $form->text('passive_stem'),
            $form->text('infinitive'),
            $form->flag('forms_passive'),
            $lexemeId,
            $form->text('source'),
            $form->checkbox('is_verified'),
            $form->text('note')
        );
    }

    /**
     * Co po smazání zůstane rozbité.
     *
     * Neblokuje to — heslo založené omylem je důvod ho smazat — ale ani jedno není z formuláře vidět:
     * cizí klíč vede od hesla k lexému a ne zpátky, takže databáze mlčí.
     *
     * @return list<DeleteWarning>
     */
    private function deleteWarnings(LemmaEntry $entry): array
    {
        $id = (int) $entry->id;
        $warnings = [];

        $orphan = $entry->lexemeId === null
            ? null
            : $this->lemmas->findOrphanLexeme($entry->lexemeId, $id);

        if ($orphan !== null) {
            $warnings[] = new DeleteWarning(
                'Na lexém „' . $orphan->primaryLemma . '“ pak neukáže žádné heslo. '
                . 'Jeho významy (' . $orphan->senses . ') a jejich rámce zůstanou '
                . 'v databázi, ale nepůjde se k nim dostat.',
                $this->url->lexeme($orphan->id),
                'Otevřít lexém'
            );
        }

        foreach ($this->lemmas->findReferrers($entry->lemma, $id) as $referrer) {
            $warnings[] = new DeleteWarning(
                'Heslo „' . $referrer->lemma . '“ na tohle ukazuje přes „' . $referrer->via
                . '“. Odkaz zůstane viset na slovo, které ve slovníku nebude.',
                $this->url->entry($referrer->id),
                'Otevřít heslo'
            );
        }

        return $warnings;
    }

    /**
     * Načte heslo, na které akce sahá, nebo skončí.
     */
    private function requireEntry(int $id): LemmaEntry
    {
        $entry = $this->lemmas->findById($id);

        if ($entry === null) {
            throw HttpException::notFound();
        }

        return $entry;
    }
}
