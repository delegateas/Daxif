(**
SolutionUpdateCustomContext
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

Generate System Context *)
let ccs = cfg.tools + @"\DG\XrmContext\XrmContext.exe"
let ctx = cfg.rootFolder + @"\..\..\BusinessDomain"

Solution.updateCustomServiceContext
  cfg.wsdlDev' ctx
  cfg.authType cfg.usrDev cfg.pwdDev cfg.domainDev
  ccs cfg.log
            
    
  [ // Includes the entities in the following solutions
    cfg.solution 
  ]
    
  [ // Logical names of additional entities to include
    // eg.: "systemuser"
  ] 
            
  [ // Additional arguments for XrmContext
    // eg.: ("deprecatedprefix", "ZZ_")
  ]