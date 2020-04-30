using Newtonsoft.Json;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace XM.ID.Invitations.Net
{
    public class WXMService
    {
        private HttpClient HttpClient;
        
        public WXMService()
        {
            HttpClient = new HttpClient();
        }
        
        public async Task<BearerToken> GetLoginToken(string username, string password)
        {
            string requestUri = SharedSettings.BASE_URL + SharedSettings.GET_LOGIN_TOKEN_API;
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, requestUri);
            List<KeyValuePair<string, string>> requestPostValues = new List<KeyValuePair<string, string>>
            {
                { new KeyValuePair<string,string>("grant_type", "password") },
                { new KeyValuePair<string,string>("username", username) },
                { new KeyValuePair<string,string>("password", password) }
            };
            request.Content = new FormUrlEncodedContent(requestPostValues);
            HttpResponseMessage response = await HttpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                return default;
            }
            string stringBearerToken = await response.Content.ReadAsStringAsync();
            BearerToken bearerToken = JsonConvert.DeserializeObject<BearerToken>(stringBearerToken);
            return bearerToken;
        }
           
        private async Task<T> MakeHttpRequestAsync<T>(string bearerToken, string httpMethod, string requestUri, string jsonBody = null)
        {
            HttpRequestMessage request = httpMethod switch
            {
                "POST" => new HttpRequestMessage(HttpMethod.Post, requestUri),
                "GET" => new HttpRequestMessage(HttpMethod.Get, requestUri),
                "PUT" => new HttpRequestMessage(HttpMethod.Put, requestUri),
                "DELETE" => new HttpRequestMessage(HttpMethod.Delete, requestUri),
                _ => new HttpRequestMessage(HttpMethod.Options, requestUri),
            };
            request.Headers.Add("Authorization", bearerToken);
            if(!string.IsNullOrWhiteSpace(jsonBody))
                request.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
            HttpResponseMessage response = await HttpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                return default;
            }
            string stringResponse = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<T>(stringResponse);
        }

        public async Task<string> GetAPIKey(string bearerToken)
        {
            string uri = SharedSettings.BASE_URL + SharedSettings.GET_APIKEY_API;
            return await MakeHttpRequestAsync<string>(bearerToken, "GET", uri);
        }

        public async Task<List<Dispatch>> GetDispatches(string bearerToken)
        {
            string uri = SharedSettings.BASE_URL + SharedSettings.GET_DISPATCHES_API;
            return await MakeHttpRequestAsync<List<Dispatch>>(bearerToken, "GET", uri);
        }

        public async Task<List<DeliveryPlan>> GetDeliveryPlans(string bearerToken)
        {
            string uri = SharedSettings.BASE_URL + SharedSettings.GET_DELIVERY_PLANS_API;
            return await MakeHttpRequestAsync<List<DeliveryPlan>>(bearerToken, "GET", uri);
        }

        public async Task<List<Question>> GetActiveQuestions(string bearerToken)
        {
            string uri = SharedSettings.BASE_URL + SharedSettings.GET_ACTIVE_QUES_API;
            return await MakeHttpRequestAsync<List<Question>>(bearerToken, "GET", uri);
        }

        public async Task<Settings> GetSettings(string bearerToken)
        {
            string uri = SharedSettings.BASE_URL + SharedSettings.SETTINGS_API;
            return await MakeHttpRequestAsync<Settings>(bearerToken, "GET", uri);
        }

        public async Task<Dispatch> GetDispatchById(string bearerToken, string dispatchId)
        {
            string uri = SharedSettings.BASE_URL + SharedSettings.GET_DISPATCH_BY_ID_API + dispatchId;
            return await MakeHttpRequestAsync<Dispatch>(bearerToken, "GET", uri);
        }

        public async Task<DeliveryPlan> GetDeliveryPlanById(string bearerToken, string deliveryPlanId)
        {
            string uri = SharedSettings.BASE_URL + SharedSettings.GET_DP_BY_ID_API + deliveryPlanId;
            return await MakeHttpRequestAsync<DeliveryPlan>(bearerToken, "GET", uri);
        }

        public async Task<List<Question>> GetQuestionsByQNR(string bearerToken, string qnr)
        {
            string uri = SharedSettings.BASE_URL + SharedSettings.GET_QUES_BY_QNR_API;
            Dictionary<string, string> body = new Dictionary<string, string>
            {
                {"name",qnr }
            };
            string jsonBody = JsonConvert.SerializeObject(body);
            return await MakeHttpRequestAsync<List<Question>>(bearerToken, "POST", uri, jsonBody);
        }
    }
}