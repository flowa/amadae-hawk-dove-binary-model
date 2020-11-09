dir output/*.json | 
    foreach { ((gc $_.FullName) -join "") } | 
    ConvertFrom-Json | 
    ConvertTo-Csv > output.csv