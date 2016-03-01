param($installPath, $toolsPath, $package, $project)

$dest = "{0}" -f $project.Name

write-host "Cleaning out .dll and .xml files from script folder:" $dest
Remove-Item "$dest\*" -recurse -include *.dll,*.xml -ErrorAction SilentlyContinue
