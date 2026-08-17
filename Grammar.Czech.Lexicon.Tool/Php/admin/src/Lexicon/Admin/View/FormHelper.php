<?php

declare(strict_types=1);

namespace Lexicon\Admin\View;

defined('LEXICON_ADMIN') || exit('Tenhle soubor se nespouští přímo.');

use Lexicon\Admin\Schema;
use Lexicon\Admin\Security\CsrfToken;

/**
 * The form controls the admin repeats on every page.
 *
 * They return markup as a string rather than printing, so a template can put one inside an attribute
 * or a table cell without caring where the output is going.
 */
final class FormHelper
{
    public function __construct(
        private readonly Schema $schema,
        private readonly CsrfToken $csrf
    ) {
    }

    /**
     * The hidden field that says this form came from a page this session rendered.
     */
    public function csrf(): string
    {
        return '<input type="hidden" name="csrf" value="' . h($this->csrf->value()) . '">';
    }

    /**
     * Renders a select over the permitted values of a column.
     *
     * The id defaults to the name and can be given separately, for the pages that render the same
     * field once per row. Two elements sharing an id is not a cosmetic problem there: a label points at
     * the first one, so clicking the second row's label focuses the first row's select.
     */
    public function select(
        string $name,
        string $column,
        ?string $selected,
        bool $allowEmpty = true,
        ?string $id = null
    ): string {
        $html = '<select name="' . h($name) . '" id="' . h($id ?? $name) . '">';

        if ($allowEmpty) {
            $html .= '<option value="">— neuvedeno —</option>';
        }

        foreach ($this->schema->enum($column) as $value => $label) {
            $isSelected = $value === $selected ? ' selected' : '';
            $html .= '<option value="' . h($value) . '"' . $isSelected . '>' . h($label) . '</option>';
        }

        return $html . '</select>';
    }

    /**
     * Renders a three-state flag as a group of radio buttons.
     */
    public function flagField(string $name, ?int $value): string
    {
        $options = [null => 'neuvedeno', 1 => 'ano', 0 => 'ne'];
        $html = '<span class="flag">';

        foreach ($options as $option => $label) {
            $checked = $option === $value ? ' checked' : '';
            $id = $name . '_' . ($option === null ? 'x' : $option);

            $html .= '<label for="' . h($id) . '"><input type="radio" id="' . h($id) . '" name="' . h($name)
                . '" value="' . ($option === null ? '' : $option) . '"' . $checked . '> ' . h($label) . '</label>';
        }

        return $html . '</span>';
    }

    /**
     * Opens a foldable section of a form.
     *
     * A <details> rather than anything scripted, because the fields inside a collapsed one are still in
     * the form and still post. A section hidden with display:none would do the same, but one built out
     * of tabs or removed from the DOM would silently drop half the entry on save.
     */
    public function foldOpen(string $title, int $filled, int $total): string
    {
        // Vyplněná sekce se otevře sama. Nevyplněná zůstane složená a řekne to číslem, aby nešlo splést
        // „nic tam není“ s „nekoukal jsem dovnitř“.
        $badge = $filled === 0
            ? '<span class="muted">prázdné</span>'
            : '<span class="badge">' . $filled . ' z ' . $total . '</span>';

        return '<details class="fold"' . ($filled > 0 ? ' open' : '') . '><summary>' . h($title) . $badge
            . '</summary>';
    }

    /**
     * Closes a foldable section.
     */
    public function foldClose(): string
    {
        return '</details>';
    }

    /**
     * Counts how many of the named columns the row actually says something in.
     *
     * Drives which sections of the entry form open by themselves. A collapsed section that turns out to
     * hold data is worse than no collapsing at all — the value is invisible and looks absent — so a
     * section opens whenever it has something to show.
     *
     * A flag set to "no" counts: 0 is a claim, and only null is the gap.
     *
     * @param array<string, mixed> $row
     * @param list<string> $columns
     */
    public function filledCount(array $row, array $columns): int
    {
        $filled = 0;

        foreach ($columns as $column) {
            $value = $row[$column] ?? null;

            if ($value !== null && $value !== '') {
                $filled++;
            }
        }

        return $filled;
    }
}
