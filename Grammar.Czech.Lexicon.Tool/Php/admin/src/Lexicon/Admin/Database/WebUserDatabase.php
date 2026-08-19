<?php

declare(strict_types=1);

namespace Lexicon\Admin\Database;

defined('LEXICON_ADMIN') || exit('Tenhle soubor se nespouští přímo.');

use Lexicon\Admin\Config;
use PDO;

/**
 * A read-only connection to the official website's own database, for one purpose: looking up the
 * account a sign-in is attempted with.
 *
 * A second connection rather than a rename of Database, because it is a different MySQL database on a
 * different account — the website keeps its own `user` table, and the admin never writes to it. There
 * is deliberately no run()/insert()/transaction() here: a connection that can only SELECT cannot be
 * made to write by a bug two calls away.
 */
final class WebUserDatabase
{
    private ?PDO $pdo = null;

    public function __construct(private readonly Config $config)
    {
    }

    /**
     * Finds the website account with this email address, or null.
     *
     * @return array{id: int, password: string, name: ?string, mail: string, roles: string, verified: int}|null
     */
    public function findByEmail(string $mail): ?array
    {
        $row = $this->pdo()
            ->prepare('SELECT id, password, name, mail, roles, verified FROM user WHERE mail = ?');
        $row->execute([$mail]);
        $result = $row->fetch();

        return $result === false ? null : $result;
    }

    private function pdo(): PDO
    {
        if ($this->pdo instanceof PDO) {
            return $this->pdo;
        }

        $dsn = $this->config->require(
            'LEXICON_WEB_MYSQL_DSN',
            'Chybí LEXICON_WEB_MYSQL_DSN. Bez něj se nedá ověřit přihlášení proti webu.'
        );

        $pdo = new PDO(
            $dsn,
            $this->config->get('LEXICON_WEB_MYSQL_USER'),
            $this->config->get('LEXICON_WEB_MYSQL_PASSWORD'),
            [
                PDO::ATTR_ERRMODE => PDO::ERRMODE_EXCEPTION,
                PDO::ATTR_EMULATE_PREPARES => false,
                PDO::ATTR_STRINGIFY_FETCHES => false,
                PDO::ATTR_DEFAULT_FETCH_MODE => PDO::FETCH_ASSOC,
            ]
        );

        // Bez tohohle se spojení může domluvit na latin1 a diakritika ve jménu dorazí poškozená.
        $pdo->query('SET NAMES utf8mb4')?->closeCursor();

        return $this->pdo = $pdo;
    }
}
