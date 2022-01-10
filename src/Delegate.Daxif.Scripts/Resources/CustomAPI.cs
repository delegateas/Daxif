
namespace DG.DelegateAS.DAXIFdev.Plugins
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Globalization;
    using System.Linq;
    using System.ServiceModel;
    using System.Linq.Expressions;
    using Microsoft.Xrm.Sdk;

    // MainCustomAPIConfig      : UniqueName, IsFunction, EnabledForWorkflow, AllowedCustomProcessingStepType, BindingType, BoundEntityLogicalName
    // ExtendedCustomAPIConfig  : PluginType, OwnerId, OwnerType, IsCustomizable, IsPrivate, ExecutePrivilegeName, Description
    // RequestParameterConfig   : 
    // ResponsePropertyConfig   : 
    using MainCustomAPIConfig = System.Tuple<string, int, int, int, int, string>;
    using ExtendedCustomAPIConfig = System.Tuple<string, string, string, bool, int, string, string>;
    using RequestParameterConfig = System.Tuple<string>; // TODO
    using ResponsePropertyConfig = System.Tuple<string>; // TODO

    /// <summary>
    /// Base class for all CustomAPIs.
    /// </summary>    
    public class CustomAPI : IPlugin
    {
        protected class LocalPluginContext
        {
            internal IServiceProvider ServiceProvider
            {
                get;

                private set;
            }

            internal IOrganizationService OrganizationService
            {
                get;

                private set;
            }

            // Delegate A/S added:
            internal IOrganizationService OrganizationAdminService
            {
                get;

                private set;
            }

            internal IPluginExecutionContext PluginExecutionContext
            {
                get;

                private set;
            }

            internal ITracingService TracingService
            {
                get;

                private set;
            }

            private LocalPluginContext()
            {
            }

            internal LocalPluginContext(IServiceProvider serviceProvider)
            {
                if (serviceProvider == null)
                {
                    throw new ArgumentNullException("serviceProvider");
                }

                // Obtain the execution context service from the service provider.
                this.PluginExecutionContext = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));

                // Obtain the tracing service from the service provider.
                this.TracingService = (ITracingService)serviceProvider.GetService(typeof(ITracingService));

                // Obtain the Organization Service factory service from the service provider
                IOrganizationServiceFactory factory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));

                // Use the factory to generate the Organization Service.
                this.OrganizationService = factory.CreateOrganizationService(this.PluginExecutionContext.UserId);

                // Delegate A/S added: Use the factory to generate the Organization Admin Service.
                this.OrganizationAdminService = factory.CreateOrganizationService(null);
            }

            internal void Trace(string message)
            {
                if (string.IsNullOrWhiteSpace(message) || this.TracingService == null)
                {
                    return;
                }

                if (this.PluginExecutionContext == null)
                {
                    this.TracingService.Trace(message);
                }
                else
                {
                    this.TracingService.Trace(
                        "{0}, Correlation Id: {1}, Initiating User: {2}",
                        message,
                        this.PluginExecutionContext.CorrelationId,
                        this.PluginExecutionContext.InitiatingUserId);
                }
            }
        }

        /// <summary>
        /// Gets the List of events that the plug-in should fire for. Each List
        /// Item is a <see cref="System.Tuple"/> containing the Pipeline Stage, Message and (optionally) the Primary Entity. 
        /// In addition, the fourth parameter provide the delegate to invoke on a matching registration.
        /// </summary>
        protected Action<LocalPluginContext> RegisteredEvent { get; private set; }

        /// <summary>
        /// Gets or sets the name of the child class.
        /// </summary>
        /// <value>The name of the child class.</value>
        protected string ChildClassName { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CustomAPI"/> class.
        /// </summary>
        /// <param name="childClassName">The <see cref="" cred="Type"/> of the derived class.</param>
        internal CustomAPI(Type childClassName)
        {
            this.ChildClassName = childClassName.ToString();
        }


        /// <summary>
        /// Executes the plug-in.
        /// </summary>
        /// <param name="serviceProvider">The service provider.</param>
        /// <remarks>
        /// For improved performance, Microsoft Dynamics CRM caches plug-in instances. 
        /// The plug-in's Execute method should be written to be stateless as the constructor 
        /// is not called for every invocation of the plug-in. Also, multiple system threads 
        /// could execute the plug-in at the same time. All per invocation state information 
        /// is stored in the context. This means that you should not use global variables in plug-ins.
        /// </remarks>
        public void Execute(IServiceProvider serviceProvider)
        {
            if (serviceProvider == null)
            {
                throw new ArgumentNullException("serviceProvider");
            }

            // Construct the Local plug-in context.
            LocalPluginContext localcontext = new LocalPluginContext(serviceProvider);

            localcontext.Trace(string.Format(CultureInfo.InvariantCulture, "Entered {0}.Execute()", this.ChildClassName));
            localcontext.Trace(localcontext.PluginExecutionContext.Stage.ToString());

            try
            {
                // Iterate over all of the expected registered events to ensure that the CustomAPI
                // has been invoked by an expected event
                // For any given plug-in event at an instance in time, we would expect at most 1 result to match.
                Action<LocalPluginContext> action = this.RegisteredEvent;

                if (action != null)
                {
                    localcontext.Trace(string.Format(
                        CultureInfo.InvariantCulture,
                        "{0} is firing for Entity: {1}, Message: {2}\n",
                        this.ChildClassName,
                        localcontext.PluginExecutionContext.PrimaryEntityName,
                        localcontext.PluginExecutionContext.MessageName));

                    action.Invoke(localcontext);

                    // now exit - if the derived plug-in has incorrectly registered overlapping event registrations,
                    // guard against multiple executions.
                    return;
                }
            }
            catch (FaultException<OrganizationServiceFault> e)
            {
                localcontext.Trace(string.Format(CultureInfo.InvariantCulture, "Exception: {0}", e.ToString()));

                // Handle the exception.
                throw;
            }
            finally
            {
                localcontext.Trace(string.Format(CultureInfo.InvariantCulture, "Exiting {0}.Execute()", this.ChildClassName));
            }
        }

        // Delegate A/S added:
        /// <summary>
        /// The methods exposes the RegisteredEvents as a collection of tuples
        /// containing:
        /// - The full assembly name of the class containing the RegisteredEvents
        /// - The Pipeline Stage
        /// - The Event Operation
        /// - Logical Entity Name (or empty for all)
        /// This will allow to instantiate each plug-in and iterate through the 
        /// PluginProcessingSteps in order to sync the code repository with 
        /// MS CRM without have to use any extra layer to perform this operation
        /// </summary>
        /// <returns></returns>
        /// 

        //public IEnumerable<Tuple<string, int, string, string>> PluginProcessingSteps()
        //{
        //    var className = this.ChildClassName;
        //    foreach (var events in this.RegisteredEvents)
        //    {
        //        yield return new Tuple<string, int, string, string>
        //            (className, events.Item1, events.Item2, events.Item3);
        //    }
        //}

        #region CustomAPI retrieval
        /// <summary>
        /// Made by Delegate A/S
        /// Get the CustomAPI configurations.
        /// </summary>
        /// <returns>API</returns>
        public Tuple<MainCustomAPIConfig, ExtendedCustomAPIConfig, IEnumerable<RequestParameterConfig>, IEnumerable<ResponsePropertyConfig>> GetCustomAPIConfig()
        { // TODO
            //var className = this.ChildClassName;
            var config = this.CustomAPIConfig;
            return new Tuple<MainCustomAPIConfig, ExtendedCustomAPIConfig, IEnumerable<RequestParameterConfig>, IEnumerable<ResponsePropertyConfig>>(
                new MainCustomAPIConfig(config._Name, config._IsFunction, config._EnabledForWorkflow, config._AllowedCustomProcessingStepType, config._BindingType, config._BoundEntityLogicalName),
                new ExtendedCustomAPIConfig(config._PluginType, "", "", config._IsCustomizable, config._IsPrivate, config._ExecutePrivilegeName, config._Description),
                config.GetRequestParameters(),
                config.GetResponseProperties());
        }


        protected CustomAPIConfig RegisterCustomAPI(string name, Action<LocalPluginContext> action)
        {
            var apiConfig = new CustomAPIConfig(name);

            if (this.CustomAPIConfig != null || this.RegisteredEvent != null)
            {
                throw new InvalidOperationException("The CustomAPI class does not support multiple registrations");
            }
            this.CustomAPIConfig = (ICustomAPIConfig)apiConfig;
            this.RegisteredEvent = action;
            return apiConfig;
        }


        //private ICustomAPIConfig apiConfig;
        private ICustomAPIConfig CustomAPIConfig { get; set; }
        #endregion
    }

    #region CustomAPIConfig made by Delegate A/S
    interface ICustomAPIConfig
    {
        int _AllowedCustomProcessingStepType { get; }
        int _BindingType { get; }
        string _BoundEntityLogicalName { get; }
        string _Description { get; }
        string _DisplayName { get; }
        string _ExecutePrivilegeName { get; }
        bool _IsCustomizable { get; }
        int _IsFunction { get; }
        int _IsPrivate { get; }
        string _Name { get; }
        string _PluginType { get; }
        string _UniqueName { get; }
        int _EnabledForWorkflow { get; }
        IEnumerable<RequestParameterConfig> GetRequestParameters();
        IEnumerable<ResponsePropertyConfig> GetResponseProperties();
    }

    public class CustomAPIConfig : ICustomAPIConfig
    { // TODO
        public int _AllowedCustomProcessingStepType { get; private set; }
        public int _BindingType { get; private set; }
        public string _BoundEntityLogicalName { get; private set; }
        public string _Description { get; private set; } // Remove?
        public string _DisplayName { get; private set; } // Remove?
        public string _ExecutePrivilegeName { get; private set; }
        public bool _IsCustomizable { get; private set; }
        public int _IsFunction { get; private set; }
        public int _IsPrivate { get; private set; }
        public string _Name { get; private set; }
        public string _UniqueName { get; private set; }
        public string _PluginType { get; private set; }
        public int _EnabledForWorkflow { get; private set; }

        public Collection<CustomAPIRequestParameter> _RequestParameters = new Collection<CustomAPIRequestParameter>();
        public Collection<CustomAPIResponseProperty> _ResponseProperties = new Collection<CustomAPIResponseProperty>();

        public CustomAPIConfig(string name)
        {
            this._Name = name;
            this._DisplayName = name;
            this._UniqueName = name;
            this._IsFunction = 0;
            this._EnabledForWorkflow = 0;
            this._AllowedCustomProcessingStepType = 0; // None
            this._BindingType = 0; // Global
            this._BoundEntityLogicalName = "";

            this._PluginType = null;
            this._IsCustomizable = false;
            this._IsPrivate = 0;
            this._ExecutePrivilegeName = null; // TODO
            this._Description = null; // TODO
        }

        public CustomAPIConfig AllowCustomProcessingStep(AllowedCustomProcessingStepType type)
        {
            this._AllowedCustomProcessingStepType = (int)type;
            return this;
        }

        public CustomAPIConfig Bind<T>(BindingType bindingType) where T : Entity
        {
            this._BindingType = (int)bindingType;
            this._BoundEntityLogicalName = Activator.CreateInstance<T>().LogicalName;
            return this;
        }

        public CustomAPIConfig MakeFunction()
        {
            this._IsFunction = 1;
            return this;
        }
        public CustomAPIConfig MakePrivate()
        {
            this._IsPrivate = 1;
            return this;
        }

        public CustomAPIConfig EnableForWorkFlow()
        {
            this._EnabledForWorkflow = 1;
            return this;
        }

        public CustomAPIConfig EnableCustomization()
        {
            this._IsCustomizable = true;
            return this;
        }

        public CustomAPIConfig AddRequestParameter(string name)
        {
            this._RequestParameters.Add(new CustomAPIRequestParameter(name));
            return this;
        }

        public CustomAPIConfig AddResponseProperty(string name)
        {
            this._ResponseProperties.Add(new CustomAPIResponseProperty(name));
            return this;
        }

        public IEnumerable<RequestParameterConfig> GetRequestParameters()
        {
            foreach (var requestParameter in this._RequestParameters)
            {
                yield return new RequestParameterConfig(requestParameter.Name); // TODO: Populate
            }
        }

        public IEnumerable<RequestParameterConfig> GetResponseProperties()
        {
            foreach (var responseProperty in this._ResponseProperties)
            {
                yield return new ResponsePropertyConfig(responseProperty.Name); // TODO: Populate
            }
        }

        /// <summary>
        /// Container for information about Request Parameters attached to Custom APIs
        /// </summary>
        public class CustomAPIRequestParameter
        {
            public string Name { get; private set; }
            // TODO: More parameters

            public CustomAPIRequestParameter(string name)
            {
                this.Name = name;
            }
        }

        /// <summary>
        /// Container for information about Response Properties attached to Custom APIs
        /// </summary>
        public class CustomAPIResponseProperty
        {
            public string Name { get; private set; }

            public CustomAPIResponseProperty(string name)
            {
                this.Name = name;
            }
        }
    }

    public enum AllowedCustomProcessingStepType
    {
        //None = 0, // This value is default and should not be selectable
        AsyncOnly = 1,
        SyncAndAsync = 2
    }

    public enum BindingType
    {
        // Global = 0, // This value is default and should not be selectable
        Entity = 1,
        EntityCollection = 2
    }
    #endregion
}