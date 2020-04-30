using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace XM.ID.Invitations.Net
{

    #region Partner hosted Models
    public class DispatchRequest
    {
        public List<List<PreFillValue>> PreFill { get; set; }
        public string DispatchID { get; set; }
    }

    public class RequestPrefill
    {
        public List<List<PreFillValue>> PreFill { get; set; }
        public string DeliveryPlanID { get; set; }
        public List<string> Channels { get; set; }
        public string uniqueCustomerIDByPreFilledQuestionTag { get; set; }
        public string questionnaireName { get; set; }
    }

    public class PreFillValue
    {
        public string questionId { get; set; }
        public string input { get; set; }
    }

    public class BatchResponse
    {
        public string BatchId { get; set; }
        public List<StatusByDispatch> StatusByDispatch { get; set; }
    }
    public class StatusByDispatch
    {

        public string DispatchId { get; set; }
        public string Message { get; set; }
        public string DispatchStatus { get; set; }
    }

    public interface ISampler
    {
        //inplace sampler
        public Task IsSampledAsync(List<DispatchRequest> dispatchRequests);
    }

    public class WXMSampler : ISampler
    {
        public async Task IsSampledAsync(List<DispatchRequest> dispatchRequests)
        {
            await Task.CompletedTask;
        }
    }

    public interface IUnsubscribeChecker
    {
        public Task<bool> IsUnsubscribedAsync(string customerIdentifier);
    }

    public class WXMUnsubscribeChecker : IUnsubscribeChecker
    {
        public async Task<bool> IsUnsubscribedAsync(string customIdentifier)
        {
            return await Task.FromResult(false);
        }
    }

    public interface IBatchingQueue<T>
    {
        public void Insert(T item);
    }
    #endregion

    #region WXM Models

    public class BearerToken
    {
        [JsonProperty(PropertyName = "access_token")]
        public string AccessToken { get; set; }

        [JsonProperty(PropertyName = "token_type")]
        public string TokenType { get; set; }

        [JsonProperty(PropertyName = "expires_in")]
        public int ExpiresIn { get; set; }

        [JsonProperty(PropertyName = "userName")]
        public string UserName { get; set; }

        [JsonProperty(PropertyName = "email")]
        public string Email { get; set; }

        [JsonProperty(PropertyName = "primaryRole")]
        public string PrimaryRole { get; set; }

        [JsonProperty(PropertyName = "managedBy")]
        public string ManagedBy { get; set; }

        [JsonProperty(PropertyName = "preview")]
        public string Preview { get; set; }

        [JsonProperty(PropertyName = "station")]
        public string Station { get; set; }

        [JsonProperty(PropertyName = "hash")]
        public string Hash { get; set; }

        [JsonProperty(PropertyName = ".issued")]
        public string Issued { get; set; }

        [JsonProperty(PropertyName = ".expires")]
        public string Expires { get; set; }
    }

    public class UserProfile
    {
        public string user { get; set; }
        public object name { get; set; }
        public object enterpriseRole { get; set; }
        public object department { get; set; }
        public object enterpriseRoleId { get; set; }
        public object departmentId { get; set; }
        public bool? isDepartmentAdmin { get; set; }
        public bool? isAccountAdmin { get; set; }
        public object reportsTo { get; set; }
        public List<object> languages { get; set; }
        public List<object> regions { get; set; }
        public double rating { get; set; }
        public int ratingCount { get; set; }
        public DateTime since { get; set; }
        public DateTime lastSeen { get; set; }
        public object locations { get; set; }
        public object conditionalFilter { get; set; }
        public object status { get; set; }
        public string email { get; set; }
        public object phone { get; set; }
        public bool? isPhoneVerified { get; set; }
        public bool? rememberTwoFactor { get; set; }
        public bool? highPrecisionMode { get; set; }
        public object timeZoneOffset { get; set; }
    }

    public class Dispatch
    {
        public string Id { get; set; }
        public string User { get; set; }
        public string Name { get; set; }
        public DateTime Created { get; set; }
        public DateTime Updated { get; set; }
        public string DeliveryPlanId { get; set; }
        public Dictionary<string, string> ContentTemplateIds { get; set; }
        public string TokenTemplateId { get; set; }
        public string QuestionnaireName { get; set; }
        public string QuestionnaireDisplayName { get; set; }
        public bool IsLive { get; set; }
        public string Message { get; set; }
    }


    public class Schedule
    {
        public string onChannel { get; set; }
        public int paceConnections { get; set; }
        public bool? nonConCurrent { get; set; }
        public int delayByHours { get; set; }
        public string subject { get; set; }
        public string textBody { get; set; }
        public string htmlBody { get; set; }
        public string templateId { get; set; }
        public string externalDNDCheck { get; set; }
        public string additionalURLParameter { get; set; }
    }

    public class RouteEmailSMTP
    {
        public string fromName { get; set; }
        public string fromAddress { get; set; }
        public string server { get; set; }
        public string login { get; set; }
        public string password { get; set; }
        public int port { get; set; }
        public bool? enableSSL { get; set; }
    }

    public class DeliveryPlan
    {
        public string id { get; set; }
        public string user { get; set; }
        public string outboundResidency { get; set; }
        public DateTime created { get; set; }
        public DateTime updated { get; set; }
        public string name { get; set; }
        public string description { get; set; }
        public bool isLive { get; set; }
        public DateTime? goodAfterTimeOfDay { get; set; }
        public DateTime? goodBeforeTimeOfDay { get; set; }
        public List<string> goodOnDaysOfWeek { get; set; }
        public DateTime? startAfter { get; set; }
        public DateTime? endBefore { get; set; }
        public int remindOnlyAfterHours { get; set; }
        public bool? remindOnlyAfterOpenHours { get; set; }
        public int repeatOnlyAfterHours { get; set; }
        public int repeatOnlyLessThanResponses { get; set; }
        public int remindOnlyLessThanInvites { get; set; }
        public string uniqueCustomerIDByPreFilledQuestionTag { get; set; }
        public bool uniquieIDAccountWide { get; set; }
        public List<Schedule> schedule { get; set; }
        public bool notifyOnExceptions { get; set; }
        public List<string> notifyToEmailIds { get; set; }
        public string emailFromAddress { get; set; }
        public string emailFromName { get; set; }
        public string whitelabelSurveyDomainPath { get; set; }
        public List<string> maskQuestionIdsOnCollection { get; set; }
        public List<string> ommitQuestionIdsOnCollection { get; set; }
        public RouteEmailSMTP routeEmailSMTP { get; set; }
        public int tokensAttached { get; set; }
        public int surveysDelivered { get; set; }
        public int surveysOpened { get; set; }
        public int surveysAnswered { get; set; }
        public int remindersDelivered { get; set; }
        public int emailSent { get; set; }
        public int emailBounces { get; set; }
        public int unsubscribes { get; set; }
        public int smsSent { get; set; }
        public int deliveryExceptions { get; set; }
        public int expiredTokens { get; set; }
        public int duplicateSupressed { get; set; }
        public bool overrideEarlierPendingRequests { get; set; }
        public DateTime lastRun { get; set; }
        public string status { get; set; }
        public string message { get; set; }
    }

    public class Settings
    {
        public string id { get; set; }
        public string user { get; set; }
        public bool locationDataMigrated { get; set; }
        public List<Location> locationList { get; set; }
        public Integration Integrations { get; set; }
    }

    public class Integration
    {
        public List<QueueChannel> QueueDetails { get; set; }
    }

    public class QueueChannel
    {
        public string Type { get; set; } //channel type azurequeue / Azure service bus / aws Queue
        public string QueueName { get; set; }
        public string ConnectionString { get; set; }
    }

    /// <summary>
    /// Personalization Per Location
    /// </summary>
    public class Location
    {
        /// <summary>
        /// If true, means that the 'Name' property acts as an immutable Identifier, and 'DisplayName' property should be used for modifiable display and other cosmetic properties.
        /// If false, means that the 'Name' property can be used both as an Identifier, as well as for display purposes.
        /// </summary>
        public bool IsNameImmutable { get; set; } = false;

        /// <summary>
        /// Location Name (ex: Downtown)
        /// </summary>
        [Required]
        public string Name { get; set; }
        /// <summary>
        /// This can be renamed everyday for UI/reports and is only for cosmetic display
        /// </summary>
        public string DisplayName { get; set; }
        /// <summary>
        /// Address for map
        /// </summary>
        public string Address { get; set; }
        /// <summary>
        /// Brand Name (ex: Orange)
        /// </summary>
        public string Brand { get; set; }
        /// <summary>
        /// Displayed Poll Channels
        /// </summary>
        public List<string> PollChannels { get; set; }
        /// <summary>
        /// Logo Set
        /// </summary>
        public string LogoURL { get; set; }
        /// <summary>
        /// Background URL set
        /// </summary>
        [DataType(DataType.ImageUrl)]
        public string BackgroundURL { get; set; }

        /// <summary>
        /// Background URL set
        /// </summary>
        [DataType(DataType.ImageUrl)]
        public string BusinessURL { get; set; }

        /// <summary>
        /// ex: South, North
        /// </summary>
        public string Group { get; set; }

        /// <summary>
        /// Location Tags
        /// </summary>
        public List<string> Tags { get; set; }

        /// <summary>
        /// Hex Color Code
        /// </summary>
        public string ColorCode1 { get; set; }
        /// <summary>
        /// Hex Color Code
        /// </summary>
        public string ColorCode2 { get; set; }
        /// <summary>
        /// Hex Color Code
        /// </summary>
        public string ColorCode3 { get; set; }

        /// <summary>
        /// Welcome title on questionnaire start
        /// </summary>
        public string WelcomeTitle { get; set; } // "Please help us understand .."
        /// <summary>
        /// Welcome text on questionnaire start
        /// </summary>
        public string WelcomeText { get; set; } // "Please help us understand .."
        /// <summary>
        /// Thank you title on questionnaire end
        /// </summary>
        public string ThankyouTitle { get; set; } // "Please help us understand .."

        /// <summary>
        /// Thank you title on questionnaire end
        /// </summary>
        /// <remarks>Back Compat Only, Pending Remove Post Porting</remarks>
        [Obsolete]
        public string ThankyouTtitle { get; set; } // "Please help us understand .."

        /// <summary>
        /// Thank you text on questionnaire end
        /// </summary>
        public string ThankyouText { get; set; } // "Please help us understand .."
        /// <summary>
        /// Welcome Audio
        /// </summary>
        public string WelcomeAudio { get; set; } // Optional Audio For IVRS
        /// <summary>
        /// Thankyou Audio
        /// </summary>
        public string ThankyouAudio { get; set; } // Optional Audio For IVRS
        /// <summary>
        /// Welcome Disclaimer Text
        /// </summary>
        public string WelcomeDisclaimerText { get; set; } // Optional Welcome Disclaimer Text
        /// <summary>
        /// Thank you Disclaimer Text
        /// </summary>
        public string ThankyouDisclaimerText { get; set; } // Optional Thank you Disclaimer Text
        /// <summary>
        /// Redirect to URL on survey completion
        /// </summary>
        public string RedirectOnSubmit { get; set; }
        /// <summary>
        /// Custom Attributes Per Location
        /// </summary>
        public Dictionary<string, string> Attributes { get; set; }

        /// <summary>
        /// Multi Language Support, ISO 639-1 Code(ex: en = > english), Translated Display Item
        /// </summary>
        public Dictionary<string, AltDisplaySettings> Translated { get; set; }

        /// <summary>
        /// Data Retention Limit For Location
        /// </summary>
        /// <remarks>Min = 7 Days, Max = 1095 Days(3 Years)</remarks>
        public int DataRetentionDays { get; set; }

        /// <summary>
        /// Contains details for each themes, Ex. custom dictionnary
        /// </summary>
        public List<ThemeDetails> ThemeDictionary { get; set; }

        /// <summary>
        /// If Specified hash the response on collecting with algorithm specified EX: sha256 or sha384 or sha512
        /// after converting the <text> to lowercase, hash the responses in the format sha256:<text> or sha384:<text>
        /// </summary>
        public string HashPIIBy { get; set; }
    }
    public class AltDisplaySettings
    {
        /// <summary>
        /// Optional Welcome Intro/Title
        /// </summary>
        public string WelcomeTitle { get; set; }

        /// <summary>
        /// Ex: "Please help us understand .."
        /// </summary>
        public string WelcomeText { get; set; }
        /// <summary>
        /// Optional Audio For IVRS
        /// </summary>
        public string WelcomeAudio { get; set; }
        /// <summary>
        /// Welcome Disclaimer Text
        /// </summary>
        public string WelcomeDisclaimerText { get; set; } // Optional Welcome Disclaimer Text
        /// <summary>
        /// Thank you message title
        /// </summary>
        public string ThankyouTitle { get; set; }
        /// <summary>
        /// Thank you message text
        /// </summary>
        public string ThankyouText { get; set; }
        /// <summary>
        /// Optional Audio For IVRS
        /// </summary>
        public string ThankyouAudio { get; set; }
        /// <summary>
        /// Thank you Disclaimer Text
        /// </summary>
        public string ThankyouDisclaimerText { get; set; } // Optional Thank you Disclaimer Text
        /// <summary>
        /// ex: "Information provided here is kept .."
        /// </summary>
        public string DisclaimerText { get; set; }
    }


    public class ThemeDetails
    {
        public string Name { get; set; }
        public List<string> Tags { get; set; }
        public int Weight { get; set; }
    }

    #endregion

    #region ActiveQuestions

    public class Question
    {
        public string Id { get; set; }
        public string User { get; set; }
        public DateTime? LastUpdate { get; set; }
        public string LastAuthor { get; set; }
        public string SetName { get; set; }
        public int Sequence { get; set; }
        public string Text { get; set; }
        public string TitleText { get; set; }
        public string Audio { get; set; }
        public string DisplayType { get; set; }
        public List<string> MultiSelect { get; set; }
        public List<string> DisplayLegend { get; set; }
        public List<string> MultiSelectChoiceTag { get; set; }
        public bool StaffFill { get; set; }
        public bool ApiFill { get; set; }
        public List<string> DisplayLocation { get; set; }
        public List<string> DisplayLocationByTag { get; set; }
        public double UserWeight { get; set; }
        public string DisplayStyle { get; set; }
        public object ConditionalToQuestion { get; set; }
        public object ConditionalAnswerCheck { get; set; }
        public int ConditionalNumber { get; set; }
        public bool EndOfSurvey { get; set; }
        public string EndOfSurveyMessage { get; set; }
        public string PresentationMode { get; set; }
        public object AnalyticsTag { get; set; }
        public bool IsRequired { get; set; }
        public List<string> QuestionTags { get; set; }
        public List<string> TopicTags { get; set; }
        public DateTime? GoodAfter { get; set; }
        public DateTime? GoodBefore { get; set; }
        public DateTime? TimeOfDayAfter { get; set; }
        public DateTime? TimeOfDayBefore { get; set; }
        public bool IsRetired { get; set; }
        public string Note { get; set; }
        public PIISetting piiSettings { get; set; }
        public List<string> headerTags { get; set; }
    }

    public class PIISetting
    {
        public bool isPII { get; set; } // set a question as PII 
        public string piiType { get; set; }
        public string exceptionBy { get; set; }
        public DateTime? exceptionAt { get; set; }
    }

    #endregion

    #region EventLogs

    [BsonIgnoreExtraElements]
    public class Prefill
    {
        /// <summary>
        /// refills question Ids , or add Default question tags (i.e Email /Mobile)
        /// </summary>
        public string QuestionId { get; set; }
        /// <summary>
        /// Actual input of the prefill 
        /// </summary>
        public string Input { get; set; }
        /// <summary>
        /// if question is set as PII and Hashed, the hashed value of the input 
        /// </summary>
        public string Input_Hash { get; set; }

    }
    [BsonIgnoreExtraElements]
    public class LogEvent
    {
        /// <summary>
        /// Token number or BsonId where token is not present
        /// </summary>
        [BsonId]
        public string Id { get; set; }

        public string TokenId { get; set; }

        /// <summary>
        /// Triggered By Delivery Plan Id
        /// </summary>
        public string DeliveryWorkFlowId { get; set; }


        public string DispatchId { get; set; }

        public string BatchId { get; set; }

        /// <summary>
        /// Account 
        /// </summary>
        public string User { get; set; }

        /// <summary>
        /// Location or Questionnaire Name
        /// </summary>
        public string Location { get; set; }

        /// <summary>
        /// First Trigger
        /// </summary>
        public DateTime Created { get; set; }

        /// <summary>
        /// Latest Update
        /// </summary>
        public DateTime Updated { get; set; }

        /// <summary>
        /// Collected Events
        /// </summary>
        public List<InvitationLogEvent> Events { get; set; }

        /// <summary>
        /// Typically a common identifier(ex: accountid, number, email when allowed, mobile when allowed)
        /// </summary>

        public string Target { get; set; }
        /// <summary>
        /// Hashed value of the target, i.e the Common Identifier.
        /// this will be main point to fetch all the other prefills along with the batch Id and dispatch Id
        /// </summary>
        public string TargetHashed { get; set; }


        /// <summary>
        ///Add prefills, along with the hashed value for the slicing of information. (ex. response rate by location/touchpoint)
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

        public static string SeverityLevel_Debug = "Debug";
        public static string SeverityLevel_Warning = "Warning";
        public static string SeverityLevel_Information = "Information";
        public static string SeverityLevel_Error = "Error";
        public static string SeverityLevel_Critical = "Failure";
    }
    [BsonIgnoreExtraElements]
    public class InvitationLogEvent
    {

        public DateTime TimeStamp { get; set; }

        /// <summary>
        /// Email,SMS 
        /// In case of Action requested, Channel could be BatchRequest
        /// </summary>
        public EventChannel Channel { get; set; }

        /// <summary>
        /// Requested(For all event before a token is created, i.e when an API call is received with some records) , Sent, Bounced, Unsubscribed/Suppressed
        /// </summary>
        public EventAction Action { get; set; }

        /// <summary>
        /// Optional Note, Subject for Email, ex : Rated NPS 7, SMTP Error, Email Bounced
        /// </summary>
        public string Message { get; set; }

        //by channel whichever id is been used 
        public string TargetId { get; set; }
        /// <summary>
        /// properties like, Requested, rejected,accepted, queued, and the number against it, 
        /// Properties might be different for Different Action
        /// </summary>
        public DeliveryEventStatus EventStatus { get; set; }
        /// <summary>
        /// Log level can be DWIEF
        /// </summary>
        public LogMessage LogMessage { get; set; }

        public enum EventChannel { Email, SMS, DispatchAPI, Unknown, Invalid };
        public enum EventAction
        {
            Requested, Rejected, TokenCreated, Sent, Error, Supressed,
            DispatchSuccessful, DispatchUnsuccessful, Throttled
        };


    }



    [BsonIgnoreExtraElements]
    public class DeliveryEventStatus
    {
        public int Requested { get; set; }
        public int Accepetd { get; set; }
        public int Rejected { get; set; }
    }
    #endregion

    #region BulkToken
    public class RequestBulkToken
    {
        public string DispatchId { get; set; }
        public List<List<Response>> PrefillReponse { get; set; }
        public string UUID { get; set; }
        public string Batchid { get; set; }
    }

    public class BulkTokenResult
    {
        public string Token { get; set; }
        public string UUID { get; set; }
        public string Batchid { get; set; }
    }

    public class ActivityFilter
    {
        public string BatchId { get; set; }
        public string DispatchId { get; set; }
        public string Token { get; set; }
        public string Created { get; set; }
        public string Target { get; set; }
    }

    public class Response
    {
        /// <summary>
        /// Question ID of Presented Question
        /// </summary>
        [Required]
        public string QuestionId { get; set; }
        /// <summary>
        /// Question Text as When Presented
        /// </summary>
        public string QuestionText { get; set; }
        /// <summary>
        /// Text Input If Question Accepts Text
        /// </summary>
        public string TextInput { get; set; }
        /// <summary>
        /// Text Input If Question Accepts Number
        /// </summary>
        public int NumberInput { get; set; }

    }
    #endregion

    #region AccountConfigurationManagement [ACM] Models

    public class AccountConfiguration
    {
        [BsonId]
        public string Id { get; set; }
        public List<DispatchChannel> DispatchChannels { get; set; }
        public Queue Queue { get; set; }
        public List<Vendor> Vendors { get; set; }
        public string WXMAPIKey { get; set; }
        public string WXMAdminUser { get; set; }
        public string WXMBaseURL { get; set; }
        public string WXMUser { get; set; }
        public Dictionary<string, string> ExtendedProperties { get; set; }
    }

    public class DispatchChannel
    {
        [Required]
        public string DispatchId { get; set; }
        [Required]
        public string DispatchName { get; set; }
        [Required]
        public ChannelDetails ChannelDetails { get; set; }
        [Required]
        public List<StaticPrefill> StaticPrefills { get; set; }
        [Required]
        public Notify Notify { get; set; }
    }

    public class Queue
    {
        public string QueueType { get; set; }
        public string QueueConnectionString { get; set; }
    }

    public class Vendor
    {
        [Required]
        public string VendorType { get; set; }
        [Required]
        public string VendorName { get; set; }
        [Required]
        public bool IsBulkVendor { get; set; }
        [Required]
        public Dictionary<string, string> VendorDetails { get; set; }
    }

    public class ChannelDetails
    {
        [Required]
        public Channel Email { get; set; }
        [Required]
        public Channel Sms { get; set; }
    }

    public class Channel
    {
        [Required]
        public bool IsValid { get; set; }
        public string Vendorname { get; set; }
    }

    public class TTLBearerToken
    {
        public BearerToken BearerToken { get; set; }
        public DateTime ExpiresAt { get; set; }
    }

    public class StaticPrefill
    {
        [Required]
        public string QuestionId { get; set; }
        [Required]
        public string Note { get; set; }
        public string PrefillValue { get; set; }
    }

    public class Notify
    {
        public string D { get; set; }
        public string I { get; set; }
        public string W { get; set; }
        public string E { get; set; }
        public string F { get; set; }
    }


    public class SPALoginRequest
    {
        [Required]
        public string Username { get; set; }
        [Required]
        public string Password { get; set; }
    }

    public class ACMLoginResponse
    {
        public bool IsSuccessful { get; set; }
        public string Message { get; set; }
    }

    public class ACMGenericResult<T>
    {
        public T Value { get; set; }
        public int StatusCode { get; set; }
    }

    public class DispatchesAndQueueDetails
    {
        public List<KeyValuePair<string, string>> Dispatches { get; set; }
        public Queue Queue { get; set; }
    }

    #endregion

    #region UnSubscibe
    public class Unsubscribed
    {
        [BsonId]
        public string Id { get; set; }
        public string Email { get; set; }
        public string Mobile { get; set; }
        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime UnsubscribedAt { get; set; }
    }
    #endregion

    #region SurveyQuestionnaire
    public class SurveyQuestionnaire
    {
        public bool isNameImmutable { get; set; }
        public string name { get; set; }
        public string displayName { get; set; }
        public string hashPIIBy { get; set; }
    }
    #endregion

    #region Dispatcher Related
    public class DB_MessagePayload
    {
        [BsonId]
        public string Id { get; set; }
        public string BulkVendorName { get; set; }
        public string Status { get; set; }
        public DateTime InsertTime { get; set; }
        public string MessagePayload { get; set; }
    }
    #endregion
}