<?php

declare(strict_types=1);

namespace Lexicon\Admin\Input;

defined('LEXICON_ADMIN') || exit('Tenhle soubor se nespouští přímo.');

use Lexicon\Admin\Schema;

/**
 * Checks a posted vzor against the patterns its category has.
 */
final class PatternValidator
{
    public function __construct(private readonly Schema $schema)
    {
    }

    /**
     * Reads the vzor and decides whether it can be stored.
     *
     * Unlike an enum, an unknown value is not quietly dropped to null. A vzor is the one field the
     * inflection services cannot work around: a null means "nothing declines this word", which is a
     * legitimate state, while a typo silently turned into null would look like a deliberate choice and
     * leave a word that never inflects with nobody knowing why. So this reports instead, and the caller
     * refuses the save.
     *
     * Case is folded because all three inflection services look the pattern up through Pattern.
     * ToLower(). The value is stored as typed — the C# side folds again on the way in.
     */
    public function check(?string $value, string $category): PatternResult
    {
        if ($value === null) {
            return PatternResult::accepted(null);
        }

        $label = $this->schema->label('category', $category, $category);
        $patterns = $this->schema->patternsFor($category);

        if ($patterns === null) {
            return PatternResult::refused(
                'Vzor „' . $value . '“ nelze uložit: ' . $label
                . ' se podle vzoru neskloňuje. Nech pole prázdné.'
            );
        }

        $wanted = mb_strtolower($value, 'UTF-8');

        foreach ($patterns as $pattern) {
            if ($wanted === mb_strtolower($pattern, 'UTF-8')) {
                return PatternResult::accepted($value);
            }
        }

        return PatternResult::refused(
            'Vzor „' . $value . '“ neexistuje. Slovní druh ' . $label . ' má tyhle: '
            . implode(', ', $patterns) . '.'
        );
    }
}
