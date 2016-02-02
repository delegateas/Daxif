param($installPath, $toolsPath, $package, $project)

$sampleFolder = "Daxif"
$dest = "{0}\{1}" -f $project.Name, $sampleFolder

write-host "Cleaning out .dll and .xml files from script folder:" $dest
Remove-Item "$dest\*" -recurse -include *.dll,*.xml -ErrorAction SilentlyContinue
