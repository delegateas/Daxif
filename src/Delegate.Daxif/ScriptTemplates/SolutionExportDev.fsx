(**
SolutionExportDev
=================
*)

#load @"_Config.fsx"
open _Config

open DG.Daxif

// Export unmanaged
Solution.Export(Env.dev, XrmSolution.name, Path.Daxif.crmSolutionsFolder, managed = false, extended = true);;

// Export managed
Solution.Export(Env.dev, XrmSolution.name, Path.Daxif.crmSolutionsFolder, managed = true, extended = true)
