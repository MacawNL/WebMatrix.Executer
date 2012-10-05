$executingScriptDirectory = Split-Path -Path $MyInvocation.MyCommand.Definition -Parent
pushd $executingScriptDirectory
git add Readme.md
git commit -m "Updated the documentation"
git push
[System.Diagnostics.Process]::Start("https://github.com/MacawNL/WebMatrix.Executer/blob/master/Readme.md")
[System.Diagnostics.Process]::Start("http://documentup.com/MacawNL/WebMatrix.Executer/recompile")
Write-Output "Sleeping for 5 second to wait for recompile of Readme.md at DocumentUp.com"
Start-Sleep -s 5
$wc = New-Object System.Net.WebClient
$wc.DownloadString("http://documentup.com/MacawNL/WebMatrix.Executer") > $executingScriptDirectory\..\WebMatrix.Executer.gh-pages\WebMatrix.Executer\index.html
cd $executingScriptDirectory\..\WebMatrix.Executer.gh-pages\WebMatrix.Executer
git add index.html
git commit -m "Updated DocumentUp version of Readme.rd"
git push
popd

[System.Diagnostics.Process]::Start("http://macawnl.github.com/WebMatrix.Executer/")
