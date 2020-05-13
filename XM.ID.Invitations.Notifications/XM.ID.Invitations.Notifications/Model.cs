using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Text;

namespace InvitationNotification
{
    #region Invitation-Dispatcher
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
        public bool IsNotified { get; set; }

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

        public enum EventChannel { Email, SMS, DispatchAPI };
        public enum EventAction
        {
            Requested, Rejected, TokenCreated, Sent, Error, Supressed,
            Dispatcher_QT_Processing, Dispatcher_QT_Success, Dispatcher_QT_Failure, //QT = QueueTriggerFunction Actions
            Dispatcher_TT_Processing, Dispatcher_TT_Success, Dispatcher_TT_Failure, //TT = TimeTriggerFucntion Actions
            Throttled
        };


    }

    [BsonIgnoreExtraElements]
    public class DeliveryEventStatus
    {
        public int Requested { get; set; }
        public int Accepetd { get; set; }
        public int Rejected { get; set; }
    }

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
    public class AccountConfiguration
    {
        [BsonId]
        public string Id { get; set; }
        public List<DispatchChannel> DispatchChannels { get; set; }
        public string WXMAPIKey { get; set; }
        public string WXMAdminUser { get; set; }
        public string WXMBaseURL { get; set; }
        public string WXMUser { get; set; }
        public Dictionary<string, string> ExtendedProperties { get; set; }
    }

    public class DispatchChannel
    {
        public string DispatchId { get; set; }

        public string DispatchName { get; set; }

        public ChannelDetails ChannelDetails { get; set; }

        public List<StaticPrefill> StaticPrefills { get; set; }

        public Notify Notify { get; set; }
    }
    public class Notify
    {
        public string D { get; set; } //madhu@getcloudhcerry
        public string I { get; set; }
        public string W { get; set; }
        public string E { get; set; }
        public string F { get; set; }
    }

    public class ChannelDetails
    {
        public Channel Email { get; set; }

        public Channel Sms { get; set; }
    }

    public class Channel
    {
        public bool IsValid { get; set; }
        public string Vendorname { get; set; }
    }


    public class StaticPrefill
    {
        public string QuestionId { get; set; }

        public string Note { get; set; }
        public string PrefillValue { get; set; }
    }
    #endregion

    #region Notification Configs
    public class Configuration
    {
        public string MongoConnectionStriong = "";

    }
    public class EmailTemplateViewModel
    {

    }
    public class SMTPServer
    {
        /// <summary>
        /// ex: Your Company Name
        /// </summary>

        public string FromName { get; set; }

        /// <summary>
        /// ex: address@yourserver.net
        /// </summary>

        public string FromAddress { get; set; }

        /// <summary>
        /// ex: smtp.yoursever.net
        /// </summary>
        public string Server { get; set; }

        /// <summary>
        /// Usually address@yourserver.net
        /// </summary>
        public string Login { get; set; }

        /// <summary>
        /// Password to send email
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        /// Ex: 587(Submission), 25(Classic SMTP)
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        /// Set to require Secure SSL Connection
        /// </summary>
        public bool EnableSSL { get; set; }

        public override string ToString()
        {
            string text = Server + ":" + Port + " SSL:" + EnableSSL + " Login:" + Login + " From:" + FromAddress;
            return text;
        }
    }
    public class Frequency
    {
        public string Every { get; set; }
        public int Hour { get; set; }
        public int Minute { get; set; }
        public int RealtImeMaxLevel { get; set; }

    }

    public class ProjectedLog
    {
        public DateTime Created { get; set; }
        public string BatchId { get; set; }
        public string DispatchId { get; set; }
        public string LogLevel { get; set; }
        public string Message { get; set; }
    }
    #endregion
}
