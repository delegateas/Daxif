(**
SolutionPack
============
*)

#load @"_Config.fsx"
open _Config
open DG.Daxif
open DG.Daxif.Common

// Unmanaged
Solution.Pack(
  Utility.addEndingToFilename Path.Daxif.unmanagedSolution "_packed", 
  Path.SolPack.customizationsFolder, 
  Path.SolPack.xmlMappingFile, 
  managed = false)

// Managed
Solution.Pack(
  Utility.addEndingToFilename Path.Daxif.managedSolution "_packed", 
  Path.SolPack.customizationsFolder, 
  Path.SolPack.xmlMappingFile, 
  managed = true
)
