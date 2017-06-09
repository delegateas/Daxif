(**
SolutionExtract
===============
*)
#load @"_Config.fsx"
open _Config
open DG.Daxif

Solution.Extract(Path.Daxif.unmanagedSolution, Path.SolPack.customizationsFolder, Path.SolPack.xmlMappingFile, Path.SolPack.projFile)
