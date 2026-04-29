# Fix Naming Convention Script
# Removes underscore prefixes from private field names in test files

$testFiles = Get-ChildItem -Path "test" -Filter "*Tests.cs" -Recurse

$fixed = 0
$errors = 0

foreach ($file in $testFiles) {
    try {
        $content = Get-Content $file.FullName -Raw
        $originalContent = $content
        
        # Pattern 1: private Type _fieldName = ...
        $content = $content -replace 'private\s+(\w+<[^>]+>|\w+)\s+_(\w+)\s*=', 'private $1 $2 ='
        
        # Pattern 2: private readonly Type _fieldName;
        $content = $content -replace 'private\s+readonly\s+(\w+<[^>]+>|\w+)\s+_(\w+)\s*;', 'private readonly $1 $2;'
        
        # Pattern 3: private readonly Type _fieldName = ...
        $content = $content -replace 'private\s+readonly\s+(\w+<[^>]+>|\w+)\s+_(\w+)\s*=', 'private readonly $1 $2 ='
        
        # Pattern 4: _fieldName usage (with this. prefix)
        $content = $content -replace '(\s+)_(\w+)', '$1$2'
        
        # Pattern 5: Fix "this.fieldName" back to just "fieldName" where appropriate  
        # This pattern needs to be more careful - only fix in constructor assignments
        $content = $content -replace '(\w+)\(([^)]+)\)\s*\{[^}]*this\.(\w+)\s*=\s*\3', '$1($2) { this.$3 = $3'
        
        if ($content -ne $originalContent) {
            Set-Content -Path $file.FullName -Value $content -NoNewline
            Write-Host "Fixed: $($file.FullName)" -ForegroundColor Green
            $fixed++
        }
    }
    catch {
        Write-Host "Error processing $($file.FullName): $_" -ForegroundColor Red
        $errors++
    }
}

Write-Host "`nSummary:" -ForegroundColor Cyan
Write-Host "Files fixed: $fixed" -ForegroundColor Green
Write-Host "Errors: $errors" -ForegroundColor $(if ($errors -eq 0) { "Green" } else { "Red" })
