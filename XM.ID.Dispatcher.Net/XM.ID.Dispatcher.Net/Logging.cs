using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace XM.ID.Dispatcher.Net
{
    [BsonIgnoreExtraElements]
    public class LogEvent
    {
        [BsonId]
        public string Id { get; set; }
        public string TokenId { get; set; }
        public string DeliveryWorkFlowId { get; set; }
        public string DispatchId { get; set; }
        public string BatchId { get; set; }
        public string User { get; set; }
        public string Location { get; set; }
        public DateTime Created { get; set; }
        public DateTime Updated { get; set; }
        /// <summary>
        /// Captures the Journey Details of the Invitation
        /// </summary>
        public List<InvitationLogEvent> Events { get; set; }
        /// <summary>
        /// Recepient's UUID (Actual)
        /// </summary>
        public string Target { get; set; }
        /// <summary>
        /// Recepient's UUID (Hashed/Actual)
        /// </summary>
        public string TargetHashed { get; set; }
        /// <summary>
        /// Pre-Fill Questions and Answers
        /// </summary>
        public List<Prefill> Prefills { get; set; }
        public LogMessage LogMessage { get; set; }
        public List<string> Tags { get; set; }
    }

    [BsonIgnoreExtraElements]
    public class LogMessage
    {
        public string Message { get; set; }
        public string Level { get; set; }
        public string Exception { get; set; }
        public static string SeverityLevel_Verbose = "Debug";
        public static string SeverityLevel_Warning = "Warning";
        public static string SeverityLevel_Information = "Information";
        public static string SeverityLevel_Error = "Error";
        public static string SeverityLevel_Critical = "Failure";
    }

    [BsonIgnoreExtraElements]
    public class InvitationLogEvent
    {
        /// <summary>
        /// Creation DataTime
        /// </summary>
        public DateTime TimeStamp { get; set; }
        /// <summary>
        /// Restricted to Email/SMS/Unknown/Invalid for Serverless Compute
        /// </summary>
        public EventChannel Channel { get; set; }
        /// <summary>
        /// Restricted to DispatchSuccessful/DispatchUnsuccessful for Serverless Compute
        /// </summary>
        public EventAction Action { get; set; }
        /// <summary>
        /// Any Additional Details
        /// </summary>
        public string Message { get; set; }
        /// <summary>
        /// Recepient's EmailId/MobileNumber
        /// </summary>
        public string TargetId { get; set; }
        /// <summary>
        /// Not-Required for Serverless Computes
        /// </summary>
        public DeliveryEventStatus EventStatus { get; set; }
        /// <summary>
        /// Log-Details
        /// </summary>
        public LogMessage LogMessage { get; set; }
    }

    /// <summary>
    /// For Serverless Computes value used should be Email/SMS/Unknown/Invalid
    /// </summary>
    public enum EventChannel { Email, SMS, DispatchAPI, Unknown, Invalid };
    /// <summary>
    /// For Serverless Computes value used should be DispatchSuccessful, DispatchUnsuccessful 
    /// </summary>
    public enum EventAction
    {
        Requested, Rejected, TokenCreated, Sent, Error, Supressed,
        DispatchSuccessful, DispatchUnsuccessful, Throttled
    };

    [BsonIgnoreExtraElements]
    public class DeliveryEventStatus
    {
        public int Requested { get; set; }
        public int Accepetd { get; set; }
        public int Rejected { get; set; }
    }

    //IRDLM = Invitation-Related Dispatcher Log-Messages
    public static class IRDLM
    {
        internal static LogMessage Dequeued = new LogMessage
        {
            Level = LogMessage.SeverityLevel_Verbose,
            Message = "Invitation dequeued"
        };

        internal static LogMessage Validated(string additionalParams)
        {
            return new LogMessage
            {
                Level = LogMessage.SeverityLevel_Verbose,
                Message = $"Invitation validated | Additional Token Parameters: {additionalParams}"
            };
        }

        internal static LogMessage Invalidated = new LogMessage
        {
            Level = LogMessage.SeverityLevel_Error,
            Message = "Invitation invalidated => Token-Id/Batch-Id/Dispatch-Id is null/empty"
        };

        internal static LogMessage ChannelNotConfigured1 = new LogMessage
        {
            Level = LogMessage.SeverityLevel_Error,
            Message = "Invitation's channel not configured => Both EmailId and MobileNumber are present"
        };

        internal static LogMessage ChannelNotConfigured2 = new LogMessage
        {
            Level = LogMessage.SeverityLevel_Error,
            Message = "Invitation's channel not configured => Both EmailId and MobileNumber are null"
        };

        internal static LogMessage EmailChannelConfigured = new LogMessage
        {
            Level = LogMessage.SeverityLevel_Verbose,
            Message = "Invitation's channel configured => Email"
        };

        internal static LogMessage SmsChannelConfigured = new LogMessage
        {
            Level = LogMessage.SeverityLevel_Verbose,
            Message = "Invitation's channel configured => Sms"
        };

        internal static LogMessage UserDataFound(string id)
        {
            return new LogMessage
            {
                Level = LogMessage.SeverityLevel_Verbose,
                Message = $"Invitation's User-Data-Log-Event configured => Matching document id: {id}"
            };
        }

        internal static LogMessage UserDataNotFound = new LogMessage
        {
            Level = LogMessage.SeverityLevel_Error,
            Message = "Invitation's User-Data-Log-Event not configured => No matching document found in collection"
        };

        internal static LogMessage HashLookUpDictConfigured = new LogMessage
        {
            Level = LogMessage.SeverityLevel_Verbose,
            Message = "Invitation's Hash Look-Up Dictionary Configured"
        };

        internal static LogMessage DispatchVendorNamePresent(string name)
        {
            return new LogMessage
            {
                Level = LogMessage.SeverityLevel_Verbose,
                Message = $"Invitation's Dispatch-Id's Dispatch-Vendor-Name was found => Dispatch's Vendor Name: {name}"
            };
        }

        internal static LogMessage DispatchVendorNameMissing = new LogMessage
        {
            Level = LogMessage.SeverityLevel_Error,
            Message = "Invitation's Dispatch-Id's Dispatch-Vendor-Name was not found => Misconfigured DispatchChannel in Account-Configuration"
        };

        internal static LogMessage DispatchVendorConfigPresent(Vendor vendor)
        {
            return new LogMessage
            {
                Level = LogMessage.SeverityLevel_Verbose,
                Message = $"Invitation's Dispatch-Vendor-Config was found => {JsonConvert.SerializeObject(vendor)}"
            };
        }

        internal static LogMessage DispatchVendorConfigMissing = new LogMessage
        {
            Level = LogMessage.SeverityLevel_Error,
            Message = "Invitation's Dispatch-Vendor-Config was not found => Absent Vendor-Details from Account-Config"
        };

        internal static LogMessage VendorIsBulk = new LogMessage
        {
            Level = LogMessage.SeverityLevel_Verbose,
            Message = "Invitation's VendorType is bulk => Invitation will now be inserted in to [Collection: BulkMessage]"
        };

        internal static LogMessage VendorIsNotBulk = new LogMessage
        {
            Level = LogMessage.SeverityLevel_Verbose,
            Message = "Invitation's VendorType is not bulk => Invitation will now be dispatched"
        };

        internal static LogMessage DispatchVendorImplemenatationPresent(Vendor vendor)
        {
            return new LogMessage
            {
                Level = LogMessage.SeverityLevel_Verbose,
                Message = $"Invitation's Dispatch-Vendor-Implementation was found in Memory => {JsonConvert.SerializeObject(vendor)}"
            };
        }

        internal static LogMessage DispatchVendorImplementationMissing = new LogMessage
        {
            Level = LogMessage.SeverityLevel_Error,
            Message = "Invitation's Dispatch-Vendor-Implementation was not found in Memory => Vendor Name is Missing From DispatchVendor_CreationStratergies"
        };

        /// <summary>
        /// Create a LogMessage which marks an Invitation's Dispatch status as Successful
        /// </summary>
        /// <param name="vendorName"></param>
        /// <returns>A LogMessage which marks the Invitation's Dispatch status as Successful</returns>
        public static LogMessage DispatchSuccessful(string vendorName)
        {
            return new LogMessage
            {
                Level = LogMessage.SeverityLevel_Information,
                Message = $"Invitation has been successfully dispatched => via {vendorName}"
            };
        }

        /// <summary>
        /// Create a LogMessage which marks an Invitation's Dispatch status as Unsuccessful
        /// </summary>
        /// <param name="vendorName"></param>
        /// <param name="ex">Reason for Failure</param>
        /// <returns>A LogMessage which marks the Invitation's Dispatch status as Unsuccessful due to Exception:ex</returns>
        public static LogMessage DispatchUnsuccessful(string vendorName, Exception ex)
        {
            return new LogMessage
            {
                Exception = JsonConvert.SerializeObject(ex),
                Level = LogMessage.SeverityLevel_Error,
                Message = $"Invitation was not dispatched => via {vendorName}"
            };
        }

        internal static LogMessage ReadFromDB = new LogMessage
        {
            Level = LogMessage.SeverityLevel_Verbose,
            Message = $"Invitation read from DB into memory => Vendor: {Resources.GetInstance().BulkVendorName}"
        };

        internal static LogMessage InternalException(Exception ex)
        {
            return new LogMessage
            {
                Exception = JsonConvert.SerializeObject(ex),
                Level = LogMessage.SeverityLevel_Critical,
                Message = "Internal Exception"
            };
        }

        internal static LogMessage TimeTriggerStart = new LogMessage
        {
            Level = LogMessage.SeverityLevel_Verbose,
            Message = $"Time Trigger Function has now started"
        };

        internal static LogMessage TimeTriggerEnd(int messageCount)
        {
            return new LogMessage
            {
                Level = LogMessage.SeverityLevel_Verbose,
                Message = $"Time Trigger Function has now ended => Messages Processed = {messageCount}"
            };
        }

        internal static LogMessage TimeTriggerRunningLate = new LogMessage
        {
            Level = LogMessage.SeverityLevel_Warning,
            Message = $"Time Trigger Function is runnning late"
        };

        internal static LogMessage DispatchChannelNotFound = new LogMessage
        {
            Level = LogMessage.SeverityLevel_Error,
            Message = "Dispatch Channel Missing from Account-Configuration"
        };

    }
}
