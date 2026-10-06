# Strings-epic audit. Read-only.
#   .\strings-audit.ps1 -Module CampaignMechanics/Crafting -OwnedPrefix 'enchant|priest|artisan|refine'
# Reports ids a module's code asks for that tor_strings.xml lacks, and ids under the module's
# prefixes that nothing names as a literal. The second list is candidates only: ids built by
# concatenation ("tor_x_" + culture) will appear there and must be checked by hand.
param(
    [Parameter(Mandatory)] [string] $Module,
    [string] $OwnedPrefix
)

$root = Resolve-Path "$PSScriptRoot\..\..\.."
$xmlIds = [regex]::Matches((Get-Content -Raw "$root\ModuleData\tor_strings.xml"), '<string id="([^"]+)"') |
    ForEach-Object { $_.Groups[1].Value }
$ids = [System.Collections.Generic.HashSet[string]]::new([string[]]$xmlIds)
$bases = [System.Collections.Generic.HashSet[string]]::new([string[]]($xmlIds | ForEach-Object { $_.Split('.')[0] }))

$dupes = $xmlIds | Group-Object | Where-Object Count -gt 1 | ForEach-Object Name
"tor_strings.xml: $($xmlIds.Count) ids, $(@($dupes).Count) duplicated $($dupes -join ', ')"

$call = [regex]'(TORTextHelper\.Get\w+|GameTexts\.FindText|GameTexts\.TryGetText)\(\s*"([^"]+)"'
$refs = @{}
Get-ChildItem "$root\CSharpSourceCode\$Module" -Recurse -Filter *.cs | ForEach-Object {
    $n = 0
    foreach ($line in Get-Content $_.FullName) {
        $n++
        foreach ($m in $call.Matches($line)) { $refs[$m.Groups[2].Value] += @("$($_.Name):$n") }
    }
}

"`nReferenced but absent (str_* ids are native and expected here):"
$refs.Keys | Sort-Object | Where-Object { -not $ids.Contains($_) -and -not $bases.Contains($_) } |
    ForEach-Object { "  $_  $($refs[$_] -join ' ')" }

if ($OwnedPrefix) {
    $corpus = (Get-ChildItem "$root\CSharpSourceCode", "$root\GUI", "$root\ModuleData" -Recurse -Include *.cs, *.xml |
        Where-Object Name -ne 'tor_strings.xml' | ForEach-Object { Get-Content -Raw $_.FullName }) -join "`n"
    "`nOwned ids never named as a literal (candidates):"
    $bases | Where-Object { $_ -match "^tor_($OwnedPrefix)" } | Sort-Object |
        Where-Object { -not $corpus.Contains("`"$_`"") } | ForEach-Object { "  $_" }
}
