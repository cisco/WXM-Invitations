using MongoDB.Bson.Serialization.Attributes;
using System.Collections.Generic;

namespace XM.ID.Dispatcher.Net
{
    [BsonIgnoreExtraElements]
    internal class AccountConfiguration
    {
        [BsonId]
        public string Id { get; set; }
        public string WXMAPIKey { get; set; }
        public string WXMUser { get; set; }
        public string WXMBaseURL { get; set; }
        public string WXMAdminUser { get; set; }
        public List<DispatchChannel> DispatchChannels { get; set; }
        public List<Vendor> Vendors { get; set; }
        public Queue Queue { get; set; }
        public Dictionary<string, string> ExtendedProperties { get; set; }
    }

    [BsonIgnoreExtraElements]
    internal class DispatchChannel
    {
        public string DispatchId { get; set; }
        public string DispatchName { get; set; }
        public ChannelDetails ChannelDetails { get; set; }
        public List<StaticPrefill> StaticPrefills { get; set; }
        public Notify Notify { get; set; }
    }

    [BsonIgnoreExtraElements]
    internal class ChannelDetails
    {
        public Channel Email { get; set; }
        public Channel Sms { get; set; }
    }

    [BsonIgnoreExtraElements]
    internal class Channel
    {
        public bool IsValid { get; set; }
        public string Vendorname { get; set; }
    }

    [BsonIgnoreExtraElements]
    internal class StaticPrefill
    {
        public string QuestionId { get; set; }
        public string Note { get; set; }
        public string PrefillValue { get; set; }
    }

    [BsonIgnoreExtraElements]
    internal class Notify
    {
        public string D { get; set; }
        public string I { get; set; }
        public string W { get; set; }
        public string E { get; set; }
        public string F { get; set; }
    }

    [BsonIgnoreExtraElements]
    public class Vendor
    {
        /// <summary>
        /// "Email" or "Sms"
        /// </summary>
        public string VendorType { get; set; }
        /// <summary>
        /// Name of the vendor. This property is also used as Id
        /// for any search purposes (Case-insensitive matching)
        /// </summary>
        public string VendorName { get; set; }
        /// <summary>
        /// Is Single-Send or Bulk-Send
        /// </summary>
        public bool IsBulkVendor { get; set; }
        /// <summary>
        /// Key-Value Properties regarding the vendor-configuration
        /// </summary>
        public Dictionary<string, string> VendorDetails { get; set; }
    }

    [BsonIgnoreExtraElements]
    internal class Queue
    {
        public string QueueType { get; set; }
        public string QueueConnectionString { get; set; }
    }

    public class QueueData
    {
        /// <summary>
        /// Token Number
        /// </summary>
        public string TokenId { get; set; }
        /// <summary>
        /// Recepient's UUID (Hashed/Actual)
        /// </summary>
        public string CommonIdentifier { get; set; }
        /// <summary>
        /// Recepient's Email-Id
        /// </summary>
        public string EmailId { get; set; }
        /// <summary>
        /// Recepient's Mobile Number
        /// </summary>
        public string MobileNumber { get; set; }
        /// <summary>
        /// Account Name
        /// </summary>
        public string User { get; set; }
        /// <summary>
        /// Email Subject
        /// </summary>
        public string Subject { get; set; }
        /// <summary>
        /// Sms/Email(plain-text) body
        /// </summary>
        public string TextBody { get; set; }
        /// <summary>
        /// Email(rich-html) body
        /// </summary>
        public string HTMLBody { get; set; }
        /// <summary>
        /// Prefilled Question-Answer Values
        /// </summary>
        public Dictionary<string, string> MappedValue { get; set; }
        public string DispatchId { get; set; }
        public string BatchId { get; set; }
        public string TemplateId { get; set; }
        /// <summary>
        /// Details regarding Ivitation's Channel and Reminder-Level
        /// </summary>
        public string AdditionalURLParameter { get; set; }
    }

    [BsonIgnoreExtraElements]
    public class Prefill
    {
        public string QuestionId { get; set; }
        public string Input { get; set; }
        public string Input_Hash { get; set; }
    }
}
