﻿module internal DG.Daxif.Modules.Info.InfoHelper

open DG.Daxif
open DG.Daxif.Common

let version' proxyGen = 
  let p = proxyGen()
  CrmDataInternal.Info.version p
