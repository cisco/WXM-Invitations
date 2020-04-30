using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace XM.ID.Invitations.Net
{
    public class ConfigService
    {
        private ViaMongoDB ViaMongoDB;
        private WXMService WXMService;


        public ConfigService(ViaMongoDB viaMongoDB, WXMService wXMService)
        {
            ViaMongoDB = viaMongoDB;
            WXMService = wXMService;
        }

        public async Task<bool> IsUserAdminValid(string wxmAdmin)
        {
            AccountConfiguration accountConfiguration = await ViaMongoDB.GetAccountConfiguration();
            string dbAdmin = accountConfiguration?.WXMAdminUser;
            if (string.IsNullOrWhiteSpace(dbAdmin))
                return true;
            else
                return wxmAdmin == dbAdmin;
        }

        public async Task<ACMLoginResponse> Login(SPALoginRequest request)
        {
            ACMLoginResponse response = new ACMLoginResponse();
            try
            {
                BearerToken bearerToken = await WXMService.GetLoginToken(request.Username, request.Password);
                if (bearerToken == default || !(await IsUserAdminValid(bearerToken.ManagedBy)))
                {
                    response.IsSuccessful = false;
                    response.Message = "Incorrect Username/Password";
                }
                else
                {
                    string APIKey = await WXMService.GetAPIKey("Bearer " + bearerToken.AccessToken);
                    if (APIKey == default)
                    {
                        response.IsSuccessful = false;
                        response.Message = "No API Key found at WXM";
                    }
                    else
                    {
                        var tryUpdate = await ViaMongoDB.AddOrUpdateAccountConfiguration_WXMFields(bearerToken.ManagedBy, APIKey, bearerToken.UserName, SharedSettings.BASE_URL);
                        if (tryUpdate == default)
                        {
                            response.IsSuccessful = false;
                            response.Message = "Account Configuration Update Has Failed";
                        }
                        else
                        {
                            response.IsSuccessful = true;
                            response.Message = bearerToken.AccessToken;
                        }
                    }
                }
            }
            catch (Exception)
            {
                response.IsSuccessful = false;
                response.Message = "Internal Exception";
            }
            return response;
        }

        public async Task<ACMGenericResult<DispatchesAndQueueDetails>> GetDispatches(string bearerToken)
        {
            var result = new ACMGenericResult<DispatchesAndQueueDetails>();
            try
            {
                List<Dispatch> dispatches = await WXMService.GetDispatches(bearerToken);
                List<DeliveryPlan> deliveryPlans = await WXMService.GetDeliveryPlans(bearerToken) ?? new List<DeliveryPlan>();
                List<Question> preFillQuestions = (await WXMService.GetActiveQuestions(bearerToken)).Where(x => x.StaffFill == true)?.ToList() ?? new List<Question>();
                Settings settings = await WXMService.GetSettings(bearerToken);

                if (dispatches?.Count > 0 == false || settings == null)
                {
                    result.StatusCode = 400;
                    result.Value = null;
                }
                else
                {
                    var configuredDispatchChannels = await ConfigureDispatchChannels(dispatches, deliveryPlans, preFillQuestions);
                    var configuredQueue = ConfigureQueueDetails(settings);
                    result.StatusCode = 200;
                    result.Value = new DispatchesAndQueueDetails
                    {
                        Dispatches = configuredDispatchChannels?
                        .Select(x => new KeyValuePair<string, string>(x.DispatchId, x.DispatchName))?
                        .ToList() ?? new List<KeyValuePair<string, string>>(),
                        Queue = configuredQueue
                    };
                }
            }
            catch (Exception)
            {
                result.StatusCode = 500;
                result.Value = null;
            }
            return result;
        }

        public async Task<List<DispatchChannel>> ConfigureDispatchChannels(List<Dispatch> dispatches, List<DeliveryPlan> deliveryPlans, List<Question> preFillQuestions)
        {
            List<DispatchChannel> dispatchChannels = new List<DispatchChannel>();
            foreach (Dispatch dispatch in dispatches)
            {
                DeliveryPlan deliveryPlan = deliveryPlans.Find(x => x.id == dispatch.DeliveryPlanId);
                List<StaticPrefill> staticPrefills = preFillQuestions
                    .Where(x => x.DisplayLocation?.Contains(dispatch.QuestionnaireName, StringComparer.InvariantCultureIgnoreCase) ?? false)?
                    .Select(x => new StaticPrefill { Note = x.Note, PrefillValue = null, QuestionId = x.Id })?
                    .ToList() ?? new List<StaticPrefill>();
                DispatchChannel dispatchChannel = new DispatchChannel
                {
                    ChannelDetails = new ChannelDetails
                    {
                        Email = new Channel
                        {
                            IsValid = deliveryPlan?.schedule?.Any(x => x.onChannel?.StartsWith("email") ?? false) ?? false ? true : false,
                            Vendorname = null
                        },
                        Sms = new Channel
                        {
                            IsValid = deliveryPlan?.schedule?.Any(x => x.onChannel?.StartsWith("sms") ?? false) ?? false ? true : false,
                            Vendorname = null
                        }
                    },
                    DispatchId = dispatch.Id,
                    DispatchName = dispatch.Name + (dispatch.IsLive == true ? string.Empty : " [PAUSED]"),
                    StaticPrefills = staticPrefills,
                    Notify = new Notify
                    {
                        D = null,
                        E = null,
                        F = null,
                        I = null,
                        W = null
                    }
                };
                dispatchChannels.Add(dispatchChannel);
            }

            AccountConfiguration accountConfiguration = await ViaMongoDB.GetAccountConfiguration();
            if (accountConfiguration.DispatchChannels == null)
                accountConfiguration.DispatchChannels = dispatchChannels;
            else
            {
                foreach (DispatchChannel dc in dispatchChannels)
                {
                    int index1 = accountConfiguration.DispatchChannels.FindIndex(x => x.DispatchId == dc.DispatchId);
                    if (index1 == -1)                                                                                   //Add new dispatch channel                                                                                   
                        accountConfiguration.DispatchChannels.Add(dc);
                    else                                                                                                //Update existing dispatch channel
                    {
                        accountConfiguration.DispatchChannels[index1].ChannelDetails.Email.IsValid = dc.ChannelDetails.Email.IsValid;
                        accountConfiguration.DispatchChannels[index1].ChannelDetails.Sms.IsValid = dc.ChannelDetails.Sms.IsValid;

                        accountConfiguration.DispatchChannels[index1].DispatchName = dc.DispatchName;

                        foreach (StaticPrefill sp in dc.StaticPrefills)
                        {
                            int index2 = accountConfiguration.DispatchChannels[index1].StaticPrefills.FindIndex(x => x.QuestionId == sp.QuestionId);
                            if (index2 == -1)
                                accountConfiguration.DispatchChannels[index1].StaticPrefills.Add(sp);                   //Add new static prefill
                            else
                                accountConfiguration.DispatchChannels[index1].StaticPrefills[index2].Note = sp.Note;    //Update existing static prefill
                        }
                        accountConfiguration.DispatchChannels[index1].
                            StaticPrefills.RemoveAll(x => dc.StaticPrefills.All(y => y.QuestionId != x.QuestionId));    //Remove old static prefills

                        if (accountConfiguration.DispatchChannels[index1].Notify == default)
                            accountConfiguration.DispatchChannels[index1].Notify = dc.Notify;
                    }
                }
                accountConfiguration.DispatchChannels                                                                   //Remove old dispatch channels
                    .RemoveAll(x => dispatchChannels.All(y => y.DispatchId != x.DispatchId));
            }
            return (await ViaMongoDB.UpdateAccountConfiguration_DispatchChannels(accountConfiguration.DispatchChannels)).DispatchChannels;
        }

        public Queue ConfigureQueueDetails(Settings settings)
        {
            string queueName = settings.Integrations?.QueueDetails?.ElementAt(0)?.QueueName;
            string queueConnectionString = settings.Integrations?.QueueDetails?.ElementAt(0)?.ConnectionString;
            string queueType = settings.Integrations?.QueueDetails?.ElementAt(0)?.Type;
            if (string.IsNullOrWhiteSpace(queueName) || string.IsNullOrWhiteSpace(queueConnectionString) || string.IsNullOrWhiteSpace(queueType))
                return new Queue
                {
                    QueueConnectionString = "Details unavailable. Please check this in Experience Management",
                    QueueType = "Details unavailable. Please check this in Experience Management"
                };
            else
                return new Queue
                {
                    QueueConnectionString = queueName + "@" + queueConnectionString,
                    QueueType = queueType
                };
        }

        public async Task<ACMGenericResult<DispatchChannel>> GetDispatchChannel(string dispatchId)
        {
            var result = new ACMGenericResult<DispatchChannel>();
            try
            {
                AccountConfiguration accountConfiguration = await ViaMongoDB.GetAccountConfiguration();
                if (accountConfiguration.DispatchChannels == null)
                {
                    result.StatusCode = 204;
                    result.Value = null;
                }
                else
                {
                    DispatchChannel dispatchChannel = accountConfiguration.DispatchChannels.Find(x => x.DispatchId == dispatchId);
                    if (dispatchChannel == default)
                    {
                        result.StatusCode = 204;
                        result.Value = null;
                    }
                    else
                    {
                        result.StatusCode = 200;
                        result.Value = dispatchChannel;
                    }
                }
            }
            catch (Exception)
            {
                result.StatusCode = 500;
                result.Value = null;
            }
            return result;
        }

        public async Task<ACMGenericResult<DispatchChannel>> AddOrUpdateDispatchChannel(DispatchChannel dispatchChannel)
        {
            var result = new ACMGenericResult<DispatchChannel>();
            try
            {
                AccountConfiguration accountConfiguration = await ViaMongoDB.GetAccountConfiguration();
                if (accountConfiguration.DispatchChannels == null)
                    accountConfiguration.DispatchChannels = new List<DispatchChannel> { dispatchChannel };
                else
                {
                    int index = accountConfiguration.DispatchChannels.FindIndex(x => x.DispatchId == dispatchChannel.DispatchId);
                    if (index == -1)
                        accountConfiguration.DispatchChannels.Add(dispatchChannel);
                    else
                        accountConfiguration.DispatchChannels[index] = ToClone(dispatchChannel);
                }
                result.StatusCode = 200;
                result.Value = (await ViaMongoDB.UpdateAccountConfiguration_DispatchChannels(accountConfiguration.DispatchChannels))
                    .DispatchChannels?.Find(x => x.DispatchId == dispatchChannel.DispatchId);
            }
            catch (Exception)
            {
                result.StatusCode = 500;
                result.Value = null;
            }
            return result;
        }

        public async Task<ACMGenericResult<Vendor>> GetVendor(string vendorName)
        {
            var result = new ACMGenericResult<Vendor>();
            try
            {
                AccountConfiguration accountConfiguration = await ViaMongoDB.GetAccountConfiguration();
                if (accountConfiguration.Vendors == null)
                {
                    result.StatusCode = 204;
                    result.Value = null;
                }
                else
                {
                    Vendor vendor = accountConfiguration.Vendors
                        .Find(x => string.Equals(x.VendorName, vendorName, StringComparison.InvariantCultureIgnoreCase));
                    if (vendor == default)
                    {
                        result.StatusCode = 204;
                        result.Value = null;
                    }
                    else
                    {
                        result.StatusCode = 200;
                        result.Value = vendor;
                    }
                }
            }
            catch (Exception)
            {
                result.StatusCode = 500;
                result.Value = null;
            }
            return result;
        }

        public async Task<ACMGenericResult<Vendor>> AddOrUpdateVendor(Vendor newVendor)
        {
            var result = new ACMGenericResult<Vendor>();
            try
            {
                AccountConfiguration accountConfiguration = await ViaMongoDB.GetAccountConfiguration();
                if (accountConfiguration.Vendors == null)
                    accountConfiguration.Vendors = new List<Vendor> { newVendor };
                else
                {
                    int index = accountConfiguration.Vendors.FindIndex(x => string.Equals(x.VendorName, newVendor.VendorName));
                    if (index == -1)
                        accountConfiguration.Vendors.Add(newVendor);
                    else
                        accountConfiguration.Vendors[index] = ToClone(newVendor);
                }
                result.StatusCode = 200;
                result.Value = (await ViaMongoDB.UpdateAccountConfiguration_Vendors(accountConfiguration.Vendors))
                    .Vendors.Find(x => string.Equals(x.VendorName, newVendor.VendorName));
            }
            catch (Exception)
            {
                result.StatusCode = 500;
                result.Value = null;
            }
            return result;
        }

        public async Task<ACMGenericResult<Dictionary<string, string>>> GetExtendedProperties()
        {
            var result = new ACMGenericResult<Dictionary<string, string>>();
            try
            {
                result.StatusCode = 200;
                result.Value = (await ViaMongoDB.GetAccountConfiguration()).ExtendedProperties;
            }
            catch (Exception)
            {
                result.StatusCode = 500;
                result.Value = null;
            }
            return result;
        }

        public async Task<ACMGenericResult<Dictionary<string, string>>> UpdateExtendedProperties(Dictionary<string, string> extendedProperties)
        {
            var result = new ACMGenericResult<Dictionary<string, string>>();
            try
            {
                result.StatusCode = 200;
                result.Value = (await ViaMongoDB.UpdateAccountConfiguration_ExtendedProperties(extendedProperties)).ExtendedProperties;
            }
            catch (Exception)
            {
                result.StatusCode = 500;
                result.Value = null;
            }
            return result;
        }

        public async Task<ACMGenericResult<string>> DeleteAccountConfiguration()
        {
            var deleteResponseObj = new ACMGenericResult<string>();
            try
            {
                await ViaMongoDB.DeleteAccountConfiguration();
                deleteResponseObj.StatusCode = 200;
                deleteResponseObj.Value = "Account has been cleared";
            }
            catch (Exception)
            {
                deleteResponseObj.StatusCode = 500;
                deleteResponseObj.Value = null;
            }
            return deleteResponseObj;
        }

        public T ToClone<T>(T obj)
        {
            return JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(obj));
        }
    }
}
