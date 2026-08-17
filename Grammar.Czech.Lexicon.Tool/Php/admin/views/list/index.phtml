<?php

declare(strict_types=1);

defined('LEXICON_ADMIN') || exit('Tenhle soubor se nespouští přímo.');

/**
 * Seznam hesel s hledáním a stránkováním.
 *
 * @var \Lexicon\Admin\Read\Page $page
 * @var string|null $query
 * @var \Lexicon\Admin\View\Url $url
 * @var \Lexicon\Admin\Schema $schema
 */
?>

<form method="get" action="<?= h($url->entries()) ?>" class="search">
    <label for="q" class="sr">Hledat heslo</label>
    <input type="search" id="q" name="q" value="<?= h((string) $query) ?>" placeholder="Hledat od začátku lemmatu…">
    <button type="submit">Hledat</button>
    <?php if ($query !== null): ?>
        <a href="<?= h($url->entries()) ?>">Zrušit</a>
    <?php endif; ?>
</form>

<p class="count"><?= $page->total ?> <?= $page->total === 1 ? 'heslo' : ($page->total < 5 ? 'hesla' : 'hesel') ?></p>

<?php if ($page->rows === []): ?>
    <p class="empty">Nic tu není. <a href="<?= h($url->newEntry()) ?>">Přidej první heslo.</a></p>
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
    <?php foreach ($page->rows as $row): ?>
        <tr>
            <td><a href="<?= h($url->entry($row->id)) ?>"><?= h($row->lemma) ?></a></td>
            <td><?= h($schema->label('category', $row->category)) ?></td>
            <td><?= h($schema->label('gender', $row->gender)) ?></td>
            <td class="mono"><?= h($row->pattern ?? '—') ?></td>
            <td><?= h($schema->label('aspect', $row->aspect)) ?></td>
            <td>
                <?php if ($row->lexemeId === null): ?>
                    —
                <?php else: ?>
                    <a href="<?= h($url->lexeme($row->lexemeId)) ?>"><?= $row->frames ?></a>
                <?php endif; ?>
            </td>
            <td><?= $row->isVerified === 1 ? 'ano' : '<span class="muted">ne</span>' ?></td>
        </tr>
    <?php endforeach; ?>
    </tbody>
</table>
</div>

<?php if ($page->count > 1): ?>
<nav class="pager">
    <?php foreach ($page->numbers() as $number): ?>
        <?php if ($number === null): ?>
            <span class="muted">…</span>
        <?php elseif ($number === $page->number): ?>
            <span class="now"><?= $number ?></span>
        <?php else: ?>
            <a href="<?= h($url->entries($query, $number)) ?>"><?= $number ?></a>
        <?php endif; ?>
    <?php endforeach; ?>
</nav>
<?php endif; ?>
<?php endif; ?>
