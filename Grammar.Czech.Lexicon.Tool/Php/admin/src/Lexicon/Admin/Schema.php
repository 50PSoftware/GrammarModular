<?php

declare(strict_types=1);

namespace Lexicon\Admin;

defined('LEXICON_ADMIN') || exit('Tenhle soubor se nespouští přímo.');

use InvalidArgumentException;

/**
 * Typed access to the column map in ../schema-tables.php.
 *
 * The constants stay where they are and in the shape they are in. They are shared with the API, which
 * loads no classes, and PhpSchemaParityTests parses that file as text — it expects `const
 * LEXICON_TABLES = [`, single quotes and a closing `];` in the first column. Moving them into this
 * class would break seven tests and buy nothing; this is a reader, not a new home.
 *
 * Everything the admin needs to know about tables, columns, permitted values and inflection patterns
 * comes through here, so the map is loaded from one place and a mistyped column name fails with a
 * named exception rather than an undefined index.
 */
final class Schema
{
    /**
     * All tables, parent tables first.
     *
     * @return list<string>
     */
    public function tables(): array
    {
        return array_keys(LEXICON_TABLES);
    }

    /**
     * Every column of a table, in the order the schema declares them.
     *
     * @return list<string>
     */
    public function columns(string $table): array
    {
        if (!array_key_exists($table, LEXICON_TABLES)) {
            throw new InvalidArgumentException("Tabulka '$table' ve schématu není.");
        }

        return LEXICON_TABLES[$table];
    }

    /**
     * Every column of a table except the surrogate key the database assigns.
     *
     * This is the list an INSERT and an UPDATE are built from, so it exists here instead of being
     * written out a second time next to the SQL — that copy was in lemma.php and had to be kept in
     * step with the map by hand.
     *
     * The key is dropped only when the first column is named after the table with an _id suffix, which
     * is what a surrogate key looks like throughout this schema. lexicon_meta, keyed by meta_key, keeps
     * all of its columns.
     *
     * @return list<string>
     */
    public function writableColumns(string $table): array
    {
        $columns = $this->columns($table);

        if ($columns !== [] && str_ends_with($columns[0], '_id')) {
            return array_values(array_slice($columns, 1));
        }

        return $columns;
    }

    /**
     * Determines whether a table has a column.
     */
    public function hasColumn(string $table, string $column): bool
    {
        return in_array($column, $this->columns($table), true);
    }

    /**
     * The permitted values of a constrained column, keyed by what goes in the database.
     *
     * @return array<string, string> Value to Czech label.
     */
    public function enum(string $column): array
    {
        if (!array_key_exists($column, LEXICON_ENUMS)) {
            throw new InvalidArgumentException("Sloupec '$column' nemá výčet hodnot.");
        }

        return LEXICON_ENUMS[$column];
    }

    /**
     * Determines whether a value is one the column accepts.
     */
    public function isPermitted(string $column, string $value): bool
    {
        return array_key_exists($value, $this->enum($column));
    }

    /**
     * The Czech label of a value, falling back to the value itself.
     *
     * The fallback matters for rows written before a value was removed from the map: the list has to
     * render them as something, and the stored value says more than an empty cell.
     */
    public function label(string $column, ?string $value, string $whenNull = '—'): string
    {
        if ($value === null) {
            return $whenNull;
        }

        return $this->enum($column)[$value] ?? $value;
    }

    /**
     * The inflection patterns of every category that inflects by pattern.
     *
     * @return array<string, list<string>>
     */
    public function patterns(): array
    {
        return LEXICON_PATTERNS;
    }

    /**
     * The patterns a category accepts, or null when the category does not inflect by pattern.
     *
     * Null and an empty list are different answers: an absent category means a pattern on such a word
     * is an error, not an empty choice.
     *
     * @return list<string>|null
     */
    public function patternsFor(string $category): ?array
    {
        return LEXICON_PATTERNS[$category] ?? null;
    }

    /**
     * The verb classes, each with the pattern it conjugates by and how it is recognised.
     *
     * @return array<string, array{pattern: string, ending: string, examples: list<string>}>
     */
    public function verbClasses(): array
    {
        return LEXICON_VERB_CLASSES;
    }

    /**
     * The pattern a verb class conjugates by, or null when the class is unknown.
     */
    public function patternForVerbClass(?string $verbClass): ?string
    {
        if ($verbClass === null) {
            return null;
        }

        return LEXICON_VERB_CLASSES[$verbClass]['pattern'] ?? null;
    }
}
