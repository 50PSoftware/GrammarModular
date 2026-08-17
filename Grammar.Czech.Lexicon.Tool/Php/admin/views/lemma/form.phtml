<?php

declare(strict_types=1);

defined('LEXICON_ADMIN') || exit('Tenhle soubor se nespouští přímo.');

/**
 * Založení a úprava jednoho hesla.
 *
 * Formulář je na slovo, ne na tabulku: heslo se ukládá do lemma_entry, ale valence visí na lexému,
 * a ten se odsud dá založit nebo připojit. Vidová dvojice sdílí jeden lexém, takže dát a dávat mají
 * jeden rámec místo dvou kopií, které se rozejdou.
 *
 * @var \Lexicon\Admin\Entity\LemmaEntry|null $entry
 * @var bool $isNew
 * @var int $id
 * @var array<string, mixed> $stored
 * @var \Lexicon\Admin\View\FormValues $values
 * @var list<\Lexicon\Admin\Entity\Lexeme> $lexemes
 * @var list<\Lexicon\Admin\Entity\LemmaVariant> $variants
 * @var list<\Lexicon\Admin\Read\EntrySense> $senses
 * @var list<\Lexicon\Admin\Read\DeleteWarning> $deleteWarnings
 * @var \Lexicon\Admin\View\Url $url
 * @var \Lexicon\Admin\View\FormHelper $form
 * @var \Lexicon\Admin\Schema $schema
 * @var \Lexicon\Admin\View\View $view
 */

// Skládací sekce. Formulář pokrývá všechny slovní druhy, takže u každého hesla jsou dvě třetiny
// políček bezpředmětné — u substantiva vid a kmeny, u slovesa pomnožnost. Sekce, ve které heslo něco
// má, se otevře sama; zbytek se dá rozbalit. Pole zůstávají ve formuláři i složená a odesílají se.
$foldColumns = [
    'flags' => [
        'has_mobile_e', 'has_genitive_plural_shortening', 'has_epenthesis_in_genitive_plural',
        'is_indeclinable', 'is_plural_only', 'is_countable', 'prefers_short_form',
    ],
    'verb' => ['verb_class', 'aspect', 'aspect_counterpart', 'aktionsart', 'base_verb_lemma'],
    'inherent' => ['inherent_functor'],
    'stems' => [
        'stem', 'present_stem', 'past_stem', 'future_stem', 'imperative_stem', 'passive_stem',
        'infinitive', 'forms_passive',
    ],
];

$foldFilled = array_map(
    static fn (array $columns): int => $form->filledCount($stored, $columns),
    $foldColumns
);

// reflexive_type je NOT NULL s výchozím 'None', takže by sekci Sloveso otvíral i u substantiva.
// Počítá se, jen když opravdu něco říká.
if (($stored['reflexive_type'] ?? 'None') !== 'None') {
    $foldFilled['verb']++;
}
?>

<p class="crumbs"><a href="<?= h($url->entries()) ?>">Hesla</a> / <?= $isNew ? 'nové' : h($entry->lemma) ?></p>

<form method="post" action="<?= h($isNew ? $url->newEntry() : $url->entry($id)) ?>" class="card">
    <?= $form->csrf() ?>

    <h2>Heslo</h2>

    <div class="grid">
        <p class="field">
            <label for="lemma">Lemma</label>
            <input type="text" id="lemma" name="lemma" value="<?= h($values->text('lemma')) ?>" required autofocus>
            <small>Klíč pro vyhledávání se z něj spočítá sám.</small>
        </p>
        <p class="field">
            <label for="category">Slovní druh</label>
            <?= $form->select('category', 'category', $values->choice('category', 'Noun'), allowEmpty: false) ?>
        </p>
        <p class="field">
            <label for="homonym_index">Pořadí homonyma</label>
            <input type="number" id="homonym_index" name="homonym_index" min="1" value="<?= $values->int('homonym_index', 1) ?>">
            <small>1, pokud lemma není homonymní.</small>
        </p>
        <p class="field">
            <label for="pattern">Vzor</label>
            <input type="text" id="pattern" name="pattern" value="<?= h($values->text('pattern')) ?>" class="mono" list="patterns">
            <?php /* Nabídka, ne výběr: vzor závisí na slovním druhu, což je jiné pole. Co se opravdu
                     uloží, rozhoduje PatternValidator při ukládání, ne tenhle seznam. */ ?>
            <datalist id="patterns">
                <?php foreach ($schema->patterns() as $patternCategory => $patterns): ?>
                    <?php foreach ($patterns as $pattern): ?>
                        <option value="<?= h($pattern) ?>" label="<?= h($schema->label('category', $patternCategory)) ?>"></option>
                    <?php endforeach; ?>
                <?php endforeach; ?>
            </datalist>
            <small>Prázdné u slov, která se podle vzoru neskloňují.</small>
        </p>
        <p class="field">
            <label for="gender">Rod</label>
            <?= $form->select('gender', 'gender', $values->choice('gender')) ?>
        </p>
        <p class="field">
            <label>Životnost</label>
            <?= $form->flagField('is_animate', $values->flag('is_animate')) ?>
        </p>
    </div>

    <?= $form->foldOpen('Hláskové a tvarové příznaky', $foldFilled['flags'], count($foldColumns['flags'])) ?>
    <p class="hint">Neuvedeno není totéž co „ne“ — resolvery s tím počítají jinak.</p>

    <div class="grid">
        <p class="field"><label>Pohybné -e</label>
            <?= $form->flagField('has_mobile_e', $values->flag('has_mobile_e')) ?></p>
        <p class="field"><label>Krácení v gen. pl.</label>
            <?= $form->flagField('has_genitive_plural_shortening', $values->flag('has_genitive_plural_shortening')) ?></p>
        <p class="field"><label>Vkladné -e- v gen. pl.</label>
            <?= $form->flagField('has_epenthesis_in_genitive_plural', $values->flag('has_epenthesis_in_genitive_plural')) ?></p>
        <p class="field"><label>Nesklonné</label>
            <?= $form->flagField('is_indeclinable', $values->flag('is_indeclinable')) ?></p>
        <p class="field"><label>Pomnožné</label>
            <?= $form->flagField('is_plural_only', $values->flag('is_plural_only')) ?></p>
        <p class="field"><label>Počitatelné</label>
            <?= $form->flagField('is_countable', $values->flag('is_countable')) ?></p>
        <p class="field"><label>Preferuje krátký tvar</label>
            <?= $form->flagField('prefers_short_form', $values->flag('prefers_short_form')) ?></p>
    </div>
    <?= $form->foldClose() ?>

    <?= $form->foldOpen('Sloveso', $foldFilled['verb'], count($foldColumns['verb']) + 1) ?>

    <div class="grid">
        <p class="field">
            <label for="verb_class">Slovesná třída</label>
            <?php /* Vlastní select místo $form->select(): popiska nese vzory třídy, aby se dala vybrat
                     podle toho, jak sloveso zní, ne podle čísla, které si nikdo nepamatuje. */ ?>
            <select name="verb_class" id="verb_class">
                <option value="">— neuvedeno —</option>
                <?php foreach ($schema->verbClasses() as $class => $info): ?>
                    <option value="<?= h($class) ?>" data-pattern="<?= h($info['pattern']) ?>"<?= $values->choice('verb_class') === $class ? ' selected' : '' ?>>
                        <?= h($schema->label('verb_class', $class)) ?>
                        (<?= h($info['ending']) ?>) — <?= h(implode(', ', $info['examples'])) ?>
                    </option>
                <?php endforeach; ?>
            </select>
            <small>Vyplní vzor, když žádný není. Vyplněný nepřepíše — psát ani moci se do třídy zapsat nedají.</small>
        </p>
        <p class="field">
            <label for="aspect">Vid</label>
            <?= $form->select('aspect', 'aspect', $values->choice('aspect')) ?>
        </p>
        <p class="field">
            <label for="aspect_counterpart">Vidový protějšek</label>
            <input type="text" id="aspect_counterpart" name="aspect_counterpart" value="<?= h($values->text('aspect_counterpart')) ?>">
            <small>Nech prázdné, když protějšek nemá — u sloves pohybu prefixace vid netvoří.</small>
        </p>
        <p class="field wide">
            <label for="aktionsart">Způsob slovesného děje</label>
            <?= $form->select('aktionsart', 'aktionsart', $values->choice('aktionsart')) ?>
            <small>Není to jemnější vid: většina sloves do žádné skupiny nepatří a prázdno znamená
                nezařazeno, ne „žádný“. Skupina vid určuje — (a)–(r) dokonavé, (s)–(y) nedokonavé —
                a <code>validate</code> to hlídá. Když se skupina význam od významu liší, nech tohle
                prázdné a zapiš ji níž, po významech.</small>
        </p>
        <p class="field">
            <label for="reflexive_type">Reflexivita</label>
            <?= $form->select('reflexive_type', 'reflexive_type', $values->choice('reflexive_type', 'None'), allowEmpty: false) ?>
        </p>
        <p class="field">
            <label for="base_verb_lemma">Odvozeno ze slovesa</label>
            <input type="text" id="base_verb_lemma" name="base_verb_lemma" value="<?= h($values->text('base_verb_lemma')) ?>">
            <small>U dějových substantiv — příjezd ← přijet. Rámec pak dědí.</small>
        </p>
    </div>
    <?= $form->foldClose() ?>

    <?= $form->foldOpen('Vlastní funktor', $foldFilled['inherent'], count($foldColumns['inherent'])) ?>

    <div class="grid">
        <p class="field wide">
            <label for="inherent_functor">Funktor</label>
            <?= $form->select('inherent_functor', 'inherent_functor', $values->choice('inherent_functor')) ?>
            <small>Co slovo přináší do věty samo o sobě: dnes kdy (TWHEN), doma kde (LOC), rychle jak
                (MANN), asi modalitu (MOD), jen ukazuje na jádro (RHEM), ach stojí mimo stavbu věty
                (PARTL). Odvodit to nejde — ze zakončení ne a u příslovce ani z přídavného jména, ze
                kterého vzniklo. Prázdno znamená nezapsáno, ne „žádný“; generátor pak roli potřebuje
                dostat zvenčí. Vyplňuje se jen u příslovcí, částic a citoslovcí — jinde funktor
                rozhoduje rámec slovesa a <code>validate</code> to hlásí jako chybu.</small>
        </p>
    </div>
    <?= $form->foldClose() ?>

    <?= $form->foldOpen('Kmeny', $foldFilled['stems'], count($foldColumns['stems'])) ?>
    <p class="hint">Prázdné je běžný stav — kmen si určí vzor. Vyplňuje se jen to, co vzor netrefí:
        říct se časuje podle 1. třídy a minulý čas přesto tvoří na <code>řek-</code>. Píše se bez
        koncovky a bez pomlčky, a vždycky celý za tohle heslo — u odvozeného slovesa tedy i s
        předponou (<code>odnes</code>, ne <code>nes</code>).</p>
    <p class="hint">Obecný kmen platí pro slovesa i pro podstatná jména. Zbytek políček je slovesný;
        u podstatného jména by se nikdy nepřečetl a <code>validate</code> ho hlásí jako chybu.</p>

    <div class="grid">
        <p class="field">
            <label for="stem">Kmen</label>
            <input type="text" id="stem" name="stem" value="<?= h($values->text('stem')) ?>" class="mono">
            <small>U sloves kmen, ze kterého se odvozují ostatní — nes, ber. U podstatných jmen kmen
                po alternaci — dům → dom, nůž → nož.</small>
        </p>
        <p class="field">
            <label for="present_stem">Kmen přítomný</label>
            <input type="text" id="present_stem" name="present_stem" value="<?= h($values->text('present_stem')) ?>" class="mono">
            <small>Jen když se liší od obecného — moci → můž.</small>
        </p>
        <p class="field">
            <label for="past_stem">Kmen minulý</label>
            <input type="text" id="past_stem" name="past_stem" value="<?= h($values->text('past_stem')) ?>" class="mono">
            <small>říct → řek, jíst → jed. Příčestí je pak řekl, jedl.</small>
        </p>
        <p class="field">
            <label for="future_stem">Kmen budoucí</label>
            <input type="text" id="future_stem" name="future_stem" value="<?= h($values->text('future_stem')) ?>" class="mono">
            <small>Jen u sloves s vlastním budoucím tvarem — jít → půjd.</small>
        </p>
        <p class="field">
            <label for="imperative_stem">Kmen rozkazovací</label>
            <input type="text" id="imperative_stem" name="imperative_stem" value="<?= h($values->text('imperative_stem')) ?>" class="mono">
            <small>být → buď, jíst → jez.</small>
        </p>
        <p class="field">
            <label for="passive_stem">Kmen trpný</label>
            <input type="text" id="passive_stem" name="passive_stem" value="<?= h($values->text('passive_stem')) ?>" class="mono">
            <small>Základ trpného příčestí — dát → dán, vzít → vzat.</small>
        </p>
        <p class="field">
            <label for="infinitive">Infinitiv</label>
            <input type="text" id="infinitive" name="infinitive" value="<?= h($values->text('infinitive')) ?>" class="mono">
            <small>Jen když se liší od lemmatu — říct vedle říci.</small>
        </p>
        <p class="field">
            <label>Tvoří pasivum</label>
            <?= $form->flagField('forms_passive', $values->flag('forms_passive')) ?>
            <small>„Ne“ jen u sloves bez trpného příčestí — moci ho nemá, pomoci má pomožen.</small>
        </p>
    </div>
    <?= $form->foldClose() ?>

    <h2>Valence</h2>

    <div class="grid">
        <p class="field">
            <label for="lexeme_id">Lexém</label>
            <select name="lexeme_id" id="lexeme_id">
                <option value="">— bez valence —</option>
                <option value="new">+ založit nový lexém</option>
                <?php foreach ($lexemes as $lexeme): ?>
                    <option value="<?= $lexeme->id ?>"<?= $values->int('lexeme_id') === $lexeme->id ? ' selected' : '' ?>>
                        <?= h($lexeme->primaryLemma) ?>
                    </option>
                <?php endforeach; ?>
            </select>
            <small>Vidová dvojice sdílí jeden lexém, a tím i rámce.</small>
        </p>
        <?php if ($entry !== null && $entry->lexemeId !== null): ?>
        <p class="field">
            <label>Rámce</label>
            <a class="btn" href="<?= h($url->lexeme($entry->lexemeId)) ?>">Otevřít lexém</a>
        </p>
        <?php endif; ?>
    </div>

    <h2>Původ záznamu</h2>

    <div class="grid">
        <p class="field">
            <label for="source">Zdroj</label>
            <input type="text" id="source" name="source" value="<?= h($values->text('source', 'IJP')) ?>">
            <small>VALLEX ani PDT-Vallex sem nepatří — jsou CC BY-NC-SA.</small>
        </p>
        <p class="field">
            <label>Ověřeno</label>
            <?= $form->flagField('is_verified', $values->int('is_verified', 0)) ?>
        </p>
    </div>

    <p class="field wide">
        <label for="note">Poznámka</label>
        <textarea id="note" name="note" rows="2"><?= h($values->text('note')) ?></textarea>
    </p>

    <div class="actions">
        <button type="submit">Uložit</button>
        <a href="<?= h($url->entries()) ?>">Zpět</a>
    </div>
</form>

<?php if (!$isNew): ?>
    <?= $view->render('lemma/_variants', ['id' => $id, 'variants' => $variants]) ?>

    <?php if ($senses !== []): ?>
        <?= $view->render('lemma/_senses', ['id' => $id, 'senses' => $senses]) ?>
    <?php endif; ?>

    <?= $view->render('lemma/_delete', [
        'id' => $id,
        'entry' => $entry,
        'deleteWarnings' => $deleteWarnings,
    ]) ?>
<?php endif; ?>

<script>
    // Propsání třídy do vzoru. Totéž dělá PHP při ukládání; tohle je proto, aby to bylo vidět dřív.
    (function () {
        var verbClass = document.getElementById('verb_class');
        var pattern = document.getElementById('pattern');

        if (!verbClass || !pattern) {
            return;
        }

        verbClass.addEventListener('change', function () {
            var option = verbClass.options[verbClass.selectedIndex];
            var derived = option ? option.getAttribute('data-pattern') : null;

            // Pojmenovaný vzor je rozhodnutí, které třída přebít nesmí.
            if (derived && (pattern.value === '' || /^trida[1-5]$/.test(pattern.value))) {
                pattern.value = derived;
            }
        });
    })();
</script>
