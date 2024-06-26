# Release Notes
### 5.6.0 - April 25 2024
* Update assembly comparison when determining if we want to update the assembly. Compare the version of the local assembly to the version currently registered, if the local version is higher (by semver rules) than the registered version, update the assembly even if the hash matches. (@mkholt)
* Changed the hashing functionality to no longer load in the project files and dependencies, but instead taking a SHA1 sum of the assembly file. This removes the dependency on the project files, and makes the hashing more reliable. (@mkholt)

### 5.5.1 - May 23 2023
* Fixed 'useUniqueInstance' parameter to GetCrmServiceClient() with default value: 'false'. - Developer now has to actively enable multiple instances of service client. This is due to potential authentication issues if too many simultaneous tasks are spawning connections (such as WebResourceSync functionality) (@bo-stig-christensen)

### 5.5.0 - May 22 2023
* Added support for Custom APIs. You can use the CustomAPI.cs file from Delegate.Daxif.Scripts to activate the functionality. (@magesoe, @mlRosenquist)
* Added 'useUniqueInstance' parameter to GetCrmServiceClient() with default value: 'true'. - This enables a single script to correctly connect to multiple environments, while still having the option to set the parameter to 'false' in order to optimize any existing scripts that may be adversely affected regarding performance by this change. (@bo-stig-christensen)
* Added 'publishAfterSync' parameter to WebResourceSync with default value: 'true'. - This ensures that it is possible to omit PublishAll and explicitly adding such a step in a pipeline afterwards or use a different toolkit for publishing (@bo-stig-christensen)

### 5.4.0 - October 10 2022
* Fix impersonation in plugin registration (@skovlund)
* Add support for resx files (@skovlund)

### 5.2.0 - September 27 2021
* Added support for attribute mapping (@mlRosenquist)
* Removed notification when XrmMockup metadata generation has completed - notification has been moved to XrmMockup as per 1.8.2 (@elaurs)

### 5.1.1 - May 21 2021
* Added automatic retry to publish all and configurable timeout

### 5.1.0 - October 27 2020
* Update Extended Import to delete workflow post-import instead of pre-import
* Fix issue with incorrect verion number in Delegate.Daxif.dll file

### 5.0.0 - September 24 2020
* Split Daxif and the ScriptTemplates to sepatare packages. This will make it easier to update daxif dll without accedentially reseting scripts. New Daxif Script packages "Delegate.Daxif.Scripts"
* Update Extended solution to match web resources on Name and not Guid

### 4.8.1 - September 24 2020
* Env.executeProcess now correctly passes option url arg

### 4.8.0 - Septebmer 17 2020
* Add support for async solution export
* Add Modern Flow support in extended solution
* Fix missing argument in extended solution post-import interface

### 4.7.2 - September 04 2020
* Fixed issue when calling XrmContext or XrmDefinitelyTyped with connection string

### 4.7.1 - June 17 2020
* Added Connection String Authentication method

### 4.7.0 - April 17 2020
* Added option to reassign workflows to same owner as export using extended solution

### 4.6.2 - March 27 2020
* fix issue with publish after import

### 4.6.1 - March 23 2020
* Fix an issue in pre- and post-import extended solution when referencing the path to the solution
* Add new build server script for publishing all customizations

### 4.6.0 - March 20 2020
* Now possible to perform extended solution export and import across multiple scripts
* Update Solution.Import with new option to not publish customizations after import
* Exposed steps that are nested within the Solution.Import
    * ExtendedSolution.Export - Extend an existing exported solution with extended informationer from an instance
    * ExtendedSolution.Pre-import - Deletes depricated/replaced customizations that will block a normal import
    * ExtendedSolution.Post-import - Deletes any remaining 
    * Solution.PublishAll - Publish all customizations

### 4.5.6 - March 12 2020
* Fixed issue where extended solution failed to delete plugins before import of new assembly

### 4.5.5 - February 24 2020
* Changed timeout for CrmServiceClient to 1 hour

### 4.5.4 - February 14 2020
* Added more verbose logging when deleting things with extended solution

### 4.5.3 - February 13 2020
* Fixed errors where proxy authentication was assumed for internal methods

### 4.5.2 - February 13 2020
* Fixed error when using new Auth methods with DAXIF functions

### 4.5.1 - February 13 2020
* Fixed casing error when reading fsi arguments

### 4.5.0 - February 11 2020
* Added support for client secret authentication

### 4.4.0 - January 24 2020
* Added export/import dynamic diff

### 4.3.0 - Semptember 24 2019
* Added support for MFA

### 4.2.14 - September 12 2019
* WorkflowSync will now look for a matching assembly according to both name and version

### 4.2.13 - September 12 2019
* Hack to circumvent bug in MS concerning "flow"

### 4.2.12 - August 28 2019
* Added support for syncing web resources to a patch solution while taking into account web resources in the base solution. Use the optional parameter patchSolutionName when calling WebResource.Sync

### 4.2.11 - August 06 2019
* Fix an issue where Daxif did not publish unmanaged solution after import

### 4.2.10 - August 05 2019
* Fix an issue where updating an image through plugin sync resulted in a null reference exception

### 4.2.9 - July 26 2019
* Run all of extended solutions before crashing, in order to see multiple errors

### 4.2.8 - July 26 2019
* Fail on error in general but especially in solution export

### 4.2.7 - June 26 2019
* Added isolation mode option to workflow registration (@ferodom)

### 4.2.6 - March 19 2019
* Increased proxy timeout to 59 minutes

### 4.2.5 - March 05 2019
* Added native support for generating XrmMockup Metadata

### 4.2.4 - September 14 2018
* Added FSharp.Core as NuGet reference

### 4.2.3 - September 13 2018
* Removed SolutionImportDev and SolutionImportTest script templates - use SolutionImportArg instead
* Removed dash naming limitation from local web resources

#### 4.2.2 - June 21 2018
* Changed Extended Solution to use assembly name as primary key (instead of assembly guid)

#### 4.2.1 - June 04 2018
* Added default reference to bin\Microsoft.Crm.Sdk.Proxy.dll in _Config.fsx
* Skip trying to load types returned from Assembly.GetTypes() that cannot be loaded
* Update plugin synchronization to filter out invalid plugins
* Update Daxif to .NET Framework 4.6.2

#### 4.2.0 - April 03 2018
* Renamed DGSolution to ExtendedSolution
* Renamed dgSolution.xml to ExtendSolution.xml
* Readded logging for when assembly is created or updated in plugin synchronization
* Fix bug in Extended Solution where records was not deleted or status of records was not updated on import

#### 4.1.1 - February 02 2018
* Fixed the expected type of statecode and statuscode for attributes in ViewExtender

#### 4.1.0 - February 02 2018
* Added support for duplicate naming for option's label in ViewExtender

#### 4.0.0 - December 07 2017
* Fixed error in solution extract https://github.com/delegateas/Daxif/issues/8
* Improved performance of ViewExtender https://github.com/delegateas/Daxif/issues/12
* Fixed error with filtering in ViewExtender where attributes not on savedquery were invalid
* Added condition with no arguments to ViewExtender and renamed AddCondition to AddCondition1


#### 3.1.9 - October 30 2017
* Added support for SVG files in Dynamics365 9.0 and newer

#### 3.1.8 - October 13 2017
* Fixed error when using proxy in Playground.fsx
* Fixed versioning to support Dynamics365 9.0 and future releases

#### 3.1.7 - September 14 2017
* Added better character handling to the ViewGuid generator in ViewExtender
* State and Status is no longer send along when updating a view in ViewExtender

#### 3.1.6 - August 21 2017
* Changed RemoveLink to RemoveRelatedColumn and initFilter to InitFilter

#### 3.1.5 - August 21 2017
* Fixed error with standard ViewExtender script

#### 3.1.4 - August 18 2017
* Fixed error with View.addLink, View.addLinkFirst and View.addLinkLast
* Renamed several end points to give a more logical name

#### 3.1.3 - August 18 2017
* Added ViewExtender to Daxif

#### 3.1.2 - August 8 2017
* Removed invalid line in `SolutionImportArg.fsx` script

#### 3.1.1 - August 7 2017
* Fixed `Workflow.Sync`, when adding dll for the first time

#### 3.1.0 - August 4 2017
* Added `Environment.executeProcess`, which can pass on credential information to an external process
* Added `getCredentialsFromPath` function to CredentialManagement

#### 3.0.2 - July 18 2017
* Updated some of the solution import/export scripts
* Fixed extended solution export using an invalid path in certain cases

#### 3.0.1 - July 18 2017
* ***New API***
* New `Environment` type which stores connection information
* Local credential storage added. Credentials can now be encrypted and stored locally in `.daxif` files.
* Completely reworked starting scripts
* Restructured the entire project internally
* Reworked most of the plugin synchronization
* Now checks if a given plugin assembly is up-to-date before synchronizing
* WebresourceSync does not expect a folder named `<prefix>_<solutionName>` anymore, but instead synchronizes the specified folder and prefixes the files automatically
* Added solution version increment function

#### 2.4.1 - Mar 14 2017
* Udpated DGSolution to also include WebResources and Workflows (processes)
* Changed DGSolution to read solution name from solution.xml and use Guid to compare objects to persist between environments where possible
* Fixed a bug in DGSolution where plugins with same name would cause problems. Changed to use Guid instead of names
* Fixed a bug in `Solution.Import` where the progression would report 0% for a long time and then jump to ~95%
* Fixed a bug where multiple call to `Solution.exportWithDGSolution` would cause an error

#### 2.4.0 - Feb 19 2017
* Added new functionality `Plugins.syncSolutionWhitelist`
* Removed a lot of try/catch in main methods to allow exceptions to be caught outside of Daxif
* Removed Agent usage in ConsoleLogger

#### 2.3.3.0 - Jan 23 2017
* Refactored plugin sync to be more readable and reduce the amount calls to CRM 
  improving the synchronization time
* Added new method `syncSolution` for synching plugins that takes in an enum specifing the 
  isolationmode of the plugin assembly
* Added two new import functions for importing entities with or without references. 
  `importWithoutReferences` imports entities withouth any `EntityReferences` attributes
  and `importReferences` that only entities with EntityReferences attributes
* Reduceed the number of ExecuteMultipleRequest Calls to 10 call made in parallel in DataHelper 
  to on-premise environments due to limitation with CRM on-premise

#### 2.3.2.0 - Sep 29 2016
* Changed description of synchronized plugins/workflow activities to include who did it and when it was performed
* Fixed low timespan for timeout when using with on-premise environment
* Updated import to use status of a async job state for determining if the job is completed or failed
* Added function to merge two solutions
* Fixed exportview to use new serilization union
* Added better error message when plugin synch fails to get plugin configuration from dll through invocation.
* Added new function to publish duplicate detection rules given by a list of 
  names of the duplicate detection rules to publish in a target environment

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
* Updated SolutionExtract script file to comply with TFS/Git

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
* Updated SolutionUpdateContext.fsx script example

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
