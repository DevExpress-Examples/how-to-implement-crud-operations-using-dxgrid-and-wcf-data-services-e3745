using System;
using System.Collections.Generic;
using System.Data.Services;
using System.Data.Services.Common;
using System.Linq;
using System.ServiceModel.Web;
using System.Web;

namespace WCFInstantFeedbackCRUD.Web
{
    public class NorthwindService : DataService<NorthwindEntities>
    {
        // This method is called only once to initialize service-wide policies.
        public static void InitializeService(DataServiceConfiguration config)
        {
            // TODO: set rules to indicate which entity sets and service operations are visible, updatable, etc.
            // Examples:
            config.SetEntitySetAccessRule("*", EntitySetRights.All);
            config.SetServiceOperationAccessRule("GetCustomersExtendedData", ServiceOperationRights.AllRead);
            config.DataServiceBehavior.MaxProtocolVersion = DataServiceProtocolVersion.V2;
        }

        [WebGet(UriTemplate = "GetCustomersExtendedData?extendedDataInfo={extendedDataInfo}")]
        public string GetCustomersExtendedData(string extendedDataInfo) {
            return DevExpress.Xpf.Core.ServerMode.ExtendedDataHelper.GetExtendedData(CurrentDataSource.Customers, extendedDataInfo);
        }
    }
}
