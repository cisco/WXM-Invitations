using System.Collections.Generic;

namespace XM.ID.Invitations.Net
{
    public static class SharedSettings
    {
        public const string AuthorizationDenied = "Authorization has been denied for this request.";
        public const string MaxRecordSizeExceedeed = "Maximum number record supported exceeded";
        public const string MaxDispatchNumberExceeded = "Maximum number of dispatch per call supported exceeded";
        public const string NoQuestionnaireFound = "No Questionnaire found.";
        public const string NoDeliveryPlanFound = "No Delivery Plan found.";
        public const string NoDispatchFound = "No Dispatch found.";
        public const string NoSettingsFound = "Not able to fetch WXM settings";
        public const string NoHashAlogConfigured = "No Hashing algo configured, switching to default: sha512";
        public const string UnsupportedHashAlogConfigured = "Unsupported hashing algo, switching to default";
        public const string BulkAPIFailureMessage = "Bulk token failed";
        public const string InvalidOrUnsupportedChannels = "No valid/supported channels found in the dispatch";
        public const string UniQueIdQuestionMissingInDP = "No valid UniqueId question configured in the DP";
        public const string BadRequest = "Bad Request.";
        public const string InteralError = "Internal Error";
        public const string Sampledrecord = "Record sampled";
        public const string NoSamplingConfigured = "No Sampling Configured";
        public const string NoConfigInSPA = "Account is not completely setup for Invitation delivery";
        public const string NoDispatchInSPA = "DispatchID not present in SPA record.";
        public const string InvalidDispatch = "Dispatch Invalid";
        public const string PausedDispatch = "Dispatch paused";
        public const string NovalidDispatchInTheBatch = "No valid dispatch request in the batch found";
        public const string PausedDP = "Underlying DP is paused";
        public const string AllRecordsRejected = "All records are rejected";
        public const string AcceptedForProcessing = "Accepted for processing";
        public const string FailDueToEmailOrMobile = "Failed due to invalid Email or mobile number";
        public const string FailDueToUUIDOrChannel = "Failed due to no Common Identifier or Channel";
        public const string DuplicateRecord = "Duplicate record found";
        public const string PayLoadTooLarge = "Payload size is larger than configured limit.";

        public const string ALL_DISPATCH_API_URI = "/api/Dispatches";
        public const string SURVEY_QUESTIONNAIRE = "/api/AllSurveyQuestionnaires";
        public const string ALL_Delivery_ID = "/api/DeliveryPlan";
        public const string ACTIVE_QUES = "/api/Questions/Active";
        public const string BULK_TOKEN_API = "/api/SurveyByToken/Import/Dispatch";
        public const string SETTINGS_API = "/api/settings";
        public const string GET_APIKEY_API = "/api/GetAPIKey";
        public const string GET_DISPATCHES_API = "/api/Dispatches";
        public const string GET_DELIVERY_PLANS_API = "/api/DeliveryPlan";
        public const string GET_ACTIVE_QUES_API = "/api/Questions/Active";
        public const string GET_LOGIN_TOKEN_API = "/api/logintoken";
        public const string GET_DISPATCH_BY_ID_API = "/api/Dispatches/";
        public const string GET_DP_BY_ID_API = "/api/DeliveryPlan/";
        public const string GET_QUES_BY_QNR_API = "/api/Questions/Questionnaire";
        public const string GET_LOGIN_TOKEN = "/api/LoginToken";

        public static string BASE_URL;
        public static double AuthTokenCacheExpiryInSeconds;
        public static double CacheExpiryInSeconds;

        public static Dictionary<string, ISampler> AvailableSamplers = new Dictionary<string, ISampler>
        {
            { "wxm", new WXMSampler() }
        };
        public static Dictionary<string, IUnsubscribeChecker> AvailableUnsubscribeCheckers = new Dictionary<string, IUnsubscribeChecker>
        {
            { "wxm", new WXMUnsubscribeChecker() }
        };
        public static Dictionary<string, IBatchingQueue<RequestBulkToken>> AvailableQueues = new Dictionary<string, IBatchingQueue<RequestBulkToken>>
        {
            { "inmemory", SingletonConcurrentQueue<RequestBulkToken>.Instance }
        };

    }

}
