open System

// TODO: replace __ with crm instance name

let pubPrefix = @"dg"
let pubName = @"delegateas"
let pubDisplay = @"Delegate A/S"
let solutionName = @"XrmOrg"
let solDisplay = @"XrmOrg"
let usr = @"admin@__.onmicrosoft.com"
let pwd =  @"pass@word1"
let domain = @""
let wsdl = Uri(@"https://__.crm4.dynamics.com/XRMServices/2011/Organization.svc")
let organization = "__"

let root = __SOURCE_DIRECTORY__ 
let resourceRoot = root + @"\Resources\"
let workflowDll = @"ILMerged.Delegate.Delegate.UnitTest1.Workflow"
let workflowPath = resourceRoot + workflowDll + ".dll"
let pluginDll = @"ILMerged.Delegate.Delegate.UnitTest1.Plugins"
let pluginPath = resourceRoot + pluginDll + ".dll"