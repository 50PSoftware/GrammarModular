<?php

declare(strict_types=1);

namespace Lexicon\Admin\Database;

defined('LEXICON_ADMIN') || exit('Tenhle soubor se nespouští přímo.');

use Lexicon\Admin\Config;
use PDO;
use PDOException;
use Throwable;

/**
 * The connection to the lexicon, and the four ways the admin talks to it.
 *
 * Repositories hold the SQL; this holds the connection, the statement handling and the transaction.
 * Constraint failures come back out as IntegrityViolation so that callers can answer them without
 * knowing what SQLSTATE is.
 */
final class Database
{
    private ?PDO $pdo = null;

    public function __construct(private readonly Config $config)
    {
    }

    /**
     * Runs a query and returns every row.
     *
     * @param list<mixed> $parameters
     * @return list<array<string, mixed>>
     */
    public function all(string $sql, array $parameters = []): array
    {
        return $this->execute($sql, $parameters)->fetchAll();
    }

    /**
     * Runs a query and returns the first row, or null.
     *
     * @param list<mixed> $parameters
     * @return array<string, mixed>|null
     */
    public function one(string $sql, array $parameters = []): ?array
    {
        $row = $this->execute($sql, $parameters)->fetch();

        return $row === false ? null : $row;
    }

    /**
     * Runs a statement that returns no rows.
     *
     * @param list<mixed> $parameters
     */
    public function run(string $sql, array $parameters = []): void
    {
        $this->execute($sql, $parameters)->closeCursor();
    }

    /**
     * Runs an insert and returns the identifier the database assigned.
     *
     * @param list<mixed> $parameters
     */
    public function insert(string $sql, array $parameters = []): int
    {
        $this->run($sql, $parameters);

        return (int) $this->pdo()->lastInsertId();
    }

    /**
     * Runs several statements as one, undoing all of them if any fails.
     *
     * A cascade is the reason this exists. Deleting a sense means deleting its realizations, its slots
     * and its frames first, and a failure between two of those steps used to leave the half that had
     * already gone — rows the foreign keys then kept anyone from repairing through the form.
     *
     * @template T
     * @param callable(): T $work
     * @return T
     */
    public function transaction(callable $work): mixed
    {
        $pdo = $this->pdo();

        // Nested calls join the transaction already open instead of starting one MySQL would silently
        // commit on the way in.
        if ($pdo->inTransaction()) {
            return $work();
        }

        $pdo->beginTransaction();

        try {
            $result = $work();
            $pdo->commit();

            return $result;
        } catch (Throwable $exception) {
            $pdo->rollBack();

            throw $exception;
        }
    }

    /**
     * Prepares and runs a statement, translating a constraint failure.
     *
     * @param list<mixed> $parameters
     */
    private function execute(string $sql, array $parameters): \PDOStatement
    {
        try {
            $statement = $this->pdo()->prepare($sql);
            $statement->execute($parameters);

            return $statement;
        } catch (PDOException $exception) {
            if (IntegrityViolation::caused($exception)) {
                throw new IntegrityViolation($exception);
            }

            throw $exception;
        }
    }

    /**
     * Opens the connection on first use.
     */
    private function pdo(): PDO
    {
        if ($this->pdo instanceof PDO) {
            return $this->pdo;
        }

        $dsn = $this->config->require('LEXICON_MYSQL_DSN', 'Chybí LEXICON_MYSQL_DSN. Doplň ho do .env.php.');

        $pdo = new PDO(
            $dsn,
            $this->config->get('LEXICON_MYSQL_USER'),
            $this->config->get('LEXICON_MYSQL_PASSWORD'),
            [
                PDO::ATTR_ERRMODE => PDO::ERRMODE_EXCEPTION,
                PDO::ATTR_EMULATE_PREPARES => false,
                PDO::ATTR_STRINGIFY_FETCHES => false,
                PDO::ATTR_DEFAULT_FETCH_MODE => PDO::FETCH_ASSOC,
            ]
        );

        // Without this the connection can negotiate latin1 and every Czech diacritic is mangled on the
        // way in — silently, because mangled text is still text.
        $pdo->query('SET NAMES utf8mb4')?->closeCursor();

        return $this->pdo = $pdo;
    }
}
