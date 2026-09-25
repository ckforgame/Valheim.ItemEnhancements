<#
.SYNOPSIS
    Convenience alias for export-dist.ps1
#>
[CmdletBinding()]
param(
    [Parameter(ValueFromRemainingArguments = $true)]
    $ArgsList
)

& "$PSScriptRoot\export-dist.ps1" @ArgsList