<?php

declare(strict_types=1);

namespace Lexicon\Admin\Entity;

defined('LEXICON_ADMIN') || exit('Tenhle soubor se nespouští přímo.');

/**
 * Reads a column out of a database row without losing the difference between null and a value.
 *
 * A plain (int) cast turns null into 0 and (string) turns it into "", which is exactly the distinction
 * the nullable flags carry — "no" against "not recorded". These two casts keep null null.
 */
final class Value
{
    /**
     * A whole number, or null when the column holds nothing.
     */
    public static function int(mixed $value): ?int
    {
        return $value === null ? null : (int) $value;
    }

    /**
     * Text, or null when the column holds nothing.
     */
    public static function text(mixed $value): ?string
    {
        return $value === null ? null : (string) $value;
    }
}
