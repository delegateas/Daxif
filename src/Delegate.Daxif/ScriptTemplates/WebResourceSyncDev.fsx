(**
WebResouresSyncDev
=================
*)

#load @"_Config.fsx"
open _Config

open DG.Daxif

WebResource.Sync(Env.dev, Path.webResourceSrc, XrmSolution.name)
