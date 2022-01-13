﻿module DG.Daxif.Modules.Plugin.Main

open DG.Daxif
open DG.Daxif.Modules.Plugin
open DG.Daxif.Common
open DG.Daxif.Common.Utility
open DG.Daxif.Common.InternalUtility

open Domain


/// Main plugin synchronization function
let syncSolution proxyGen projectPath dllPath solutionName isolationMode ignoreOutdatedAssembly dryRun =
  logVersion log
  log.Info "Action: Plugin synchronization"

  log.Info "Comparing plugins registered in CRM versus those found in your local code"
  let asmLocal, asmReg, pluginsLocal, pluginsReg, prefix = MainHelper.analyze proxyGen projectPath dllPath solutionName isolationMode ignoreOutdatedAssembly

  match dryRun with
  | false -> 
    MainHelper.performSync (proxyGen()) solutionName prefix asmLocal asmReg pluginsLocal pluginsReg
    
  | true  ->
    log.Info "***** Dry run *****"
    let regTypes, regSteps, regImages, regCustomApis = pluginsReg
    let localTypes, localSteps, localImages, localCustomApiTypes, localCustomApis = pluginsLocal
    printMergePartition "Types" localTypes regTypes Compare.pluginType log 
    printMergePartition "Steps" localSteps regSteps Compare.step log
    printMergePartition "Images" localImages regImages Compare.image log
    printMergePartition "CustomApis" localCustomApis regCustomApis Compare.api log

  pluginsLocal, pluginsReg
    