<?php

declare(strict_types=1);

namespace Lexicon\Admin\Input;

defined('LEXICON_ADMIN') || exit('Tenhle soubor se nespouští přímo.');

/**
 * The lookup key a lemma is found under.
 */
final class LemmaKey
{
    /**
     * Computes the key from a lemma.
     *
     * mb_strtolower and not strtolower: the plain one works byte by byte and leaves Á alone, which
     * would produce a key no lookup ever matches — the entry would save, and then simply never be
     * found. The C# validator checks this independently, folding with ToLowerInvariant.
     */
    public static function of(string $lemma): string
    {
        return mb_strtolower(trim($lemma), 'UTF-8');
    }
}
