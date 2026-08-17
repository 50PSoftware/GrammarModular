<?php

declare(strict_types=1);

namespace Lexicon\Admin\Database;

defined('LEXICON_ADMIN') || exit('Tenhle soubor se nespouští přímo.');

use PDOException;
use RuntimeException;

/**
 * A write the database refused on a constraint: a duplicate unique key, a foreign key with nothing
 * behind it, a CHECK that did not hold.
 *
 * Its own type because it is the one database failure the admin can answer usefully — it always means
 * the person entering data needs to hear a sentence about the data, not a server error. Catching
 * PDOException and comparing getCode() to '23000' was the same test written out at five call sites.
 */
final class IntegrityViolation extends RuntimeException
{
    public const SQLSTATE = '23000';

    public function __construct(PDOException $cause)
    {
        parent::__construct($cause->getMessage(), 0, $cause);
    }

    /**
     * Determines whether a PDO failure is a constraint violation.
     */
    public static function caused(PDOException $exception): bool
    {
        return $exception->getCode() === self::SQLSTATE;
    }
}
