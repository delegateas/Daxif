param($installPath, $toolsPath, $package, $project)

$dest = "{0}\" -f $project.Name

write-host "Copying required .dll and .xml files to scripts folder:" $dest

dir "$installPath\lib" -Recurse -include *.dll,*.xml | 
    ? { -not $_.PSIsContainer } | 
    Select -ExpandProperty FullName |
    Copy-Item -ErrorAction SilentlyContinue -Dest $dest

foreach ($dep in $package.DependencySets.Dependencies) {
    $path = "{0}\{1}*\lib" -f (get-item $installPath).parent.FullName, $dep.Id

    dir $path -Recurse -include *.dll,*.xml | 
        ? { -not $_.PSIsContainer } | 
        Select -ExpandProperty FullName |
        Copy-Item -ErrorAction SilentlyContinue -Dest $dest
}