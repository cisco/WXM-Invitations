using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using XM.ID.Invitations.Net;
using XM.ID.Net;

namespace DPReporting
{
    class DataUpload
    {
        ApplicationLog log;
        IConfigurationRoot Configuration;
        SMTPServer smtpServer;
        readonly HTTPWrapper hTTPWrapper;

        public DataUpload(IConfigurationRoot configuration, ApplicationLog applog)
        {
            Configuration = configuration;
            log = applog;
            hTTPWrapper = new HTTPWrapper();
        }

        public async Task DataUploadTask()
        {
            log.logMessage += $"Started on : {DateTime.UtcNow.ToString()} ";

            double.TryParse(Configuration["DataUploadSettings:UploadDataForLastHours"], out double LastHours);

            if (LastHours == 0)
            {
                log.logMessage += " LastHours needs to be a number";
                log.AddLogsToFile(DateTime.UtcNow);
                return;
            }

            await RunDataUploadTask(LastHours);
        }

        public async Task RunDataUploadTask(double LastHours)
        {
            try
            {
                ViaMongoDB via = new ViaMongoDB(Configuration);

                FilterBy filter = new FilterBy() { afterdate = DateTime.UtcNow.AddHours(-LastHours), beforedate = DateTime.UtcNow };

                AccountConfiguration a = await via.GetAccountConfiguration();

                string bearer = null;

                string responseBody = await hTTPWrapper.GetLoginToken(a.WXMAdminUser, a.WXMAPIKey);
                if (!string.IsNullOrEmpty(responseBody))
                {
                    BearerToken loginToken = Newtonsoft.Json.JsonConvert.DeserializeObject<BearerToken>(responseBody);
                    bearer = loginToken.AccessToken;
                }

                List<Question> questions = null;

                string q = InvitationsMemoryCache.GetInstance().GetActiveQuestionsFromMemoryCache("Bearer "+ bearer, hTTPWrapper);
                if (!string.IsNullOrEmpty(q))
                    questions = JsonConvert.DeserializeObject<List<Question>>(q);

                List<WXMPartnerMerged> data = await via.GetMergedData(filter, bearer, questions);

                if (data != null)
                    await via.Upload(data);
            }
            catch(Exception ex)
            {
                log.logMessage += $"Error uploading the data {ex.Message}    {ex.StackTrace}";
                return;
            }
        }
    }
}
