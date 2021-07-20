(**
Create or Remove AttributeMappings
*)

#load "_Config.fsx"

open _Config
open DG.Daxif
open DG.Daxif.Modules.Solution.AttributeMapping

let attributeMappingsToDelete : AttributeMapping [] =
    [| { sourceEntity = "lead"
         targetEntity = "account"
         sourceAttr = "telephone1"
         targetAttr = "telephone1" };
       { sourceEntity = "lead"
         targetEntity = "account"
         sourceAttr = "emailaddress1"
         targetAttr = "emailaddress1" }; |]

let attributeMappingsToCreate : AttributeMapping [] =
    [| { sourceEntity = "lead"
         targetEntity = "account"
         sourceAttr = "telephone1"
         targetAttr = "telephone1" };
       { sourceEntity = "lead"
         targetEntity = "account"
         sourceAttr = "emailaddress1"
         targetAttr = "emailaddress1" }; |]

Solution.CreateAttributeMapping(Env.dev, attributeMappingsToCreate)

Solution.RemoveAttributeMapping(Env.dev, attributeMappingsToDelete)
