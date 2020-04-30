using Microsoft.Extensions.Configuration;
using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace XM.ID.Invitations.Net
{
    public class ViaMongoDB
    {
        readonly IMongoCollection<AccountConfiguration> _AccountConfiguration;
        readonly IMongoCollection<Unsubscribed> _Unsubscribe;
        readonly IMongoCollection<LogEvent> _EventLog;
        readonly IMongoCollection<DB_MessagePayload> _BulkMessage;
        readonly int _maximumLevel; 

        public ViaMongoDB(IConfiguration configuration)
        {
            MongoClientSettings settings = MongoClientSettings.FromUrl(new MongoUrl(configuration["MONGODB_URL"]));
            settings.MaxConnectionIdleTime = TimeSpan.FromMinutes(3);
            settings.ConnectTimeout = TimeSpan.FromSeconds(20);
            settings.MaxConnectionPoolSize = 1000;
            settings.ReadPreference = ReadPreference.Nearest;
            var mongoClient = new MongoClient(settings);
            IMongoDatabase asyncdb = mongoClient.GetDatabase(configuration["DbNAME"]);
            int.TryParse(configuration["LoggingMaximumLevel"], out _maximumLevel);

            _AccountConfiguration = asyncdb.GetCollection<AccountConfiguration>("AccountConfiguration");
            _Unsubscribe = asyncdb.GetCollection<Unsubscribed>("Unsubscribe");
            _EventLog = asyncdb.GetCollection<LogEvent>("EventLog");
            _BulkMessage = asyncdb.GetCollection<DB_MessagePayload>("BulkMessage");


            #region Create Index
#pragma warning disable CS0618

            // For Dispatcher module to lookup and push the log events
            _EventLog.Indexes.CreateOneAsync(Builders<LogEvent>.IndexKeys.Ascending(x => x.TokenId)
.Ascending(x => x.BatchId).Ascending(x => x.DispatchId), new CreateIndexOptions { Background = false });

            // For updating the logevent once token is created
            _EventLog.Indexes.CreateOneAsync(Builders<LogEvent>.IndexKeys.Ascending(x => x.BatchId)
.Ascending(x => x.TargetHashed).Ascending(x => x.DispatchId), new CreateIndexOptions { Background = false });

            _EventLog.Indexes.CreateOneAsync(Builders<LogEvent>.IndexKeys.Ascending(x => x.Target),
                new CreateIndexOptions { Background = false });

            // For auto expiring the records after 3 months i.e. 7890000  seconds
            // Applied on Created At field
            _EventLog.Indexes.CreateOneAsync(Builders<LogEvent>.IndexKeys.Ascending(x => x.Created),
                new CreateIndexOptions {
                    Background = false,
                    ExpireAfter = TimeSpan.FromSeconds(int.Parse(configuration["MONGODB_RECORDS_EXPIRY"]))
                });

            //For Time-Trigger Serverless Computes
            _BulkMessage.Indexes.CreateOneAsync(Builders<DB_MessagePayload>.IndexKeys.Ascending(x => x.BulkVendorName)
                .Ascending(x => x.Status).Ascending(x => x.InsertTime), new CreateIndexOptions { Background = false });
            
#pragma warning restore CS0618
            #endregion
        }

        public async Task<List<LogEvent>> GetActivityDocuments(ActivityFilter filterObject)
        {
            var filter = Builders<LogEvent>.Filter.Empty;

            if (!string.IsNullOrWhiteSpace(filterObject.Token))
                filter &= Builders<LogEvent>.Filter.Eq(x => x.TokenId, filterObject.Token);
            if (!string.IsNullOrWhiteSpace(filterObject.BatchId))
                filter &= Builders<LogEvent>.Filter.Eq(x => x.BatchId, filterObject.BatchId);
            if (!string.IsNullOrWhiteSpace(filterObject.DispatchId))
                filter &= Builders<LogEvent>.Filter.Eq(x => x.DispatchId, filterObject.DispatchId);
            if (!string.IsNullOrWhiteSpace(filterObject.Target))
                filter &= Builders<LogEvent>.Filter.Eq(x => x.Target, filterObject.Target);
            if (!string.IsNullOrWhiteSpace(filterObject.Created) && DateTime.TryParse(filterObject.Created, 
                out DateTime createdon))
                filter &= Builders<LogEvent>.Filter.Gte(x => x.Created, createdon.Date) 
                    & Builders<LogEvent>.Filter.Lt(x => x.Created, createdon.Date.AddDays(1));

            return await _EventLog.Find(filter).ToListAsync();
        }

        public async Task<AccountConfiguration> GetAccountConfiguration()
        {
            return await _AccountConfiguration.Find(_ => true).FirstOrDefaultAsync();
        }

        public async Task<AccountConfiguration> AddOrUpdateAccountConfiguration_WXMFields(string adminUser, string apiKey, string user, string baseUrl)
        {
            Dictionary<string, string> defaultExtendedProperties = new Dictionary<string, string>
            {
                { "BatchingQueue", "inmemory" },
                { "Sampler", "wxm" },
                { "Unsubscriber", "wxm" }
            };
            var filter = Builders<AccountConfiguration>.Filter.Empty;
            var update = Builders<AccountConfiguration>.Update
                .SetOnInsert(x => x.DispatchChannels, null)
                .SetOnInsert(x => x.Id, ObjectId.GenerateNewId().ToString())
                .SetOnInsert(x => x.Vendors, null)
                .SetOnInsert(x => x.ExtendedProperties, defaultExtendedProperties)
                .Set(x => x.Queue, null)
                .Set(x => x.WXMAdminUser, adminUser)
                .Set(x => x.WXMAPIKey, apiKey)
                .Set(x => x.WXMBaseURL, baseUrl)
                .Set(x => x.WXMUser, user);
            var opts = new FindOneAndUpdateOptions<AccountConfiguration> { IsUpsert = true, ReturnDocument = ReturnDocument.After };
            return await _AccountConfiguration.FindOneAndUpdateAsync<AccountConfiguration>(filter, update, opts);
        }

        public async Task<AccountConfiguration> UpdateAccountConfiguration_DispatchChannels(List<DispatchChannel> dispatchChannels)
        {
            var filter = Builders<AccountConfiguration>.Filter.Empty;
            var update = Builders<AccountConfiguration>.Update.Set(x => x.DispatchChannels, dispatchChannels);
            var opts = new FindOneAndUpdateOptions<AccountConfiguration> { IsUpsert = true, ReturnDocument = ReturnDocument.After };
            return await _AccountConfiguration.FindOneAndUpdateAsync<AccountConfiguration>(filter, update, opts);
        }

        public async Task<AccountConfiguration> UpdateAccountConfiguration_Vendors(List<Vendor> vendors)
        {
            var filter = Builders<AccountConfiguration>.Filter.Empty;
            var update = Builders<AccountConfiguration>.Update.Set(x => x.Vendors, vendors);
            var opts = new FindOneAndUpdateOptions<AccountConfiguration> { IsUpsert = true, ReturnDocument = ReturnDocument.After };
            return await _AccountConfiguration.FindOneAndUpdateAsync<AccountConfiguration>(filter, update, opts);
        }

        public async Task<AccountConfiguration> UpdateAccountConfiguration_Queue(Queue queue)
        {
            var filter = Builders<AccountConfiguration>.Filter.Empty;
            var update = Builders<AccountConfiguration>.Update.Set(x => x.Queue, queue);
            var opts = new FindOneAndUpdateOptions<AccountConfiguration> { IsUpsert = true, ReturnDocument = ReturnDocument.After };
            return await _AccountConfiguration.FindOneAndUpdateAsync<AccountConfiguration>(filter, update, opts);
        }

        public async Task<AccountConfiguration> UpdateAccountConfiguration_ExtendedProperties(Dictionary<string, string> extendedProperties)
        {
            var filter = Builders<AccountConfiguration>.Filter.Empty;
            var update = Builders<AccountConfiguration>.Update.Set(x => x.ExtendedProperties, extendedProperties);
            var opts = new FindOneAndUpdateOptions<AccountConfiguration> { IsUpsert = true, ReturnDocument = ReturnDocument.After };
            return await _AccountConfiguration.FindOneAndUpdateAsync<AccountConfiguration>(filter, update, opts);
        }

        public async Task DeleteAccountConfiguration()
        {
            var filter = Builders<AccountConfiguration>.Filter.Empty;
            await _AccountConfiguration.FindOneAndDeleteAsync<AccountConfiguration>(filter);
            return;
        }

        public async Task AddUnsubscribeRecord(string email)
        {
            try
            {
                long recordcount = await _Unsubscribe.CountDocumentsAsync(x => x.Email == email);
                if (recordcount == 0)
                {
                    Unsubscribed newRecord = new Unsubscribed()
                    {
                        Id = ObjectId.GenerateNewId().ToString(),
                        Email = email?.ToLower(),
                        UnsubscribedAt = DateTime.UtcNow
                    };
                    await _Unsubscribe.InsertOneAsync(newRecord);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task RemoveUnsubscribeRecord(string id)
        {
            await _Unsubscribe.DeleteOneAsync(x => x.Email.Equals(id));
        }

        public async Task<bool> CheckUnsubscribe(string email)
        {
            try
            {
                long count = await _Unsubscribe.CountDocumentsAsync(x => x.Email.Equals(email));
                if (count > 0)
                    return true;
                else
                    return false;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        #region LogEvent
        public async Task AddLogEvent(LogEvent logevents)
        {

            if (logevents == null)
                return;
            logevents.Id= ObjectId.GenerateNewId().ToString();
            logevents.Created = DateTime.UtcNow;

            await _EventLog.InsertOneAsync(logevents);
        }

        public async Task AddLogEvents(List<LogEvent> logevents)
        {
            if (logevents == null || logevents.Count == 0)
                return;
            foreach(var logevent in logevents.ToList())
            {
                if (CheckLogLevel(logevent.LogMessage.Level) > _maximumLevel)
                    logevents.Remove(logevent);
            }
            if (logevents.Count == 0)
                return;
            await _EventLog.InsertManyAsync(logevents);
        }

        public int CheckLogLevel(string message)
        {
            if (LogMessage.SeverityLevel_Critical == message)
                return 1;
            else if (LogMessage.SeverityLevel_Critical == message)
                return 2;
            else if (LogMessage.SeverityLevel_Critical == message)
                return 3;
            else if (LogMessage.SeverityLevel_Critical == message)
                return 4;
            else
                return 5;
        }

        public async Task UpdateBulkEventLog(Dictionary<LogEvent, InvitationLogEvent> logevents)
        {
            var builder = Builders<LogEvent>.Filter;
            var bulkEventLogList = new List<WriteModel<LogEvent>>();
            try
            {
                foreach (var logEvent in logevents)
                {
                    var invitationEvent = logEvent.Value;
                    //invitationEvent.TimeStamp = DateTime.UtcNow;

                    var updateUserData = Builders<LogEvent>.Update.Push(x => x.Events, invitationEvent)
                        .Set(x => x.TokenId, logEvent.Key.TokenId.ToUpper())
                        .Set(x => x.Updated, logEvent.Key.Updated);

                    bulkEventLogList.Add(new UpdateManyModel<LogEvent>(builder.Eq(x => x.BatchId, logEvent.Key.BatchId) 
                        & builder.Eq(x => x.TargetHashed, logEvent.Key.TargetHashed) 
                        & builder.Eq(x => x.DispatchId, logEvent.Key.DispatchId)
                        & builder.ElemMatch(x => x.Events, x => x.Action == 0), updateUserData));
                }

                var result = await _EventLog.BulkWriteAsync(bulkEventLogList, new BulkWriteOptions() { IsOrdered = false });
                if (result.IsAcknowledged)
                {
                    //add debug log for success
                }
                else
                {
                    //add debug log for failure 
                }

            }
            catch (Exception ex0)
            {
                await AddExceptionEvent(ex0);
            }

        }

        public async Task AddOrUpdateEvent(LogEvent logevents, InvitationLogEvent logevent = null, string tokenId = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(logevents.Id) )
                    logevents.Id = ObjectId.GenerateNewId().ToString();

                logevents.TokenId = tokenId;
                var utcNow = DateTime.UtcNow;
                var filter = Builders<LogEvent>.Filter.Eq(x => x.Id, logevents.Id);
                var update = Builders<LogEvent>.Update;
                List<UpdateDefinition<LogEvent>> updates = new List<UpdateDefinition<LogEvent>>();

                updates.Add(update.SetOnInsert(x => x.Created, utcNow));
                updates.Add(update.SetOnInsert(x => x.Location, logevents.Location));
                updates.Add(update.SetOnInsert(x => x.DeliveryWorkFlowId, logevents.DeliveryWorkFlowId));
                updates.Add(update.SetOnInsert(x => x.Target, logevents.Target));
                updates.Add(update.SetOnInsert(x => x.TargetHashed, logevents.TargetHashed));
                updates.Add(update.SetOnInsert(x => x.LogMessage, logevents.LogMessage));
                updates.Add(update.SetOnInsert(x => x.Events, logevents.Events));

                if (logevent!=null)
                {
                    logevent.TimeStamp = utcNow;
                    updates.Add(update.Push(x => x.Events, logevent));
                }

                updates.Add(update.Set(x => x.Updated, utcNow));

                var opt = new FindOneAndUpdateOptions<LogEvent> { IsUpsert = true, ReturnDocument = ReturnDocument.After };
                var up = update.Combine(updates);
                await _EventLog.FindOneAndUpdateAsync(filter, up, opt);

            }
            catch (Exception ex)
            {
                await AddExceptionEvent(ex);
            }
        }
        public async Task  AddBulkEvents(List<LogEvent> documents)
        {

            if (documents == null)
                return;
            try
            {
                await _EventLog.InsertManyAsync(documents, new InsertManyOptions() { IsOrdered = false });
            }
            catch (Exception ex0)
            {
                await AddExceptionEvent(ex0);
            }
           
        }
        public async Task AddExceptionEvent(Exception ex0, string batchId = null, string dispatchId = null, string deliveryPlanId = null, string questionnaire= null,string message = null)
        {
            List<string> tags = new List<string>();
            if (string.IsNullOrWhiteSpace(dispatchId))
                tags.Add("Account");
             
            var logEvent = new LogEvent()
            {
                BatchId = batchId,
                DispatchId = dispatchId,
                DeliveryWorkFlowId = deliveryPlanId,
                Location = questionnaire,
                LogMessage = new LogMessage() { Exception = JsonConvert.SerializeObject(ex0) ,Level = LogMessage.SeverityLevel_Critical, Message = message  },
                Tags = tags
                
            };
            await AddLogEvent(logEvent);
        }
        public async Task AddEventByLevel(int level, string message, string batchId, string dispatchId = null, string deliveryPlanId = null, string questionnaire = null)
        {
            if (level > _maximumLevel)
                return;
            string CriticalityLevel;
            if (level == 1)
                CriticalityLevel = LogMessage.SeverityLevel_Critical;
            else if (level == 2)
                CriticalityLevel = LogMessage.SeverityLevel_Error;
            else if (level == 3)
                CriticalityLevel = LogMessage.SeverityLevel_Information;
            else if (level == 4)
                CriticalityLevel = LogMessage.SeverityLevel_Warning;
            else
                CriticalityLevel = LogMessage.SeverityLevel_Debug;
            
            var logEvent = new LogEvent()
            {
                BatchId = batchId,
                DispatchId = dispatchId,
                DeliveryWorkFlowId = deliveryPlanId,
                Location = questionnaire,
                LogMessage =  new LogMessage() { Message =message,  Level = CriticalityLevel }
            };
            await AddLogEvent(logEvent);

        }
       

        #endregion
    }
}
