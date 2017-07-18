(**
SolutionExportDev
=================
*)

#load @"_Config.fsx"
open _Config

open System
open DG.Daxif
open DG.Daxif.Common.Utility

let versionIncrement = 
  if fsi.CommandLineArgs.Length > 1 then getVersionIncrement fsi.CommandLineArgs.[1]
  else VersionIncrement.Revision


// Update solution version
Solution.UpdateVersionNumber(Env.dev, SolutionInfo.name, versionIncrement)

// Export unmanaged
Solution.Export(Env.dev, SolutionInfo.name, Path.Daxif.crmSolutionsFolder, managed = false, extended = true);;

// Export managed
Solution.Export(Env.dev, SolutionInfo.name, Path.Daxif.crmSolutionsFolder, managed = true, extended = true)
