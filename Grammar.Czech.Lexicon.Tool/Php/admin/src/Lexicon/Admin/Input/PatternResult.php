<?php

declare(strict_types=1);

namespace Lexicon\Admin\Input;

defined('LEXICON_ADMIN') || exit('Tenhle soubor se nespouští přímo.');

/**
 * What came of reading the posted vzor: the value to store, or the sentence to show instead.
 */
final class PatternResult
{
    private function __construct(
        public readonly ?string $value,
        public readonly ?string $error
    ) {
    }

    /**
     * A vzor the category has, or no vzor at all — both are states worth saving.
     */
    public static function accepted(?string $value): self
    {
        return new self($value, null);
    }

    /**
     * A vzor that cannot be stored, and why.
     */
    public static function refused(string $error): self
    {
        return new self(null, $error);
    }

    /**
     * Determines whether the save may go ahead.
     */
    public function isAccepted(): bool
    {
        return $this->error === null;
    }
}
