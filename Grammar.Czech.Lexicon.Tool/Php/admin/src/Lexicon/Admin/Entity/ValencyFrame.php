<?php

declare(strict_types=1);

namespace Lexicon\Admin\Entity;

defined('LEXICON_ADMIN') || exit('Tenhle soubor se nespouští přímo.');

/**
 * A valency frame: what a sense requires of the sentence around it, in one diathesis.
 */
final class ValencyFrame
{
    public function __construct(
        public readonly ?int $id,
        public readonly int $luId,
        public readonly string $kind,
        public readonly string $diathesis,
        public readonly int $isDefault,
        public readonly string $reflexiveType,
        public readonly int $slotCount = 0
    ) {
    }

    /**
     * @param array<string, mixed> $row
     */
    public static function fromRow(array $row): self
    {
        return new self(
            Value::int($row['frame_id'] ?? null),
            (int) $row['lu_id'],
            (string) $row['kind'],
            (string) $row['diathesis'],
            (int) ($row['is_default'] ?? 0),
            (string) ($row['reflexive_type'] ?? 'None'),
            (int) ($row['slots'] ?? 0)
        );
    }
}
