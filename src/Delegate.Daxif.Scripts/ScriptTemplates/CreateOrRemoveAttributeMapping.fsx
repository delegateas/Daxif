(**
Create or Remove AttributeMappings
*)

#load @"_Config.fsx"
open _Config
open DG.Daxif


(*Attribute mappings to delete 
("<entity1>", <entity2>, <attribute1>, <attribute2>)
*)
let attributeMappingsToDelete = 
  [|  
  (* Lead to Account *)
  ("lead", "account", "telephone1", "telephone1")
  ("lead", "account", "emailaddreess1", "emailaddress1")
  |]

  (*Attribute mappings to create 
  ("<entity1>", <entity2>, <attribute1>, <attribute2>)
  *)

let attributeMappingsToCreate = 
  [| 
  (*Lead to Account*)
  ("lead", "account", "dg_mainphone", "telephone1")
  ("lead", "account", "dg_email", "emailaddress1")
  |]

Solution.CreateAttributeMapping(Env.dev, attributeMappingsToCreate)

Solution.RemoveAttributeMapping(Env.dev, attributeMappingsToDelete)