(**
SolutionUpdateTsContext
=====================

Libraries
---------

Config information *)
#load @"DG.XrmOrg.XrmSolution.Config.fsx"

module cfg = DG.XrmOrg.XrmSolution.Config

(** Open libraries for use *)
open DG.Daxif.Modules

(**
DAXIF# operations
-----------------

Generate TypeScript Context *)
let dts = cfg.tools + @"DG\XrmDefinitelyTyped\XrmDefinitelyTyped.exe"
let loc = cfg.webresources + @"\..\typings\XRM"

Solution.updateTypeScriptContext
  cfg.wsdlDev' loc
  cfg.authType cfg.usrDev cfg.pwdDev cfg.domainDev
  dts cfg.log


  [ // Includes the entities in the following solutions
    cfg.solution 
  ]
    
  [ // Logical names of additional entities to include
    // eg.: "systemuser"
  ] 
            
  [ // Additional arguments for XrmDefinitelyTyped
  ] 