(**
SolutionCreateDev
=================
*)

#load @"_Config.fsx"
open _Config
open DG.Daxif

Solution.CreatePublisher(Env.dev, Publisher.name, Publisher.displayName, Publisher.prefix)

Solution.Create(Env.dev, XrmSolution.name, XrmSolution.displayName, CrmPublisher.prefix)
