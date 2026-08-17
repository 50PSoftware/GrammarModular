<?php

declare(strict_types=1);

namespace Lexicon\Admin\Read;

defined('LEXICON_ADMIN') || exit('Tenhle soubor se nespouští přímo.');

/**
 * An entry that points at another one by lemma rather than by key.
 *
 * aspect_counterpart and base_verb_lemma carry a lemma, not a foreign key, so the entry they name can
 * be deleted out from under them and nothing in the database will say so.
 */
final class Referrer
{
    public function __construct(
        public readonly int $id,
        public readonly string $lemma,
        public readonly string $via
    ) {
    }
}
