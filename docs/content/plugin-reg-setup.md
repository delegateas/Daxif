Plugin Registration Setup
=========================

To use the plugin synchronization part of DAXIF#, you need to use 
[this extended Plugin.cs][plugin-gist], instead of the regular one 
(remember to change the namespace to match your project).

The standard way of registering a plugin looks similar to the following:

    [lang=csharp]
    // Standard method:
    base.RegisteredEvents.Add(new Tuple<int, string, string, Action<LocalPluginContext>>(
        40, // Post-operation
        "Update",
        Account.LogicalName,
        new Action<LocalPluginContext>(ExecuteAccountStuff)));


We have introduced a method for registering plugin steps. 
This new method is a more robust, more versatile and more configurable way of 
setting up the plugin step in your code with the use of specific enums 
and classes.

The above registration of a plugin step should be replaced by the new function: 

    [lang=csharp]
    // New Delegate A/S method:
    RegisterPluginStep<Account>(EventOperation.Update, ExecutionStage.PostOperation, ExecuteAccountStuff)


The main function here is `RegisterPluginStep`, which takes an entity type as a
generic type parameter. It also takes the necessary other parameters, which is
the operation (from the `EventOperation` enum), the execution stage 
(from the `ExecutionStage` enum), and the desired function to be used.

What's more with this function, is that it returns an object, which can be 
used to further configure settings such as the method of deployment, 
asynchronous/synchronous, execution order, filtering attributes and even 
adding images!


Here is an example of a plugin step with a bunch of configurations:

    [lang=csharp]
    // New Delegate A/S method:
    RegisterPluginStep<Account>(EventOperation.Update, ExecutionStage.PostOperation, ExecuteAccountStuff)
        .SetDeployment(Deployment.ServerOnly)
        .SetExecutionMode(ExecutionMode.Asynchronous)
        .AddFilteredAttributes(
            x => x.Address1_City,
            x => x.Address1_Country)
        .AddImage(ImageType.PreImage, x => x.Address1_Country);


> **Note:** To register a step on all entites (i.e. for Associate/Dissassociate)
> you need to use the class `AnyEntity` as a generic type parameter for the
> function.


All this allows for a type-safe way of registering plugin steps, as well as
always making sure that what is registered in the plugin code is also 
registered correctly on your CRM solution with the use of DAXIF# plugin sync.

This completely eliminates the need to use the 
[PluginRegistration tool][plugin-reg-tool] for registering plugins.


[plugin-gist]: https://github.com/TomMalow/ExtendedPlugin.cs
[plugin-reg-tool]: https://msdn.microsoft.com/en-us/library/gg309580.aspx
