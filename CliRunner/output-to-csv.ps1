param (
    $rootFolder = "output"
)

dir "$rootFolder/*.json" | 
    foreach { ((gc $_.FullName) -join "") } | 
    ConvertFrom-Json | 
    ConvertTo-Csv > "$rootFolder.csv"