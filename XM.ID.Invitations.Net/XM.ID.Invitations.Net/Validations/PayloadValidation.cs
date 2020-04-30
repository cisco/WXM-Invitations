using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Linq;

namespace XM.ID.Invitations.Net
{
    public class PayloadValidation
    {
        private readonly IConfiguration _config;
        List<string> dispatchArr;
        readonly ViaMongoDB viaMongo;

        public PayloadValidation(IConfiguration config, ViaMongoDB viaMongoDB)
        {
            _config = config;
            viaMongo = viaMongoDB;
        }

        public bool ValidateRequestPayloadSize(List<DispatchRequest> batchRequest, EventLogList eventLogList)
        {
            dispatchArr = new List<string>();
            double totalRecords = 0;
            
            for (int iter = 0; iter < batchRequest.Count(); iter++)
            {
                totalRecords += batchRequest[iter].PreFill.Count();
                if(!dispatchArr.Contains(batchRequest[iter].DispatchID))
                {
                    dispatchArr.Add(batchRequest[iter].DispatchID);
                }
            }

            if (totalRecords > int.Parse(_config["MaxPayloadSize"]))
            {
                eventLogList.AddEventByLevel(2, $"{SharedSettings.MaxRecordSizeExceedeed} - {totalRecords}" , null, null);
                return false;
            }
            else if (dispatchArr.Count > int.Parse(_config["MaxDispatchIDCount"]))
            {
                eventLogList.AddEventByLevel(2, $"{SharedSettings.MaxDispatchNumberExceeded} - {dispatchArr.Count}", null, null);
                return false;
            }
            return true;
        }

    }
}
