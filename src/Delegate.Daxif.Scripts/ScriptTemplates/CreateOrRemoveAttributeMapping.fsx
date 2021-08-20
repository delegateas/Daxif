(**
Create or Remove AttributeMappings
*)

#load "_Config.fsx"

open _Config
open DG.Daxif

let attributeMappingsToDelete = 
  [|
  ("lead", "account", "telephone1", "telephone1")
  ("lead", "account", "emailaddress1", "emailaddress1")
  |]
 
let attributeMappingsToCreate = 
  [|
  ("lead", "account", "telephone1", "telephone1")
  ("lead", "account", "emailaddress1", "emailaddress1")
  |]

Solution.RemoveAttributeMapping(Env.dev, attributeMappingsToDelete)

Solution.CreateAttributeMapping(Env.dev, attributeMappingsToCreate)