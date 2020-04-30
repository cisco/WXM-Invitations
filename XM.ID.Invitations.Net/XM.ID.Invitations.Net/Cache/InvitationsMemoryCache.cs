using Microsoft.Extensions.Caching.Memory;
using System;
using System.Threading.Tasks;

namespace XM.ID.Invitations.Net
{
    public class InvitationsMemoryCache
    {
        private readonly object dispatchLock = new object();
        private readonly object dpLock = new object();
        private readonly object questionsLock = new object();
        private readonly object settingsLock = new object();
        private readonly object questionniareLock = new object();

        private readonly HTTPWrapper hTTPWrapper = new HTTPWrapper();

        private readonly MemoryCache Cache = new MemoryCache(new MemoryCacheOptions
        {
        });

        private readonly MemoryCacheEntryOptions cacheEntryOptionsCache = new MemoryCacheEntryOptions()
                    // Keep in cache for this time, reset time if accessed.
                    .SetAbsoluteExpiration(TimeSpan.FromSeconds(
                        (SharedSettings.CacheExpiryInSeconds == 0) ? 3600 : SharedSettings.CacheExpiryInSeconds
                        ));

        private readonly MemoryCacheEntryOptions cacheEntryOptionsAuthToken = new MemoryCacheEntryOptions()
                    // Keep in cache for this time, reset time if accessed.
                    .SetAbsoluteExpiration(TimeSpan.FromSeconds(
                        (SharedSettings.AuthTokenCacheExpiryInSeconds == 0) ? 900 : SharedSettings.AuthTokenCacheExpiryInSeconds
                        ));

        private readonly MemoryCacheEntryOptions bulkTokenAuth = new MemoryCacheEntryOptions();

        private static InvitationsMemoryCache _instance = new InvitationsMemoryCache();

        public void SetToMemoryCache(string key, string value)
        {
            // Save data in cache.
            Cache.Set(key, value, cacheEntryOptionsCache);
        }

        public void SetAuthTokenToMemoryCache(string authToken)
        {
            // Save data in cache.
            Cache.Set(authToken, "true", cacheEntryOptionsAuthToken);
        }

        public void SetBulkTokenAuthToMemoryCache(string key, string value, double seconds)
        {
            bulkTokenAuth.SetAbsoluteExpiration(TimeSpan.FromSeconds(seconds));
            Cache.Set(key, value, bulkTokenAuth);
        }

        public void SetToMemoryCacheSliding(string key, string value)
        {
            var cacheEntryOptions = new MemoryCacheEntryOptions()
                    // Keep in cache for this time, reset time if accessed.
                    .SetSlidingExpiration(TimeSpan.FromSeconds(
                        (SharedSettings.CacheExpiryInSeconds == 0) ? 3600 : SharedSettings.CacheExpiryInSeconds
                        ));

            // Save data in cache.
            Cache.Set(key, value, cacheEntryOptions);
        }


        public string GetDispatchDataFromMemoryCache(string authToken)
        {
            if (Cache.TryGetValue("DispatchData", out string value))
                return value;
            else
            {
                string DispatchData;
                lock (dispatchLock)
                {
                    if (Cache.TryGetValue("DispatchData", out string newValue))
                        return newValue;

                    DispatchData = hTTPWrapper.GetAllDispatchInfo(authToken).GetAwaiter().GetResult();
                    if (string.IsNullOrEmpty(DispatchData))
                    {
                        return null;
                    }

                    SetToMemoryCache("DispatchData", DispatchData);
                }
                return DispatchData;
            }
        }

        public string GetDeliveryPlanFromMemoryCache(string authToken)
        {
            if (Cache.TryGetValue("DeliveryPlanData", out string value))
                return value;
            else
            {
                string DeliveryPlanData;
                lock (dpLock)
                {
                    if (Cache.TryGetValue("DeliveryPlanData", out string newValue))
                        return newValue;

                    DeliveryPlanData = hTTPWrapper.GetDeliveryPlans(authToken).GetAwaiter().GetResult();
                    if (string.IsNullOrEmpty(DeliveryPlanData))
                    {
                        return null;
                    }

                    SetToMemoryCache("DeliveryPlanData", DeliveryPlanData);
                }
                return DeliveryPlanData;
            }
        }

        public string GetActiveQuestionsFromMemoryCache(string authToken)
        {
            if (Cache.TryGetValue("ActiveQuestions", out string value))
                return value;
            else
            {
                string ActiveQuestions;
                lock (questionsLock)
                {
                    if (Cache.TryGetValue("ActiveQuestions", out string newValue))
                        return newValue;

                    ActiveQuestions = hTTPWrapper.GetActiveQuestions(authToken).GetAwaiter().GetResult();
                    if (string.IsNullOrEmpty(ActiveQuestions))
                    {
                        return null;
                    }

                    SetToMemoryCache("ActiveQuestions", ActiveQuestions);
                }
                return ActiveQuestions;
            }
        }


        public string GetSettingsFromMemoryCache(string authToken)
        {
            if (Cache.TryGetValue("settings", out string value))
                return value;
            else
            {
                string Settings;
                lock (settingsLock)
                {
                    if (Cache.TryGetValue("settings", out string newValue))
                        return newValue;

                    Settings = hTTPWrapper.GetSettings(authToken).GetAwaiter().GetResult();
                    if (string.IsNullOrEmpty(Settings))
                    {
                        return null;
                    }

                    SetToMemoryCache("settings", Settings);
                }
                return Settings;
            }
        }

        public string GetQuestionnaireFromMemoryCache(string authToken)
        {
            if (Cache.TryGetValue("SurveyQuestionnaires", out string value))
                return value;
            else
            {
                string SurveyQuestionnaires;
                lock (questionniareLock)
                {
                    if (Cache.TryGetValue("SurveyQuestionnaires", out string newValue))
                        return newValue;

                    SurveyQuestionnaires = hTTPWrapper.GetSurveyQuestionnaire(authToken).GetAwaiter().GetResult();
                    if (string.IsNullOrEmpty(SurveyQuestionnaires))
                    {
                        return null;
                    }

                    SetToMemoryCache("SurveyQuestionnaires", SurveyQuestionnaires);
                }
                return SurveyQuestionnaires;
            }
        }

        public string GetFromMemoryCache(string key)
        {
            if (Cache.TryGetValue(key, out string value))
            {
                return value;
            }
            return null;
        }

        public void RemoveFromMemoryCache(string key)
        {
            Cache.Remove(key);
        }

        public static InvitationsMemoryCache GetInstance()
        {
            return _instance;
        }
    }
}
