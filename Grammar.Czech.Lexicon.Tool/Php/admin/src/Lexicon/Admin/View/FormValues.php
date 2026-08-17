<?php

declare(strict_types=1);

namespace Lexicon\Admin\View;

defined('LEXICON_ADMIN') || exit('Tenhle soubor se nespouští přímo.');

use Lexicon\Admin\Input\OldInput;

/**
 * What to put in a form field: what the editor last typed, or failing that what is stored.
 *
 * The field names of the entry form are the column names, so one lookup covers both sources.
 */
final class FormValues
{
    /**
     * @param array<string, mixed> $stored
     */
    public function __construct(
        private readonly OldInput $old,
        private readonly array $stored
    ) {
    }

    /**
     * The raw value, from the refused form if there is one.
     */
    public function raw(string $name, mixed $default = null): mixed
    {
        return $this->old->value($name, $this->stored[$name] ?? $default);
    }

    /**
     * The value as text, empty when there is none.
     */
    public function text(string $name, string $default = ''): string
    {
        $value = $this->raw($name);

        return $value === null ? $default : (string) $value;
    }

    /**
     * The value as a whole number.
     */
    public function int(string $name, int $default = 0): int
    {
        $value = $this->raw($name);

        return $value === null || $value === '' ? $default : (int) $value;
    }

    /**
     * A three-state flag, keeping "not recorded" apart from "no".
     */
    public function flag(string $name): ?int
    {
        $value = $this->raw($name);

        return $value === null || $value === '' ? null : (int) $value;
    }

    /**
     * A value that has to be one of a fixed set, falling back when nothing is recorded.
     */
    public function choice(string $name, ?string $default = null): ?string
    {
        $value = $this->raw($name);

        return $value === null || $value === '' ? $default : (string) $value;
    }
}
