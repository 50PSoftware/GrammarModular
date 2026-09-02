<?php

declare(strict_types=1);

namespace Lexicon\Admin\Input;

defined('LEXICON_ADMIN') || exit('Tenhle soubor se nespouští přímo.');

use Lexicon\Admin\Schema;

/**
 * The fields a form posted, coerced into the types the columns hold.
 *
 * The coercions are the ones the columns need and are not interchangeable: an empty text is null and
 * not "", a flag has three states and not two, and a value outside the permitted set becomes null
 * rather than travelling to a column the C# side then fails to parse.
 */
final class FormData
{
    /**
     * @param array<string, mixed> $fields
     */
    public function __construct(
        private readonly array $fields,
        private readonly Schema $schema
    ) {
    }

    /**
     * Reads a text field, turning an empty one into null.
     *
     * Empty and absent are the same thing in a form — a cleared text input posts "" — and the columns
     * distinguish null from the empty string. Storing "" would make a lemma with no pattern compare
     * unequal to one that never had a pattern.
     */
    public function text(string $name): ?string
    {
        $value = trim((string) ($this->fields[$name] ?? ''));

        return $value === '' ? null : $value;
    }

    /**
     * Reads an integer field.
     */
    public function int(string $name, ?int $default = null): ?int
    {
        $value = $this->text($name);

        return $value === null ? $default : (int) $value;
    }

    /**
     * Reads an integer field, never below the floor.
     */
    public function atLeast(string $name, int $floor): int
    {
        return max($floor, $this->int($name, $floor) ?? $floor);
    }

    /**
     * Reads a decimal field, or null when it was left blank.
     */
    public function float(string $name): ?float
    {
        $value = $this->text($name);

        return $value === null ? null : (float) $value;
    }

    /**
     * Reads an identifier posted in a hidden field.
     */
    public function id(string $name): int
    {
        return (int) ($this->fields[$name] ?? 0);
    }

    /**
     * Reads a three-state flag: yes, no, or not recorded.
     *
     * Most of the morphological flags are nullable on purpose. "This noun does not have a mobile e" and
     * "nobody has checked" are different claims, and the resolvers treat them differently.
     */
    public function flag(string $name): ?int
    {
        return match ((string) ($this->fields[$name] ?? '')) {
            '1' => 1,
            '0' => 0,
            default => null,
        };
    }

    /**
     * Reads a checkbox, which says yes or says nothing.
     */
    public function checkbox(string $name): int
    {
        return $this->flag($name) === 1 ? 1 : 0;
    }

    /**
     * Reads a value that has to be one of the permitted ones.
     *
     * Anything unrecognised becomes null rather than being written through, so a tampered form cannot
     * put a value into a column the C# side then fails to parse.
     */
    public function enum(string $name, string $column): ?string
    {
        $value = $this->text($name);

        return $value !== null && $this->schema->isPermitted($column, $value) ? $value : null;
    }

    /**
     * Reads a value that has to be one of the permitted ones, with a fallback for the NOT NULL columns.
     */
    public function enumOr(string $name, string $column, string $fallback): string
    {
        return $this->enum($name, $column) ?? $fallback;
    }

    /**
     * Everything that was posted, for putting a refused form back on screen.
     *
     * @return array<string, mixed>
     */
    public function all(): array
    {
        return $this->fields;
    }
}
