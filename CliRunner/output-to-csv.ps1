param (
    $rootFolder = "output"
)

dir "$rootFolder/*.json" | 
    foreach { ((gc $_.FullName) -join "") } | 
    ConvertFrom-Json | 
    ConvertTo-Csv -Delimiter "`t" > "$rootFolder.csv"

gc "$rootFolder.csv" | Set-Clipboard