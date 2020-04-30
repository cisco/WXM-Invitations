using XM.ID.Invitations.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


namespace Invitations.Controllers
{
    [ApiController]
    [Route("api")]
    public class InvitationsAPIController : ControllerBase
    {
        private readonly IConfiguration Config;
        private readonly AuthTokenValidation AuthTokenValidation;
        private readonly ViaMongoDB ViaMongoDB;
        private readonly PayloadValidation PayloadValidation;
        private readonly EventLogList EventLogList;

        public InvitationsAPIController(IConfiguration config, AuthTokenValidation authTokenValidation, 
            ViaMongoDB viaMongoDB, PayloadValidation payloadValidation, EventLogList eventLogList)
        {
            Config = config;
            AuthTokenValidation = authTokenValidation;
            ViaMongoDB = viaMongoDB;
            PayloadValidation = payloadValidation;
            EventLogList = eventLogList;
        }

        [HttpPost]
        [Route("dispatchRequest")]
        public async Task<IActionResult> DispatchRequest([FromHeader(Name = "Authorization")] string authToken, 
            List<DispatchRequest> request)
        {
            try
            {

                if (request == null)
                    return BadRequest("Bad Request");

                // Fetch account configuration to be used through the whole request.
                AccountConfiguration accConfiguration = GetAccountConfiguration().Result;
                if(accConfiguration == null)
                {
                    EventLogList.AddEventByLevel(2, SharedSettings.NoConfigInSPA, null);
                    await EventLogList.AddEventLogs(ViaMongoDB);
                    return StatusCode(Microsoft.AspNetCore.Http.StatusCodes.Status500InternalServerError, SharedSettings.NoConfigInSPA);
                }

                // Validate Auth token(Basic or Bearer) and reject if fail.
                if (!AuthTokenValidation.ValidateBearerToken(authToken, accConfiguration))
                {
                    EventLogList.AddEventByLevel(2, SharedSettings.AuthorizationDenied,null,null);
                    await EventLogList.AddEventLogs(ViaMongoDB);
                    return Unauthorized(SharedSettings.AuthorizationDenied);
                }

                // Check for Payload size and number of Dispatches
                if (!PayloadValidation.ValidateRequestPayloadSize(request, EventLogList))
                {
                    await EventLogList.AddEventLogs(ViaMongoDB);
                    return StatusCode(Microsoft.AspNetCore.Http.StatusCodes.Status413PayloadTooLarge, SharedSettings.PayLoadTooLarge);
                }

                //Generate batch ID for the request
                string batchId = Guid.NewGuid().ToString();

                // Check for sampling
                if (accConfiguration.ExtendedProperties.TryGetValue("Sampler", out string samplername))
                    if (SharedSettings.AvailableSamplers.TryGetValue(samplername, out ISampler sampler))
                        await sampler.IsSampledAsync(request);
                    else
                    {
                        EventLogList.AddEventByLevel(4, SharedSettings.NoSamplingConfigured, null);
                    }

                BatchResponse batchResponse = new BatchResponse()
                {
                    BatchId = batchId,
                    StatusByDispatch = new List<StatusByDispatch>()
                };

                try
                {
                    ProcessInvitations processInvitations = new ProcessInvitations(authToken, ViaMongoDB, batchId,
                        EventLogList, accConfiguration);

                    bool res = processInvitations.GetAllInfoForDispatch();
                    if (!res)
                        throw new Exception("Retrieval of all API responses for a Dispatch is not successful");

                    await processInvitations.CheckDispatchData(request, batchId, batchResponse);
                }
                catch (Exception ex)
                {
                    EventLogList.AddExceptionEvent(ex, batchId);
                    await EventLogList.AddEventLogs(ViaMongoDB);
                    return ex.Message switch
                    {
                        SharedSettings.AuthorizationDenied => Unauthorized(ex.Message),
                        _ => StatusCode(Microsoft.AspNetCore.Http.StatusCodes.Status500InternalServerError, ex.Message)
                    };
                }
                EventLogList.AddEventByLevel(5, $"Multiple dispatch status returned", batchId, null);
                await EventLogList.AddEventLogs(ViaMongoDB);

                return StatusCode(Microsoft.AspNetCore.Http.StatusCodes.Status207MultiStatus, batchResponse);

            }
            catch (Exception ex)
            {
                EventLogList.AddExceptionEvent(ex, null, null, null, null, "Exception in DispatchRequest Controller");
                await EventLogList.AddEventLogs(ViaMongoDB);
                return ex.Message switch
                {
                    SharedSettings.AuthorizationDenied => Unauthorized(ex.Message),
                    _ => StatusCode(Microsoft.AspNetCore.Http.StatusCodes.Status500InternalServerError, ex.Message)
                };
            }
        }

        private async Task<AccountConfiguration> GetAccountConfiguration()
        {
            // Fetch AccountConfiguration stored in DB to validate the user
            AccountConfiguration accountConfiguration;
            var accountConfigurationCache = InvitationsMemoryCache.GetInstance().GetFromMemoryCache("accountconfig");
            if (accountConfigurationCache == null)
            {
                accountConfiguration = await ViaMongoDB.GetAccountConfiguration();
                InvitationsMemoryCache.GetInstance().SetToMemoryCache("accountconfig", JsonConvert.SerializeObject(accountConfiguration));
            }
            else
            {
                accountConfiguration = JsonConvert.DeserializeObject<AccountConfiguration>(accountConfigurationCache);
            }

            return accountConfiguration;
        }

        [HttpPost]
        [Route("EventLog")]
        public async Task<IActionResult> GetEventLog([FromHeader(Name = "Authorization")] string authToken, 
            ActivityFilter filterObject)
        {
            //{"BatchId":"","DispatchId":"","Token":"","Created":"","Target":""} request format
            try
            {
                // Validate Auth token(Basic or Bearer) and reject if fail.
                if (!await AuthTokenValidation.ValidateBearerToken(authToken))
                {
                    return Unauthorized(SharedSettings.AuthorizationDenied);
                }

                if (string.IsNullOrWhiteSpace(filterObject.BatchId) &&
                    string.IsNullOrWhiteSpace(filterObject.DispatchId) &&
                    string.IsNullOrWhiteSpace(filterObject.Token) &&
                    string.IsNullOrWhiteSpace(filterObject.Target) &&
                    string.IsNullOrWhiteSpace(filterObject.Created))
                    return BadRequest("EventLog filters are empty.");

                var response = await ViaMongoDB.GetActivityDocuments(filterObject);
                return StatusCode(Microsoft.AspNetCore.Http.StatusCodes.Status200OK, response);
            }
            catch (Exception ex)
            {
                Console.WriteLine("exception: ", ex.Message);
                return BadRequest(ex.Message);
            }
        }

        [HttpGet]
        [Route("dispatchSingle/{dispatchID}")]

        public async Task<IActionResult> DispatchSingle(string dispatchID, [FromHeader(Name = "Authorization")] string authToken)
        {
            try
            {
                var parameters = Request.Query.ToDictionary(q => q.Key, q => q.Value);

                if (parameters.Count == 0)
                    return BadRequest();

                List<DispatchRequest> dispatchRequests = new List<DispatchRequest>();
                DispatchRequest dispatchRequest = new DispatchRequest()
                {
                    DispatchID = dispatchID,
                    PreFill = new List<List<PreFillValue>>()
                };
                List<PreFillValue> preFillValues = new List<PreFillValue>();
                foreach (var parameter in parameters)
                {
                    PreFillValue preFillValue = new PreFillValue()
                    {
                        questionId = parameter.Key,
                        input = parameter.Value
                    };
                    preFillValues.Add(preFillValue);
                }
                dispatchRequest.PreFill.Add(preFillValues);
                dispatchRequests.Add(dispatchRequest);
                return await DispatchRequest(authToken, dispatchRequests);
            }
            catch (Exception ex)
            {
                Console.WriteLine("exception", ex.Message);
                return BadRequest(ex.Message);
            }
        }

    }
}