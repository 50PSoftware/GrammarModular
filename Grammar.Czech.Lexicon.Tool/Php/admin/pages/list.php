<?php

declare(strict_types=1);

defined('LEXICON_ADMIN') || exit('Tenhle soubor se nespouští přímo.');

/**
 * Seznam hesel s hledáním a stránkováním.
 *
 * Hledá se přes lemma_key, tedy přes tentýž sloupec, kterým se lemma vyhledává za běhu, a dotaz se
 * skládá stejně — mb_strtolower. Hledat přes lemma by při akcentově necitlivé kolaci našlo i to, co
 * se ve skutečnosti hledat nedá.
 */

$query = trim((string) ($_GET['q'] ?? ''));
$page = max(1, (int) ($_GET['strana'] ?? 1));
$offset = ($page - 1) * ADMIN_PAGE_SIZE;

$where = '';
$parameters = [];

if ($query !== '') {
    $where = 'WHERE e.lemma_key LIKE ?';
    $parameters[] = admin_lemma_key($query) . '%';
}

$total = (int) admin_one("SELECT COUNT(*) AS c FROM lemma_entry e $where", $parameters)['c'];

$rows = admin_all(
    "SELECT e.lemma_entry_id, e.lemma, e.category, e.gender, e.pattern, e.aspect, e.is_verified,
            e.lexeme_id,
            (SELECT COUNT(*) FROM lexical_unit u
              JOIN valency_frame f ON f.lu_id = u.lu_id
             WHERE u.lexeme_id = e.lexeme_id) AS frames
       FROM lemma_entry e
       $where
       ORDER BY e.lemma_key
       LIMIT " . ADMIN_PAGE_SIZE . " OFFSET $offset",
    $parameters
);

$pages = max(1, (int) ceil($total / ADMIN_PAGE_SIZE));
?>

<form method="get" class="search">
    <input type="hidden" name="p" value="list">
    <label for="q" class="sr">Hledat heslo</label>
    <input type="search" id="q" name="q" value="<?= h($query) ?>" placeholder="Hledat od začátku lemmatu…">
    <button type="submit">Hledat</button>
    <?php if ($query !== ''): ?>
        <a href="<?= h(admin_url(['p' => 'list'])) ?>">Zrušit</a>
    <?php endif; ?>
</form>

<p class="count"><?= $total ?> <?= $total === 1 ? 'heslo' : ($total < 5 ? 'hesla' : 'hesel') ?></p>

<?php if ($rows === []): ?>
    <p class="empty">Nic tu není. <a href="<?= h(admin_url(['p' => 'lemma', 'id' => 'new'])) ?>">Přidej první heslo.</a></p>
<?php else: ?>
<div class="scroller">
<table>
    <thead>
        <tr>
            <th>Lemma</th>
            <th>Druh</th>
            <th>Rod</th>
            <th>Vzor</th>
            <th>Vid</th>
            <th>Rámce</th>
            <th>Ověřeno</th>
        </tr>
    </thead>
    <tbody>
    <?php foreach ($rows as $row): ?>
        <tr>
            <td><a href="<?= h(admin_url(['p' => 'lemma', 'id' => (int) $row['lemma_entry_id']])) ?>"><?= h((string) $row['lemma']) ?></a></td>
            <td><?= h(LEXICON_ENUMS['category'][$row['category']] ?? (string) $row['category']) ?></td>
            <td><?= h($row['gender'] === null ? '—' : (LEXICON_ENUMS['gender'][$row['gender']] ?? '')) ?></td>
            <td class="mono"><?= h((string) ($row['pattern'] ?? '—')) ?></td>
            <td><?= h($row['aspect'] === null ? '—' : (LEXICON_ENUMS['aspect'][$row['aspect']] ?? '')) ?></td>
            <td>
                <?php if ($row['lexeme_id'] === null): ?>
                    —
                <?php else: ?>
                    <a href="<?= h(admin_url(['p' => 'lexeme', 'id' => (int) $row['lexeme_id']])) ?>"><?= (int) $row['frames'] ?></a>
                <?php endif; ?>
            </td>
            <td><?= ((int) $row['is_verified']) === 1 ? 'ano' : '<span class="muted">ne</span>' ?></td>
        </tr>
    <?php endforeach; ?>
    </tbody>
</table>
</div>

<?php if ($pages > 1): ?>
<nav class="pager">
    <?php for ($index = 1; $index <= $pages; $index++): ?>
        <?php if ($index === $page): ?>
            <span class="now"><?= $index ?></span>
        <?php else: ?>
            <a href="<?= h(admin_url(['p' => 'list', 'q' => $query ?: null, 'strana' => $index])) ?>"><?= $index ?></a>
        <?php endif; ?>
    <?php endfor; ?>
</nav>
<?php endif; ?>
<?php endif; ?>
