module DG.Daxif.Modules.Plugin.Main

open DG.Daxif
open DG.Daxif.Modules.Plugin
open DG.Daxif.Common.Utility
open DG.Daxif.Common.InternalUtility

open Domain


/// Main plugin synchronization function
let syncSolution proxyGen dllPath solutionName isolationMode dryRun =
  logVersion log
  log.Info "Action: Plugin synchronization"

  log.Info "Comparing plugins registered in CRM versus those found in your local code"
  let asmLocal, asmReg, pluginsLocal, pluginsReg, prefix = MainHelper.analyze proxyGen dllPath solutionName isolationMode

  match dryRun with
  | false -> 
    MainHelper.performSync (proxyGen()) solutionName prefix asmLocal asmReg pluginsLocal pluginsReg
    
  | true  ->
    log.Info "***** Dry run *****"
    let regTypes, regSteps, regImages, regCustomApis, regReqParams, regRespParams = pluginsReg
    let localTypes, localSteps, localImages, localCustomApiTypes, localCustomApis, localReqParams, localRespParams = pluginsLocal
    match MainHelper.determineOperation asmReg asmLocal with
    | Unchanged, _ -> log.Info "No changes detected to assembly"
    | Create, _ -> log.Info "Would create new assembly"
    | Update, _ -> log.Info "Would update assembly"
    printMergePartition "Types" localTypes regTypes Compare.pluginType log 
    printMergePartition "Steps" localSteps regSteps Compare.step log
    printMergePartition "Images" localImages regImages Compare.image log
    printMergePartition "CustomApis" localCustomApis regCustomApis Compare.api log

  pluginsLocal, pluginsReg
    