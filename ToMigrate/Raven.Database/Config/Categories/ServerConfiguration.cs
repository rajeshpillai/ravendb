using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using Raven.Abstractions.Data;
using Raven.Database.Config.Attributes;
using Raven.Database.Config.Settings;

namespace Raven.Database.Config.Categories
{
    public class ServerConfiguration : ConfigurationCategory
    {
        [DefaultValue(512)]
        [ConfigurationEntry("Raven/Server/MaxConcurrentRequests")]
        [ConfigurationEntry("Raven/MaxConcurrentServerRequests")]
        public int MaxConcurrentRequests { get; set; }

        [DefaultValue(50)]
        [ConfigurationEntry("Raven/Server/MaxConcurrentRequestsForDatabaseDuringLoad")]
        [ConfigurationEntry("Raven/MaxConcurrentRequestsForDatabaseDuringLoad")]
        public int MaxConcurrentRequestsForDatabaseDuringLoad { get; set; }

        [DefaultValue(5)]
        [TimeUnit(TimeUnit.Seconds)]
        [ConfigurationEntry("Raven/Server/MaxTimeForTaskToWaitForDatabaseToLoadInSec")]
        [ConfigurationEntry("Raven/MaxSecondsForTaskToWaitForDatabaseToLoad")]
        public TimeSetting MaxTimeForTaskToWaitForDatabaseToLoad { get; set; }

        [DefaultValue(192)]
        [ConfigurationEntry("Raven/Server/MaxConcurrentMultiGetRequests")]
        [ConfigurationEntry("Raven/MaxConcurrentMultiGetRequests")]
        public int MaxConcurrentMultiGetRequests { get; set; }


        [Description("Determine the value of the Access-Control-Allow-Origin header sent by the server. " +
                     "Indicates the URL of a site trusted to make cross-domain requests to this server." +
                     "Allowed values: null (don't send the header), *, http://example.org (space separated if multiple sites)")]
        [DefaultValue((string)null)]
        [ConfigurationEntry("Raven/Server/AccessControlAllowOrigin")]
        [ConfigurationEntry("Raven/AccessControlAllowOrigin")]
        public string AccessControlAllowOriginStringValue { get; set; }

        public HashSet<string> AccessControlAllowOrigin { get; set; }

        [Description("Determine the value of the Access-Control-Max-Age header sent by the server. " +
                     "Indicates how long (seconds) the browser should cache the Access Control settings. " +
                     "Ignored if AccessControlAllowOrigin is not specified.")]
        [DefaultValue("1728000" /* 20 days */)]
        [ConfigurationEntry("Raven/Server/AccessControlMaxAge")]
        [ConfigurationEntry("Raven/AccessControlMaxAge")]
        public string AccessControlMaxAge { get; set; }

        [Description("Determine the value of the Access-Control-Allow-Methods header sent by the server." +
                     " Indicates which HTTP methods (verbs) are permitted for requests from allowed cross-domain origins." +
                     " Ignored if AccessControlAllowOrigin is not specified.")]
        [DefaultValue("PUT,PATCH,GET,DELETE,POST")]
        [ConfigurationEntry("Raven/Server/AccessControlAllowMethods")]
        [ConfigurationEntry("Raven/AccessControlAllowMethods")]
        public string AccessControlAllowMethods { get; set; }

        [Description("Determine the value of the Access-Control-Request-Headers header sent by the server. " +
                     "Indicates which HTTP headers are permitted for requests from allowed cross-domain origins. " +
                     "Ignored if AccessControlAllowOrigin is not specified. " +
                     "Allowed values: null (allow whatever headers are being requested), HTTP header field name")]
        [DefaultValue(null)]
        [ConfigurationEntry("Raven/Server/AccessControlRequestHeaders")]
        [ConfigurationEntry("Raven/AccessControlRequestHeaders")]
        public string AccessControlRequestHeaders { get; set; }

        [Description("The url to redirect the user to when then try to access the local studio")]
        [DefaultValue(null)]
        [ConfigurationEntry("Raven/Server/RedirectStudioUrl")]
        [ConfigurationEntry("Raven/RedirectStudioUrl")]
        public string RedirectStudioUrl { get; set; }

        [Description("The server name")]
        [DefaultValue(null)]
        [ConfigurationEntry("Raven/Server/Name")]
        [ConfigurationEntry("Raven/ServerName")]
        public string Name { get; set; }


        public override void Initialize(NameValueCollection settings)
        {
            base.Initialize(settings);

            AccessControlAllowOrigin = string.IsNullOrEmpty(AccessControlAllowOriginStringValue) ? new HashSet<string>() : new HashSet<string>(AccessControlAllowOriginStringValue.Split());
        }
    }
}