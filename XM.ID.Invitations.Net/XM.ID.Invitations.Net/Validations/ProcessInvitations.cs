using MongoDB.Bson;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace XM.ID.Invitations.Net
{
    public class ProcessInvitations
    {
        private Dictionary<string, RequestPrefill> CorrectDispatchData;

        public readonly Regex numberTypeRegEx = new Regex(@"^(?i)metric(?i)|^(?i)scale(?i)$|^(?i)slider(?i)$|(-\d)$|^(?i)number(?i)$|^(?i)date(?i)$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        public ViaMongoDB mongoDBConn;
        readonly HTTPWrapper hTTPWrapper;
        private readonly string FinalToken;
        private string DispatchData;
        private string DeliveryPlanData;
        private string ActiveQuestions;
        private string DispatchSettings;
        private string SurverQuestionnaires;
        private bool islocationMigrated;
        private string BatchId;
        private readonly EventLogList EventLogList;
        private readonly AccountConfiguration accountConfiguration;

        public ProcessInvitations(string AuthToken, ViaMongoDB viaMongo, string batchid, EventLogList eventLogList,
            AccountConfiguration accConfig)
        {
            FinalToken = AuthToken;
            mongoDBConn = viaMongo;
            hTTPWrapper = new HTTPWrapper(batchid, eventLogList);
            BatchId = batchid;
            EventLogList = eventLogList;
            accountConfiguration = accConfig;
        }

        public bool GetAllInfoForDispatch()
        {
            try
            {

                DispatchData = InvitationsMemoryCache.GetInstance().GetDispatchDataFromMemoryCache(FinalToken);
                DeliveryPlanData = InvitationsMemoryCache.GetInstance().GetDeliveryPlanFromMemoryCache(FinalToken);
                ActiveQuestions = InvitationsMemoryCache.GetInstance().GetActiveQuestionsFromMemoryCache(FinalToken);
                DispatchSettings = InvitationsMemoryCache.GetInstance().GetSettingsFromMemoryCache(FinalToken);

                if (DispatchData == null)
                {
                    EventLogList.AddEventByLevel(2, SharedSettings.NoDispatchFound, BatchId);
                    return false;
                }

                if (DeliveryPlanData == null)
                {
                    EventLogList.AddEventByLevel(2, SharedSettings.NoDeliveryPlanFound, BatchId);
                    return false;
                }

                if (ActiveQuestions == null)
                {
                    EventLogList.AddEventByLevel(2, SharedSettings.NoQuestionnaireFound, BatchId);
                    return false;
                }

                if (DispatchSettings == null)
                {
                    EventLogList.AddEventByLevel(2, SharedSettings.InteralError + " - Empty Settings", BatchId);
                    return false;
                } 
                else 
                { 
                    Settings settingsRes = JsonConvert.DeserializeObject<Settings>(DispatchSettings);
                    if (settingsRes.locationDataMigrated)
                    {
                        SurverQuestionnaires = InvitationsMemoryCache.GetInstance().GetQuestionnaireFromMemoryCache(FinalToken);
                        if (string.IsNullOrEmpty(SurverQuestionnaires))
                        {
                            EventLogList.AddEventByLevel(2, SharedSettings.InteralError + " - Empty Survey Questionnaires", BatchId);
                            return false;

                        }
                        islocationMigrated = true;
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                EventLogList.AddExceptionEvent(ex,null,null,null,null,"Getting details from MemoryCache failed");
                return false;
            }
        }

        private void CheckDispatchID(ref List<DispatchRequest> batchRequests, ref BatchResponse batchResponse, 
            List<Dispatch> allDispatches)
        {
            try
            {
                foreach (var batchreq in batchRequests)
                {
                    var reqDispatch = allDispatches.Find(dis => dis.Id == batchreq.DispatchID);
                    if (null != reqDispatch)
                    {
                        // Check if the dispatch is paused
                        if (!reqDispatch.IsLive)
                        {
                            if ((batchResponse.StatusByDispatch.Find(x => x.DispatchId == batchreq.DispatchID) == null))
                            {
                                EventLogList.AddEventByLevel(2, SharedSettings.PausedDispatch, BatchId, batchreq.DispatchID);
                                batchResponse.StatusByDispatch.Add(new StatusByDispatch()
                                {
                                    DispatchId = batchreq.DispatchID,
                                    DispatchStatus = "400",
                                    Message = SharedSettings.PausedDispatch
                                });
                            }
                        }
                        else
                        {
                            if (accountConfiguration?.DispatchChannels?.Find(x => x.DispatchId == batchreq.DispatchID) != null)
                            {
                                if (CorrectDispatchData.ContainsKey(batchreq.DispatchID))
                                {
                                    //This would not remove duplicates if any in the Prefills.
                                    CorrectDispatchData[batchreq.DispatchID].PreFill.AddRange(batchreq.PreFill);
                                }
                                else
                                {
                                    CorrectDispatchData.Add(batchreq.DispatchID, new RequestPrefill()
                                    {
                                        PreFill = batchreq.PreFill,
                                        DeliveryPlanID = reqDispatch.DeliveryPlanId,
                                        Channels = new List<string>(),
                                        questionnaireName = reqDispatch.QuestionnaireName
                                    });
                                    EventLogList.AddEventByLevel(5, $"{batchreq.PreFill?.Count ?? 0} invitations added to dispatch request for bulk token generation",BatchId,batchreq.DispatchID, reqDispatch.DeliveryPlanId);

                                }
                            }
                            else
                            {
                                if ((batchResponse.StatusByDispatch.Find(x => x.DispatchId == batchreq.DispatchID) == null))
                                {
                                    EventLogList.AddEventByLevel(2, SharedSettings.NoDispatchInSPA, BatchId, batchreq.DispatchID);
                                    batchResponse.StatusByDispatch.Add(new StatusByDispatch()
                                    {
                                        DispatchId = batchreq.DispatchID,
                                        DispatchStatus = "400",
                                        Message = SharedSettings.NoDispatchInSPA
                                    });
                                }
                            }
                        }
                    }
                    else
                    {
                        if ((batchResponse.StatusByDispatch.Find(x => x.DispatchId == batchreq.DispatchID) == null))
                        {
                            EventLogList.AddEventByLevel(2, SharedSettings.InvalidDispatch, BatchId, batchreq.DispatchID);
                            batchResponse.StatusByDispatch.Add(new StatusByDispatch()
                            {
                                DispatchId = batchreq.DispatchID,
                                DispatchStatus = "400",
                                Message = SharedSettings.InvalidDispatch
                            });
                            
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                EventLogList.AddExceptionEvent(ex,BatchId ,null, null, null, "Getting details from cache failed");
            }
        }

        private void GetChannelFromDP(ref BatchResponse batchResponse, List<DeliveryPlan> deliveryPlans)
        {
            try
            {
                foreach (var dispatch in CorrectDispatchData)
                {
                    var deliveryPlan = deliveryPlans.Find(x => x.id == dispatch.Value.DeliveryPlanID);
                    if (deliveryPlan != null && deliveryPlan.isLive)
                    {
                        foreach (var schedule in deliveryPlan.schedule)
                        {
                            if (schedule.onChannel.StartsWith("email://"))
                                dispatch.Value.Channels.Add("Email");
                            else if (schedule.onChannel.StartsWith("sms://"))
                                dispatch.Value.Channels.Add("SMS");
                        }

                        if(dispatch.Value.Channels==null || dispatch.Value.Channels?.Count<1)
                        {
                            //invalid or unsupported channels configured for Dispatch
                            EventLogList.AddEventByLevel(2, SharedSettings.InvalidOrUnsupportedChannels, BatchId, dispatch.Key, deliveryPlan.id);

                            //Removing from CorrectDispatchData with 400 for dispatch
                            RemoveInvalidDispatchData(ref batchResponse, dispatch.Key, SharedSettings.InvalidOrUnsupportedChannels);
                            continue;
                        }

                        dispatch.Value.uniqueCustomerIDByPreFilledQuestionTag = 
                            deliveryPlan.uniqueCustomerIDByPreFilledQuestionTag;
                        if(string.IsNullOrEmpty(deliveryPlan.uniqueCustomerIDByPreFilledQuestionTag))
                        {
                            //invalid DP Configuration for UUID
                            EventLogList.AddEventByLevel(2, SharedSettings.UniQueIdQuestionMissingInDP, BatchId, dispatch.Key, deliveryPlan.id);

                            //Removing from CorrectDispatchData with 400 for dispatch
                            RemoveInvalidDispatchData(ref batchResponse, dispatch.Key, SharedSettings.UniQueIdQuestionMissingInDP);
                            continue;
                        }
                    }
                    else
                    {
                        EventLogList.AddEventByLevel(2, SharedSettings.PausedDP, BatchId, dispatch.Key,deliveryPlan.id);
                        //Removing from CorrectDispatchData with 400 for dispatch
                        RemoveInvalidDispatchData(ref batchResponse, dispatch.Key, SharedSettings.PausedDP);
                    }
                }
            }
            catch (Exception ex)
            {
                EventLogList.AddExceptionEvent(ex, BatchId, null, null, null, "Getting details from cache failed");
            }
        }
        
        private void RemoveInvalidDispatchData(ref BatchResponse batchResponse, string dispatchId, string errormessage)
        {
            CorrectDispatchData.Remove(dispatchId);
            batchResponse.StatusByDispatch.Add(new StatusByDispatch()
            {
                DispatchId = dispatchId,
                DispatchStatus = "400",
                Message = errormessage
            });
        }

        private void AddtoResponsesAndEventLogs(ref List<Response> responses, ref LogEvent logEvents, string questionID, 
            string textInput, bool update=false)
        {
            if (update)
            {
                var resTemp = responses.Find(x => x.QuestionId == questionID);
                var eventLogTemp = logEvents.Prefills.Find(x => x.QuestionId == questionID);

                if ((resTemp != null) && (eventLogTemp != null))
                {
                    responses.Remove(resTemp);
                    logEvents.Prefills.Remove(eventLogTemp);
                }
            }

            Response NewResponse = new Response
            {
                QuestionId = questionID,
                TextInput = textInput
            };
            responses.Add(NewResponse);

            logEvents.Prefills.Add(new Prefill()
            {
                QuestionId = questionID,
                Input = textInput,
                Input_Hash = textInput
            });

        }        

        public async Task CheckDispatchData(List<DispatchRequest> batchRequests, string batchID, BatchResponse batchResponse)
        {
            try
            {
                HashAlgos hashAlgos = new HashAlgos();
                CorrectDispatchData = new Dictionary<string, RequestPrefill>();
                string wxmHashAlgo = string.Empty;
                BatchId = batchID;

                Settings settingsRes = null;
                settingsRes = JsonConvert.DeserializeObject<Settings>(DispatchSettings);
                                           
                //Fetching all Dispatches
                var allDispatches = JsonConvert.DeserializeObject<List<Dispatch>>(DispatchData);              

                CheckDispatchID(ref batchRequests, ref batchResponse, allDispatches);

                //Check value in CorrectDispatchData
                if (CorrectDispatchData.Count == 0)
                {
                    EventLogList.AddEventByLevel(2, SharedSettings.NovalidDispatchInTheBatch, BatchId);
                    return;
                }
                var deliveryPlans = JsonConvert.DeserializeObject<List<DeliveryPlan>>(DeliveryPlanData);
                var activeQuestions = JsonConvert.DeserializeObject<List<Question>>(ActiveQuestions);

                //getting channels from delivery plan
                GetChannelFromDP(ref batchResponse, deliveryPlans);

                DateTime utcNow = DateTime.UtcNow;
                List<LogEvent> batchLogEvents = new List<LogEvent>();

                foreach (var dispatch in CorrectDispatchData)
                {
                    //If one dispatch fails the entire operation shouldn't fail.
                    try
                    {
                        if (!islocationMigrated)
                            wxmHashAlgo = settingsRes?.locationList.Find(x => x.Name == dispatch.Value.questionnaireName)?.HashPIIBy;
                        else
                        {
                            if (!string.IsNullOrEmpty(SurverQuestionnaires))
                            {
                                List<SurveyQuestionnaire> surveyQuestionnaire = JsonConvert.DeserializeObject<List<SurveyQuestionnaire>>(SurverQuestionnaires);
                                wxmHashAlgo = surveyQuestionnaire?.Find(x => x.name == dispatch.Value.questionnaireName)?.hashPIIBy;
                            }
                        }

                        if (string.IsNullOrEmpty(wxmHashAlgo))
                        {
                            EventLogList.AddEventByLevel(5, SharedSettings.NoHashAlogConfigured, batchID);
                        }
                        RequestBulkToken requestBulk = new RequestBulkToken()
                        {
                            DispatchId = dispatch.Key,
                            PrefillReponse = new List<List<Response>>()
                        };

                        int prefillFailCount = 0;
                        List<List<Response>> prefillResponses = new List<List<Response>>();
                        Question batchprefill = new Question();

                        List<string> invalidQuestionIdOrPrefill = new List<string>();
                        foreach (var prefill in dispatch.Value.PreFill)
                        {
                            int recordChannel = 0;
                            List<Response> responses = new List<Response>();
                            LogEvent logEvent = new LogEvent()
                            {
                                Id = ObjectId.GenerateNewId().ToString(),
                                Created = utcNow,
                                DispatchId = dispatch.Key,
                                BatchId = batchID,
                                DeliveryWorkFlowId = dispatch.Value.DeliveryPlanID,
                                Location = dispatch.Value.questionnaireName,
                                Prefills = new List<Prefill>(),
                                Tags = new List<string> {"UserData"}
                            };

                            bool failureFlag = false;
                            bool uuidrecord = false;

                            foreach (var record in prefill)
                            {
                                var question = activeQuestions.Find(x => x.Id == record.questionId && (x.StaffFill || x.ApiFill));
                                if (question == null)
                                {
                                    invalidQuestionIdOrPrefill.Add(record.questionId);
                                    continue;
                                }
                                Response response = new Response
                                {
                                    QuestionId = question.Id
                                };

                                //Channel Check
                                if (question.QuestionTags != null && question.QuestionTags.Contains("Email"))
                                {
                                    // Email question
                                    if (dispatch.Value.Channels.Contains("Email"))
                                    {
                                        recordChannel++;
                                        bool emailStatus = Util.IsValidEmail(record.input);
                                        if (!emailStatus)
                                        {
                                            failureFlag = true;
                                        }
                                    }
                                }
                                else if (question.QuestionTags != null && question.QuestionTags.Contains("Mobile"))
                                {
                                    // SMS question
                                    if (dispatch.Value.Channels.Contains("SMS"))
                                    {
                                        recordChannel++;
                                        bool numberStatus = Util.IsValidMobile(record.input);
                                        if (!numberStatus)
                                        {
                                            failureFlag = true;
                                        }
                                    }
                                }

                                //Common identifier check.
                                if (question.Id == dispatch.Value.uniqueCustomerIDByPreFilledQuestionTag)
                                {
                                    if (!string.IsNullOrWhiteSpace(record.input))
                                        uuidrecord = true;

                                    logEvent.Target = record.input;

                                    if ((question.piiSettings != null) && (question.piiSettings.isPII && (question.piiSettings.piiType == "hash")))
                                    {
                                        var hashedrecord = hashAlgos.GetHashedValue(record.input, wxmHashAlgo);
                                        logEvent.TargetHashed = hashedrecord;
                                    }
                                    else
                                    {
                                        logEvent.TargetHashed = record.input;
                                    }
                                }

                                //Normal prefill check
                                if ((question.piiSettings != null) && (question.piiSettings.isPII && (question.piiSettings.piiType == "hash")))
                                {
                                    logEvent.Prefills.Add(new Prefill()
                                    {
                                        QuestionId = question.Id,
                                        Input = record.input,
                                        Input_Hash = hashAlgos.GetHashedValue(record.input, wxmHashAlgo)
                                    });

                                    response.TextInput = hashAlgos.GetHashedValue(record.input, wxmHashAlgo);
                                }
                                else
                                {
                                    logEvent.Prefills.Add(new Prefill()
                                    {
                                        QuestionId = question.Id,
                                        Input = record.input,
                                        Input_Hash = record.input
                                    });

                                    if (numberTypeRegEx.IsMatch(question.DisplayType))
                                    {
                                        if (int.TryParse(record.input, out int res))
                                        {
                                            response.NumberInput = res;
                                        }
                                    }
                                    else
                                    {
                                        response.TextInput = record.input;
                                    }
                                }
                                responses.Add(response);

                            }

                            var invitationLogEvent = new InvitationLogEvent()
                            {
                                Action = InvitationLogEvent.EventAction.Requested,
                                Channel = InvitationLogEvent.EventChannel.DispatchAPI,
                                TimeStamp = utcNow
                            };

                            //Check for invalid mobile number or email
                            if (failureFlag)
                            {
                                prefillFailCount++;
                                invitationLogEvent.Action = InvitationLogEvent.EventAction.Rejected;
                                invitationLogEvent.LogMessage = new LogMessage() { Message = SharedSettings.FailDueToEmailOrMobile };
                                logEvent.Events = new List<InvitationLogEvent>() { invitationLogEvent };
                                batchLogEvents.Add(logEvent);
                                continue;
                            }

                            //Check for no channel question in record and Check for Common Identifier
                            if (recordChannel == 0 || !uuidrecord)
                            {
                                prefillFailCount++;
                                invitationLogEvent.Action = InvitationLogEvent.EventAction.Rejected;
                                invitationLogEvent.LogMessage = new LogMessage() { Message = SharedSettings.FailDueToUUIDOrChannel };
                                logEvent.Events = new List<InvitationLogEvent>() { invitationLogEvent };
                                batchLogEvents.Add(logEvent);
                                continue;
                            }

                            // Check for Duplication
                            if (uuidrecord)
                            {
                                var dupRecord = batchLogEvents.Find(x => x.DispatchId == dispatch.Key && (x.Target?.ToLower() == logEvent.Target?.ToLower()));
                                if (dupRecord != null)
                                {
                                    prefillFailCount++;
                                    invitationLogEvent.Action = InvitationLogEvent.EventAction.Throttled;
                                    invitationLogEvent.LogMessage = new LogMessage() { Message = $"{ SharedSettings.DuplicateRecord} : {logEvent.Target}"};
                                    logEvent.Events = new List<InvitationLogEvent>() { invitationLogEvent };
                                    batchLogEvents.Add(logEvent);
                                    continue;
                                }

                            }


                            // Unsubscribe data check
                            bool unsubscribestatus = false;
                            if (accountConfiguration.ExtendedProperties.TryGetValue("Unsubscriber", out string unsubcriberName))
                                if (SharedSettings.AvailableUnsubscribeCheckers.TryGetValue(unsubcriberName, out IUnsubscribeChecker unsubscribeChecker))
                                    unsubscribestatus = await unsubscribeChecker.IsUnsubscribedAsync(invitationLogEvent.TargetId?.ToLower());

                            if (unsubscribestatus)
                            {
                                prefillFailCount++;
                                invitationLogEvent.Action = InvitationLogEvent.EventAction.Supressed;
                                logEvent.Events = new List<InvitationLogEvent>() { invitationLogEvent };
                                batchLogEvents.Add(logEvent);
                                continue;
                            }

                            // Add DPID prefill for each record
                            var dpPrefill = activeQuestions.Find(x => x.QuestionTags.Contains("DeliveryPlanId"));
                            if (dpPrefill != null)
                            {
                                AddtoResponsesAndEventLogs(ref responses, ref logEvent, dpPrefill.Id, 
                                    dispatch.Value.DeliveryPlanID);
                            }

                            // Add batchID prefill for each record
                            batchprefill = activeQuestions.Find(x => x.QuestionTags.Contains("BatchId"));
                            if (batchprefill != null)
                            {
                                AddtoResponsesAndEventLogs(ref responses, ref logEvent, batchprefill.Id, batchID);
                            }

                            // Add static prefills from AccountConfigurations set using SPA front-end
                            var dispatchChannel = accountConfiguration.DispatchChannels?.Find(x => x.DispatchId == dispatch.Key);
                            if (dispatchChannel != null)
                            {
                                foreach (var staticPrefill in dispatchChannel.StaticPrefills)
                                {
                                    if (!string.IsNullOrWhiteSpace(staticPrefill.PrefillValue))
                                    {
                                        var temp = responses.Find(x => x.QuestionId == staticPrefill.QuestionId);
                                        if (temp != null)
                                        {
                                            // Remove the old values and override with this one
                                            AddtoResponsesAndEventLogs(ref responses, ref logEvent, staticPrefill.QuestionId,
                                            staticPrefill.PrefillValue, true);
                                        }
                                        else
                                        {
                                            // Add new value
                                            AddtoResponsesAndEventLogs(ref responses, ref logEvent, staticPrefill.QuestionId,
                                                staticPrefill.PrefillValue);
                                        }
                                    }
                                }
                            }


                            logEvent.Events = new List<InvitationLogEvent>() { invitationLogEvent };
                            batchLogEvents.Add(logEvent);

                            //Add records to form Bulk token Request.
                            prefillResponses.Add(responses);
                        }

                        // reject if all records are invalid
                        if (prefillFailCount == dispatch.Value.PreFill.Count)
                        {
                            batchResponse.StatusByDispatch.Add(new StatusByDispatch()
                            {
                                DispatchId = dispatch.Key,
                                DispatchStatus = "400",
                                Message = SharedSettings.AllRecordsRejected
                            });
                            EventLogList.AddEventByLevel(2, SharedSettings.AllRecordsRejected, BatchId, dispatch.Key);
                        }
                        else
                        {

                            if (invalidQuestionIdOrPrefill?.Count>0) 
                            {
                                //few records from the invites being removed with some question id
                                EventLogList.AddEventByLevel(4, $"few or all Prefills with following question ids removed: {string.Join(',', invalidQuestionIdOrPrefill)}",BatchId,dispatch.Key);

                            }
                            requestBulk.PrefillReponse = prefillResponses;
                            requestBulk.UUID = dispatch.Value.uniqueCustomerIDByPreFilledQuestionTag;
                            requestBulk.Batchid = batchprefill?.Id;

                            // Add records in batching queue
                            if (accountConfiguration.ExtendedProperties.TryGetValue("BatchingQueue", out string queueName))
                            {
                                if (SharedSettings.AvailableQueues.TryGetValue(queueName, out IBatchingQueue<RequestBulkToken> batchingQueue))
                                {
                                    batchingQueue.Insert(requestBulk);
                                    EventLogList.AddEventByLevel(5, $"{prefillResponses?.Count ?? 0} records added to queue for bulk import to Queue {queueName}", BatchId, dispatch.Key);
                                }
                            }
                            else
                            {
                                EventLogList.AddEventByLevel(1, $"Batching queue missing", BatchId, dispatch.Key);
                            }
                            if (prefillFailCount == 0)
                            {
                                batchResponse.StatusByDispatch.Add(new StatusByDispatch()
                                {
                                    DispatchId = dispatch.Key,
                                    DispatchStatus = "202",
                                    Message = SharedSettings.AcceptedForProcessing //When all records for DispatchID were successful.
                                });
                                EventLogList.AddEventByLevel(5, $"{SharedSettings.AcceptedForProcessing} {dispatch.Value.PreFill.Count}", BatchId, dispatch.Key);
                            }
                            else
                            {
                                string message = SharedSettings.AcceptedForProcessing + " " +
                                    (dispatch.Value.PreFill.Count - prefillFailCount).ToString() + " " +
                                    "Rejected: " + prefillFailCount.ToString();

                                batchResponse.StatusByDispatch.Add(new StatusByDispatch()
                                {
                                    DispatchId = dispatch.Key,
                                    DispatchStatus = "206",
                                    Message =  message //Partial records were successfull.
                                }) ;
                                EventLogList.AddEventByLevel(5, message, BatchId, dispatch.Key);
                            }
                        }

                    }
                    catch (Exception ex0)
                    {
                        EventLogList.AddExceptionEvent(ex0,batchID,dispatch.Key, 
                            dispatch.Value?.DeliveryPlanID,dispatch.Value?.questionnaireName);
                    }
                }

                //add for the entire batch
                await mongoDBConn.AddBulkEvents(batchLogEvents);
            }
            catch (Exception ex)
            {
                EventLogList.AddExceptionEvent(ex, batchID);
            }
        }
    }
}
