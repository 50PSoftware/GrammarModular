<?php

declare(strict_types=1);

namespace Lexicon\Admin\Read;

defined('LEXICON_ADMIN') || exit('Tenhle soubor se nespouští přímo.');

use Lexicon\Admin\Entity\ValencyFrame;

/**
 * A frame together with where it hangs.
 *
 * The breadcrumb needs the sense and the lexeme, and the diathesis belongs there too: one sense can
 * have several frames, and without it the active and the passive are two pages under one heading.
 */
final class FrameContext
{
    public function __construct(
        public readonly ValencyFrame $frame,
        public readonly ?string $senseLabel,
        public readonly int $lexemeId
    ) {
    }

    /**
     * What to call the sense on screen when it has no label.
     */
    public function displaySenseLabel(): string
    {
        return $this->senseLabel ?? '(bez názvu)';
    }
}
