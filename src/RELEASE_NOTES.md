# Release Notes

#### 2.3.1.1 - Aug 18 2016
* Fixed missing license in Github page
* Fixed an error in Diff module and added check if file exist when performing Diff
* Added additional information when importing and solution along with saving XML
  import file even when import fails
* Fixed spelling error in the new data scripts

#### 2.3.1.0 - Aug 15 2016
* Added three new scripts for importing and export of data
* Exposed helper function from CRMData for handling large amount of request
* Added function to delete plugins found in Target but not in Source
* Added check for status of async import job to avoid infinite loop in case job
* Added new import and export of solution which includes synching of view and 
  workflow stage and deleting of plugins found in source but not in target

#### 2.3.0.7 - Jun 21 2016
* Fixed issue by importing big solution files where status is never become 100
  (rounding numbers problem in MS CRM)

#### 2.3.0.6 - Jun 16 2016
* Updated to newest Microsoft.CrmSdk.CoreAssemblies and Microsoft.CrmSdk.CoreTools

#### 2.3.0.5 - May 06 2016
* Added PrimaryIdAttribute and PrimaryNameAttribute for entities on the
  XrmTypeProvider

#### 2.3.0.4 - Apr 05 2016
* Added comments to WorkflowHelper.fs moved ProxyContext function to 
  ServiceProxy.fs and made us of it in WorkflowHelper.fs

#### 2.3.0.3 - Apr 05 2016
* Fixed Version from the XrmTypeProvider
* Updated '.' prefix to '(...)' as they are always showed first in VS
* Added '(All Records)' (light) to the XrmTypeProvider for better scripting

#### 2.3.0.2 - Mar 17 2016
* Added DG.EnsureAssemblies.Standalone.fsx version

#### 2.3.0.1 - Mar 17 2016
* Updated to newest TypeProvider files from [FSharp.TypeProviders.StarterPack](https://github.com/fsprojects/FSharp.TypeProviders.StarterPack).
* Remark: We migth refactor the provider to be a cross-targeting erasing type provider.

#### 2.3.0.0 - Mar 16 2016
* Exposed CrmData module (metadata and CRUD) in order to make F# scripting easier.
* Fixed issue with WebResources always succeding (even if they don't).
* Console will is re-written from previous "blocking" to new based on async 
  agents with a producer / reduce pattern.

#### 2.2.0.12 - Mar 03 2016
* Fixed issue where interfaces documentation where not displayed on the GitHub
  page. FSharp.Literate is sensitive to indentation and recently the .fsi files
  where altered. The markdown notation in the .fsi files was indented resulting 
  in only the first markdown notation which hid everything between two markdown
  notations.

#### 2.2.0.11 - Mar 01 2016
* Upgraded to newest Microsoft.CrmSdk.CoreAssemblies and Microsoft.CrmSdk.CoreTools

#### 2.2.0.10 - Mar 01 2016
* Removed Daxif folder from content in NuGet specs

#### 2.2.0.9 - Mar 01 2016
* Updated PowerShell scripts after updating Daxif script path

#### 2.2.0.8 - Feb 26 2016
* Added the script CountEntites.fsx which counts the entities in the solution

#### 2.2.0.7 - Feb 22 2016
* Fixed path in script files after removel of folder level in 2.2.0.5
* Updated PluginSync to handle user context and accept a guid of a user entity 
  to impersonate as. This enables a plugin to be executed under that user.
  A new [Plugin.cs](https://gist.github.com/TomMalow/b9301e024879639a6918) is requried from the current version and up

#### 2.2.0.6 - Feb 14 2016
* Fixed Log in Config script

#### 2.2.0.5 - Feb 14 2016
* Update Ensure Assemblies to also work with standalone

#### 2.2.0.4 - Feb 12 2016
* Fixed CRMUG in naming

#### 2.2.0.3 - Feb 11 2016
* Fixed mapping name for Blueprint naming convetion 

#### 2.2.0.2 - Feb 10 2016
* Fsharp.Core complains about sigdata file. Updated DG.EnsureAssemblies file

#### 2.2.0.1 - Feb 10 2016
* Updated to Blueprint naming convetion SolutionExtract and SolutionPack script
  files

#### 2.2.0.0 - Feb 07 2016
* Daxif is released under our [Open Source License](http://delegateas.github.io/Delegate.Daxif/LICENSE.html)

#### 2.1.3.6 - Jan 07 2016
* Removed WebUI
* Upgraded Suave to 1.0

#### 2.1.3.5 - Jan 05 2016
* Fixed list issues in the Diff module and updated Setup GitHub page

#### 2.1.3.4 - Dec 29 2015
* Fixed argument sent to XrmContext

#### 2.1.3.3 - Dec 28 2015
* Updated dependencies

#### 2.1.3.2 - Dec 28 2015
* Upgraded Suave to newest version and removed FsPickler dependency

#### 2.1.3.1 - Dec 23 2015
* Updated default values in the `Config` script to match online environments
* Added default values for certain arguments to call of XrmContext

#### 2.1.3.0 - Dec 23 2015
* Added `SolutionUpdateCustomContext` script and corresponding functionality
  (uses XrmContext instead of CrmSvcUtil)
* Updated `SolutionUpdateTsContext` script and corresponding functionality
  (now takes more arguments)

#### 2.1.2.3 - Dec 17 2015
* Upgraded Microsoft.CrmSdk.CoreAssemblies and Microsoft.CrmSdk.CoreTools to newest
  version in order to support MS CRM 2016

#### 2.1.2.2 - Nov 19 2015
* Fixed an issue in workflow activation

#### 2.1.2.1 - Nov 02 2015
* Updated DAXIF# visuals in GitHub page, diff module and WebUI module
* Fixed minor problems in diff module

#### 2.1.2.0 - Oct 12 2015
* Added web interface for daxif and script to start interface in default browser
* Better error message in multiple places

#### 2.1.1.8 - Sep 14 2015
* Fixed issues with Workflow sync

#### 2.1.1.7 - Sep 10 2015
* Updated to FSharp.Core v.3.1.2.1

#### 2.1.1.6 - Sep 10 2015
* Added Visual F# Tools 3.1.2 on build server

#### 2.1.1.5 - Sep 08 2015
* Added missing library dependencies to NuGet

#### 2.1.1.4 - Sep 08 2015
* Added solution import report (Excel XML) for when import fails

#### 2.1.1.3 - Sep 01 2015
* Added method to activate/deactivate workflows
* Added solution diff feature

#### 2.1.1.2 - Jul 21 2015
* Fixed issues with Data In / Out module

#### 2.1.1.1 - Jul 21 2015
* Additional changes to timeouts for various Crm calls

#### 2.1.1.0 - Jul 21 2015
* Added Workflow sync
* Added script for Workflow sync
* Increased timeout for various CRM calls
* Fixed a delete issue in Plugin sync
* Fixed Update TypeScript Context script

#### 2.1.0.1 - Jul 16 2015
* Fixed a logging issue

#### 2.1.0.0 - Jul 16 2015
* Changed PluginSync to use a new way of registering plugins. 
  [See more here](plugin-reg-setup.html).
* DisplayName for synchronized WebResources is now just the filename and not the entire path.

#### 2.0.0.5 - Jul 07 2015
* Updated folder name in NuGet package (install.ps1 and uninstall.ps1)

#### 2.0.0.4 - Jul 07 2015
* Updated to Microsoft.CrmSdk.CoreAssemblies.7.1.0

#### 2.0.0.3 - Jul 07 2015
* Still not working so moving againg to Prime [Portable.Licensing.Prime 1.1.0.1](https://www.nuget.org/packages/Portable.Licensing.Prime/)

#### 2.0.0.2 - Jul 07 2015
* Fixing NuGet version issues with [Portable.Licensing 1.1.0](https://www.nuget.org/packages/Portable.Licensing/)

#### 2.0.0.1 - Jul 03 2015
* Initial rename succeed (still need to change to idiomatic names for module functions)

#### 1.3.0.34 - Jul 03 2015
* Moved back to [Portable.Licensing 1.1.0](https://www.nuget.org/packages/Portable.Licensing/) as now VS built don't break
* Getting ready to v.2.0 (renaming and possible a new nuget package)

#### 1.3.0.33 - Apr 28 2015
* Increased timeout for PublishAll request to 10 minutes

#### 1.3.0.32 - Mar 17 2015
* Updated importSolution due to MS CRM 2011 doesn't recognize ExecuteAsyncRequest and ExecuteAsyncResponse
* Optimized performance for dataUpdateState
* Optimized performance for reassignAllRecords
* Optimized performance for dataImport
* Optimized performance for dataReassignOwner

#### 1.3.0.31 - Mar 12 2015
* Updated .NuGet dependencies to Microsoft.CrmSdk.CoreAssemblies.7.0.1

#### 1.3.0.30 - Mar 12 2015
* Updated to Microsoft.CrmSdk.CoreAssemblies.7.0.1

#### 1.3.0.29 - Mar 06 2015
* Added more explicit error messages

#### 1.3.0.28 - Mar 03 2015
* Updated DG.XrmOrg.XrmSolution.SolutionExtract script file to comply with TFS/Git

#### 1.3.0.27 - Feb 26 2015
* Added order for import of data based on alphabetical order
* Fixed legacy createdon for templates

#### 1.3.0.26 - Feb 23 2015
* Added throttle to dataExport and dataExportDelta

#### 1.3.0.25 - Feb 10 2015
* Remark: Packages 1.3.0.21 - 1.3.0.24 are defect (no files add to the package). Please don't use those packages.

#### 1.3.0.24 - Feb 09 2015
* Updated to [Portable.Licensing 1.1.0.1](https://www.nuget.org/packages/Portable.Licensing/1.1.0.1/)

#### 1.3.0.23 - Feb 09 2015
* Replaced [Portable.Licensing 1.1.0](https://www.nuget.org/packages/Portable.Licensing/) with [Portable.Licensing 1.1.0](https://www.nuget.org/packages/Portable.Licensing/)

#### 1.3.0.22 - Feb 09 2015
* Issues with MS CRM SDK (7.0.0 to 7.0.0.1)

#### 1.3.0.21 - Feb 09 2015
* Issues with the XrmTypeProvider made us downgrade runtime from 4.3.1.0 to 4.3.0.0

#### 1.3.0.20 - Jan 27 2015
* Optimized performance for dataAssociationImport
* Incremented timeout for all ServiceProxies

#### 1.3.0.19 - Jan 27 2015
* Added dataReassignOwner to Data module to support legacy ownerid

#### 1.3.0.18 - Jan 25 2015
* Updated to Microsoft Dynamics CRM 2015 SDK core assemblies 7.0.0.1

#### 1.3.0.17 - Jan 22 2015
* Added support for GIT (default)
* Updated calledId and legacy createdon on data import module

#### 1.3.0.16 - Dec 19 2014
* Added cache of relations metadata for performance optimization

#### 1.3.0.15 - Dec 11 2014
* Added cache of metadata for performance optimization

#### 1.3.0.14 - Dec 11 2014
* Added memoization for faster data import (concurrent thread-safe)

#### 1.3.0.13 - Oct 20 2014
* Updated Pluings Sync fixed issue with order of actions

#### 1.3.0.12 - Oct 24 2014
* Updated NuGet dependencies to newest MS CRM SDK version

#### 1.3.0.11 - Oct 24 2014
* Updated to newest SDK in order to add support for CRM2015

#### 1.3.0.10 - Oct 20 2014
* Updated Pluings Sync with Parallelism and fixed issue with naming convention
  and order of actions (Delete, Create and then Update)

#### 1.3.0.9 - Oct 17 2014
* XrmProvider: Added 'LogicalName', 'SchemaName' and 'All Attributes' properties
  for each entity as well as the 'All Entities' for Metadata.

#### 1.3.0.8 - Oct 15 2014
* Added support for Pluings Sync. Note: [Expand Plugin.cs from MS CRM SDK with the following PluginProcessingSteps() method](https://gist.github.com/gentauro/93af827b91246a380d15)

#### 1.3.0.7 - Oct 06 2014
* Removed TypeScript compile statement from WebResourcesModule.syncSolutionWebResources

#### 1.3.0.6 - Oct 06 2014
* Template files: Updated in order to support the XrmTypeProvider
* MS CRM SDK: Updated to 6.1.1

#### 1.3.0.5 - Sep 30 2014
* XrmProvider: Made AuthenticationProvider argument optional
* XrmProvider: Added domain as an optional argument

#### 1.3.0.4 - Sep 29 2014
* Added script file which uses XrmDefinitelyTyped to generate TypeScript
  declaration files.

#### 1.3.0.3 - Aug 27 2014
* Updated DG.DAXIFsharp.XrmProvider. It is now able to fetch data 
  (entity GUIDs for now), metadata as well as information about 
  the CRM system itself
* Added support for TypeScript web resources

#### 1.3.0.2 - Aug 25 2014
* Removed managed files from the WebResources Sync

#### 1.3.0.1 - Jul 31 2014
* Added export filtering based on user/system views. Kudos to: J. Buthe

#### 1.3.0.0 - Jul 25 2014
* Added initial version of DG.DAXIFsharp.XrmProvider (basic and readonly 
  data/metadata from MS CRM in order to ensure type-safety when given entity
  and/or entity field logical names as parameters)

#### 1.2.0.18 - Jul 23 2014
* Added support for deleting a solution (Ex: 3rd part managed solutions)
* Added support for a delta export based on the <code>modifiedon</code> field
* Added support for enabling/disabling all the plug-in related to a given solution

#### 1.1.0.18 - Jun 30 2014
* Updated DG.XrmOrg.XrmSolution.SolutionUpdateContext.fsx script example

#### 1.1.0.17 - Jun 30 2014
* Added support for Danish (LCID 1030) OptionSets labels

#### 1.1.0.16 - Jun 18 2014
* Added support for OptionSets

#### 1.1.0.15 - Jun 18 2014
* Made changes due to CrmSvcUtil 6.1 will not work with CRM Online

#### 1.1.0.14 - Jun 18 2014
* Fixed error with regard that parent <code>result="success" errorcode="0"</code>
  doesn't mean successful deployment

#### 1.1.0.12 - Jun 13 2014
* Added solution import report (Excel XML)

#### 1.1.0.11 - Jun 11 2014
* Added progress status for solution import of customizations

#### 1.1.0.10 - Jun 06 2014
* Locking files are not optimal with parallelism so <code>LogLevel.File</code> is from now
  @deprecated. In order to collect console output to a file(s), follow this
  guidelines: [Redirecting Error Messages from Command Prompt: STDERR/STDOUT)](http://webcache.googleusercontent.com/search?q=cache:https://support.microsoft.com/en-us/kb/110930)

#### 1.1.0.9 - Jun 05 2014
* Updated DG.EnsureAssemblies.fsx to always overwrite DAXIF# assemblies

#### 1.1.0.8 - Jun 05 2014
* Updated to handle ExecuteAsyncResponse for solution import in CRM online

#### 1.1.0.7 - Jun 04 2014
* Fixed webressource sync. Only publish if changes

#### 1.1.0.6 - Jun 04 2014
* Fixed issues with Silverlight files (.xap)

#### 1.1.0.5 - May 28 2014
* Fixed parameters when calling CrmSvcUtil.exe
* Refactored some of the internal code

#### 1.1.0.4 - May 13 2014
* Fixed that nuget pkgs don't copy files on-restore

#### 1.1.0.3 - May 13 2014
* Fixed some issues with regard to the scripting files

#### 1.1.0.2 - May 12 2014
* Initial release