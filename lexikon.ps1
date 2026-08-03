<#
.SYNOPSIS
    Zkratka pro Grammar.Czech.Lexicon.Tool.

.DESCRIPTION
    Obaluje `dotnet run --project Grammar.Czech.Lexicon.Tool --`, což je na každodenní volání dlouhé.
    Argumenty předává beze změny, takže platí všechno, co umí samotný nástroj.

    Cesta k projektu se odvozuje od umístění tohoto skriptu, ne od aktuálního adresáře — dá se tedy
    volat odkudkoli.

.EXAMPLE
    .\lexikon.ps1 validate

    Zkontroluje lexikon. Bez argumentů: cesta k databázi se najde v repozitáři sama.

.EXAMPLE
    $env:LEXICON_API_URL = "https://tvadomena.cz/api/"
    $env:LEXICON_API_TOKEN = "…"
    .\lexikon.ps1 pull

    Stáhne slovník. Adresu ani token není nutné psát na příkazovou řádku — nástroj je bere
    i z proměnných prostředí, a to je bezpečnější: příkazová řádka je vidět v `ps` a zapíše se
    do historie shellu.

.EXAMPLE
    .\lexikon.ps1 dump --out seed-mysql.sql

    Vypíše slovník jako přenositelné INSERTy. `dump` a `export-json` jsou jediné příkazy, kde je
    `--out` povinné — u ostatních se na výchozí cestu přijde.
#>

[CmdletBinding()]
param(
    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]] $Arguments
)

$ErrorActionPreference = 'Stop'

$project = Join-Path $PSScriptRoot 'Grammar.Czech.Lexicon.Tool'

if (-not (Test-Path -LiteralPath $project)) {
    throw "Nenašel jsem $project. Skript patří do kořene repozitáře."
}

if (-not $Arguments) {
    # Bez argumentů vypíše nástroj svou nápovědu a skončí jedničkou; tady je to úmysl, ne chyba.
    $Arguments = @()
}

# Ztlumený build: nástroj se před spuštěním sestaví, a jeho varování jsou tady jen šum přes výstup,
# kvůli kterému se skript volá. Chyby projdou, ty quiet neskrývá.
#
# Jen -v; `dotnet run` nezná --nologo a propustil by ho jako argument nástroje, který by ho ohlásil
# jako neznámý příkaz.
dotnet run --project $project -v q -- @Arguments

exit $LASTEXITCODE
