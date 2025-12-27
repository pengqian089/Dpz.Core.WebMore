$location = Get-Location
$path = $location.Path


$cssSourcePath = [System.IO.Path]::Combine($path,'wwwroot','css')

$cssFiles = @()

foreach($item in Get-ChildItem $cssSourcePath)
{
    if($item.Name -match 'global.min.css')
    {
        continue;
    }
    $cssFiles += $item.FullName
}

Write-Host "--------------------------------" -ForegroundColor yellow
Write-Host "Merge global style" -ForegroundColor yellow
Write-Host "--------------------------------" -ForegroundColor yellow

$inputParameters = [System.String]::Join(" ",$cssFiles)
$outputPath = [System.IO.Path]::Combine($cssSourcePath,'global.min.css')
$execute = "cleancss -o $outputPath $inputParameters --with-rebase --debug"
Invoke-Expression $execute