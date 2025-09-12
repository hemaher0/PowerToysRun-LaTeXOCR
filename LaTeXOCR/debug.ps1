# this script uses [gsudo](https://github.com/gerardog/gsudo)

Push-Location
Set-Location $PSScriptRoot

sudo powershell {
	Start-Job { Stop-Process -Name PowerToys* } | Wait-Job > $null

	# change this to your PowerToys installation path
        $ptPath = "$env:LOCALAPPDATA\PowerToys"
	$projectName = 'LaTeXOCR'
	$safeProjectName = 'LaTeXOCR'
	$debug = '.\bin\x64\Debug\net9.0-windows'
	$dest = "$env:LOCALAPPDATA\Microsoft\PowerToys\PowerToys Run\Plugins\$projectName"
	$files = @(
		"Community.PowerToys.Run.Plugin.$safeProjectName.deps.json",
		"Community.PowerToys.Run.Plugin.$safeProjectName.dll",
		'plugin.json',
		'Images',
        'python'
	)

	Set-Location $debug
	mkdir $dest -Force -ErrorAction Ignore | Out-Null
	Copy-Item $files $dest -Force -Recurse

	& "$ptPath\PowerToys.exe"
}

Pop-Location
