namespace DG.Daxif.Modules

open DG.Daxif
open DG.Daxif.HelperModules
open DG.Daxif.HelperModules.Common
open DG.Daxif.HelperModules.Common.Utility
open Suave.Successful

module Diff = 
  let solution source target log = 
    let log' = ConsoleLogger.ConsoleLogger log
    log'.WriteLine(LogLevel.Info, daxifVersion)
    log'.WriteLine(LogLevel.Info, @"Delta between source and target solutions.")
    log'.WriteLine(LogLevel.Verbose, @"Source: " + source)
    log'.WriteLine(LogLevel.Verbose, @"Target: " + target)
    DiffHelper.solution' source target log'
    log'.WriteLine
      (LogLevel.Info, 
        @"The delta between source and target solutions was created successfully")
  
  let solutionApp source target log = 
    let log' = ConsoleLogger.ConsoleLogger log
    log'.WriteLine(LogLevel.Info, daxifVersion)
    log'.WriteLine(LogLevel.Info, @"Delta between source and target solutions.")
    log'.WriteLine(LogLevel.Verbose, @"Source: " + source)
    log'.WriteLine(LogLevel.Verbose, @"Target: " + target)
    let app = DiffHelper.solutionApp source target log'
    log'.WriteLine
      (LogLevel.Info, 
        @"The delta between source and target solutions was created successfully")
    app
  
  let summary source target log = 
    let log' = ConsoleLogger.ConsoleLogger log
    log'.WriteLine(LogLevel.Info, daxifVersion)
    log'.WriteLine(LogLevel.Info, @"Delta between source and target solutions.")
    log'.WriteLine(LogLevel.Verbose, @"Source: " + source)
    log'.WriteLine(LogLevel.Verbose, @"Target: " + target)
    let csv = DiffHelper.Summary.build source target log'
    log'.WriteLine
      (LogLevel.Info, 
        @"CSV file with between source and target solutions was created successfully")
    csv