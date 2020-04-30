using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace XM.ID.Dispatcher.Net
{
    public class MessagePayload
    {
        [JsonIgnore]
        internal bool IsProcessable { get; set; }
        [JsonIgnore]
        internal bool? IsEmailDelivery { get; set; }
        [JsonIgnore]
        internal bool IsUserDataLogEventConfigured { get; set; }
        [JsonIgnore]
        internal bool IsVendorConfigured { get; set; }
        [JsonIgnore]
        internal bool IsBulkVendor { get; set; }
        /// <summary>
        /// Message received from Queue
        /// </summary>
        public AzureQueueData AzureQueueData { get; set; }
        /// <summary>
        /// User-Data-Log-Event
        /// </summary>
        public LogEvent Invitation { get; set; }
        /// <summary>
        /// Invitation's Vendor-Details
        /// </summary>
        public Vendor Vendor { get; set; }
        /// <summary>
        /// Logs to capture events at an Application level
        /// </summary>
        [JsonIgnore]
        public List<LogEvent> LogEvents { get; set; } = new List<LogEvent>();
        /// <summary>
        /// Logs to capture events at an Invitation level
        /// </summary>
        [JsonIgnore]
        public List<InvitationLogEvent> InvitationLogEvents { get; set; } = new List<InvitationLogEvent>();

        [JsonConstructor]
        public MessagePayload()
        {

        }
        
        public MessagePayload(AzureQueueData azureQueueData)
        {
            AzureQueueData = azureQueueData;
            LogEvents.Add(Utils.CreateLogEvent(AzureQueueData, IRDLM.Dequeued));
        }

        internal void Validate()
        {
            bool isTokenIdPresent = !string.IsNullOrWhiteSpace(AzureQueueData.TokenId);
            bool isBatchIdPresent = !string.IsNullOrWhiteSpace(AzureQueueData.BatchId);
            bool isDispatchIdPresent = !string.IsNullOrWhiteSpace(AzureQueueData.DispatchId);
            if (isTokenIdPresent && isBatchIdPresent && isDispatchIdPresent)
            {
                IsProcessable = true;
                LogEvents.Add(Utils.CreateLogEvent(AzureQueueData, IRDLM.Validated(AzureQueueData.AdditionalURLParameter)));
            }
            else
            {
                IsProcessable = false;
                LogEvents.Add(Utils.CreateLogEvent(AzureQueueData, IRDLM.Invalidated));
            }
        }

        internal void ConfigureChannel()
        {
            if (!string.IsNullOrWhiteSpace(AzureQueueData.EmailId) && !string.IsNullOrWhiteSpace(AzureQueueData.MobileNumber))
            {
                IsEmailDelivery = null;
                LogEvents.Add(Utils.CreateLogEvent(AzureQueueData, IRDLM.ChannelNotConfigured1));

            }
            else if (!string.IsNullOrWhiteSpace(AzureQueueData.EmailId))
            {
                IsEmailDelivery = true;
                LogEvents.Add(Utils.CreateLogEvent(AzureQueueData, IRDLM.EmailChannelConfigured));
            }
            else if (!string.IsNullOrEmpty(AzureQueueData.MobileNumber))
            {
                IsEmailDelivery = false;
                LogEvents.Add(Utils.CreateLogEvent(AzureQueueData, IRDLM.SmsChannelConfigured));
            }
            else
            {
                IsEmailDelivery = null;
                LogEvents.Add(Utils.CreateLogEvent(AzureQueueData, IRDLM.ChannelNotConfigured2));
            }
        }

        internal async Task ConfigureUserData()
        {
            Invitation = await Resources.GetInstance().LogEventCollection.Find(x => x.TokenId == AzureQueueData.TokenId &&
            x.BatchId == AzureQueueData.BatchId && x.DispatchId == AzureQueueData.DispatchId).FirstOrDefaultAsync();
            if (Invitation == default)
            {
                IsUserDataLogEventConfigured = false;
                LogEvents.Add(Utils.CreateLogEvent(AzureQueueData, IRDLM.UserDataNotFound));
            }
            else
            {
                IsUserDataLogEventConfigured = true;
                LogEvents.Add(Utils.CreateLogEvent(AzureQueueData, IRDLM.UserDataFound(Invitation.Id)));
            }
        }

        internal void PrepareForHashLookUps()
        {
            Dictionary<string, string> hashLookUpDict = new Dictionary<string, string>();
            foreach (Prefill prefill in Invitation.Prefills)
            {
                if (prefill.Input_Hash != null)
                {
                    if (!hashLookUpDict.ContainsKey(prefill.Input_Hash))
                        hashLookUpDict.Add(prefill.Input_Hash, prefill.Input);
                }
            }

            AzureQueueData.CommonIdentifier = Invitation.Target;
            if (IsEmailDelivery.Value)
            {
                if (hashLookUpDict.TryGetValue(AzureQueueData.EmailId, out string emailId))
                    AzureQueueData.EmailId = emailId;
            }
            else
            {
                if (hashLookUpDict.TryGetValue(AzureQueueData.MobileNumber, out string mobileNumber))
                    AzureQueueData.MobileNumber = mobileNumber;
            }


            Dictionary<string, string> unhashedMappedValues = new Dictionary<string, string>();
            foreach (KeyValuePair<string, string> keyValuePair in AzureQueueData.MappedValue)
            {
                if (!string.IsNullOrEmpty(keyValuePair.Value))
                {
                    if (hashLookUpDict.TryGetValue(keyValuePair.Value, out string unhashedValue))
                        unhashedMappedValues.Add(keyValuePair.Key, unhashedValue);
                    else
                        unhashedMappedValues.Add(keyValuePair.Key, keyValuePair.Value);
                }
            }
            AzureQueueData.MappedValue = unhashedMappedValues;
            LogEvents.Add(Utils.CreateLogEvent(AzureQueueData, IRDLM.HashLookUpDictConfigured));
        }

        internal void ConfigureVendor()
        {
            DispatchChannel dispatchChannel = Resources.GetInstance().AccountConfiguration.DispatchChannels?.Find(x => x.DispatchId == AzureQueueData.DispatchId);
            if (dispatchChannel == default)
            {
                IsVendorConfigured = false;
                LogEvents.Add(Utils.CreateLogEvent(AzureQueueData, IRDLM.DispatchChannelNotFound));
                InvitationLogEvents.Add(Utils.CreateInvitationLogEvent(EventAction.DispatchUnsuccessful,
                    IsEmailDelivery.Value ? EventChannel.Email : EventChannel.SMS, AzureQueueData, IRDLM.DispatchChannelNotFound));
            }
            else
            {
                string vendorName = null;
                if (IsEmailDelivery.Value)
                    vendorName = dispatchChannel?.ChannelDetails?.Email?.IsValid ?? false ? dispatchChannel.ChannelDetails.Email.Vendorname : null;
                else
                    vendorName = dispatchChannel?.ChannelDetails?.Sms?.IsValid ?? false ? dispatchChannel.ChannelDetails.Sms.Vendorname : null;
                if (vendorName == null)
                {
                    IsVendorConfigured = false;
                    LogEvents.Add(Utils.CreateLogEvent(AzureQueueData, IRDLM.DispatchVendorNameMissing));
                    InvitationLogEvents.Add(Utils.CreateInvitationLogEvent(EventAction.DispatchUnsuccessful,
                        IsEmailDelivery.Value ? EventChannel.Email : EventChannel.SMS, AzureQueueData, IRDLM.DispatchVendorNameMissing));
                }
                else
                {
                    LogEvents.Add(Utils.CreateLogEvent(AzureQueueData, IRDLM.DispatchVendorNamePresent(vendorName)));
                    Vendor = Resources.GetInstance().AccountConfiguration.Vendors?.Find(x => string.Equals(x.VendorName, vendorName, StringComparison.InvariantCultureIgnoreCase));
                    if (Vendor == null)
                    {
                        IsVendorConfigured = false;
                        LogEvents.Add(Utils.CreateLogEvent(AzureQueueData, IRDLM.DispatchVendorConfigMissing));
                        InvitationLogEvents.Add(Utils.CreateInvitationLogEvent(EventAction.DispatchUnsuccessful,
                            IsEmailDelivery.Value ? EventChannel.Email : EventChannel.SMS, AzureQueueData, IRDLM.DispatchVendorConfigMissing));
                    }
                    else
                    {
                        IsVendorConfigured = true;
                        LogEvents.Add(Utils.CreateLogEvent(AzureQueueData, IRDLM.DispatchVendorConfigPresent(Vendor)));
                    }
                }
            }
        }

        internal void ConfigureVendorFlag()
        {
            IsBulkVendor = Vendor.IsBulkVendor;
            if (IsBulkVendor)
                LogEvents.Add(Utils.CreateLogEvent(AzureQueueData, IRDLM.VendorIsBulk));
            else
                LogEvents.Add(Utils.CreateLogEvent(AzureQueueData, IRDLM.VendorIsNotBulk));
        }
    }

    [BsonIgnoreExtraElements]
    internal class DB_MessagePayload
    {
        [BsonId]
        public string Id { get; set; }
        public string BulkVendorName { get; set; }
        public string Status { get; set; }
        public DateTime InsertTime { get; set; }
        public string MessagePayload { get; set; }

        public DB_MessagePayload(MessagePayload messagePayload)
        {
            Id = ObjectId.GenerateNewId().ToString();
            MessagePayload = JsonConvert.SerializeObject(messagePayload);
            Status = "Ready";
            BulkVendorName = messagePayload.Vendor.VendorName.ToLower();
            InsertTime = DateTime.UtcNow;
        }
    }
}
