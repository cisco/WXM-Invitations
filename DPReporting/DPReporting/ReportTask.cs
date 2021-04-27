using Microsoft.Extensions.Configuration;
using MongoDB.Bson.Serialization.Serializers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using XM.ID.Invitations.Net;
using XM.ID.Net;

namespace DPReporting
{
    class ReportTask
    {
        ApplicationLog log;
        IConfigurationRoot Configuration;
        SMTPServer smtpServer;
        readonly HTTPWrapper hTTPWrapper;
        ScheduleReportSettings schedule;
        DataUploadSettings dataupload;

        public ReportTask(IConfigurationRoot configuration, ApplicationLog applog)
        {
            Configuration = configuration;
            log = applog;
            hTTPWrapper = new HTTPWrapper();
        }

        public async Task ReportSendingTask()
        {
            log.logMessage += $"Started on : {DateTime.UtcNow.ToString()} ";

            #region setup

            ConfigureSettings();

            int reportFor = schedule.ReportForLastDays;

            double hourlyDelay = schedule.Frequency;

            double RunUploadEvery = dataupload.RunUploadEveryMins;

            bool IsScheduleReport = schedule.ScheduleReport;

            if (IsScheduleReport && RunUploadEvery > hourlyDelay * 60)
            {
                log.logMessage += " Data upload frequency can't be greater than report schedule frequency";
                log.AddLogsToFile(DateTime.UtcNow);
                return;
            }

            if (IsScheduleReport && (reportFor == 0 || hourlyDelay == 0))
            {
                log.logMessage += " ReportForLastDays and Frequency needs to be a number";
                log.AddLogsToFile(DateTime.UtcNow);
                return;
            }

            var sendOutReport = SetUpReportSender();            

            DateTime StartDate = new DateTime();

            try
            {
                StartDate = DateTime.ParseExact(schedule.StartDate, "yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture);

                bool autopick = schedule.AutoPickLastStartDate;

                //modify startdate for report in case app crashes and start date is eligible to be changed through the property AutoPickLastStartDate
                if (File.Exists(Configuration["LogFilePath"] + "/startdate.json") && autopick)
                {
                    using (StreamReader r = new StreamReader(Configuration["LogFilePath"] + "/startdate.json"))
                    {
                        string json = r.ReadToEnd();
                        List<Dictionary<string, string>> items = JsonConvert.DeserializeObject<List<Dictionary<string, string>>>(json);
                        StartDate = DateTime.ParseExact(items[0]["StartDate"], "yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture);
                    }
                }
            }
            catch (Exception ex)
            {
                log.logMessage += "StartDate needs to be in this format- yyyy-MM-ddTHH:mm:ss. needs to be a valid startdate";
                log.logMessage += $"Error in report task {ex.Message}    {ex.StackTrace}";
                log.AddLogsToFile(DateTime.UtcNow);
                return;
            }

            #endregion

            try
            {
                await RunReportTask(StartDate, reportFor, hourlyDelay, sendOutReport, RunUploadEvery);
            }
            catch(Exception ex)
            {
                log.logMessage += $"Error in report task {ex.Message}    {ex.StackTrace}";
                return;
            }
        }

        public void ConfigureSettings()
        {
            try
            {
                bool.TryParse(Configuration["ScheduleReport:IsScheduleReport"], out bool IsSchedule);
                int.TryParse(Configuration["ScheduleReport:ReportForLastDays"], out int reportFor);
                double.TryParse(Configuration["ScheduleReport:Frequency"], out double hourlyDelay);
                bool.TryParse(Configuration["ScheduleReport:AutoPickLastStartDate"], out bool autopick);

                schedule = new ScheduleReportSettings
                {
                    ScheduleReport = IsSchedule,
                    Frequency = hourlyDelay,
                    StartDate = Configuration["ScheduleReport:StartDate"],
                    ReportForLastDays = reportFor,
                    AutoPickLastStartDate = autopick
                };

                double.TryParse(Configuration["DataUploadSettings:RunUploadEveryMins"], out double uploadEvery);
                double.TryParse(Configuration["DataUploadSettings:UploadDataForLastHours"], out double uploadFor);
                double.TryParse(Configuration["DataUploadSettings:CheckResponsesCapturedForLastHours"], out double ResponsesCheck);

                dataupload = new DataUploadSettings 
                { 
                    RunUploadEveryMins = uploadEvery,
                    UploadDataForLastHours = uploadFor,
                    CheckResponsesCapturedForLastHours = ResponsesCheck
                };
            }
            catch (Exception ex)
            {
                schedule = null;
                dataupload = null;
            }

            if (schedule == null || dataupload == null)
            {
                log.logMessage += $" Invalid report schedule or data upload settings";
                log.AddLogsToFile(DateTime.UtcNow);
            }
        }

        public SendOutReport SetUpReportSender()
        {
            bool.TryParse(Configuration["CustomeMailServer:EnableSSL"], out bool enablessl);
            int.TryParse(Configuration["CustomeMailServer:Port"], out int port);

            try
            {
                smtpServer = new SMTPServer()
                {
                    EnableSSL = enablessl,
                    FromAddress = Configuration["CustomeMailServer:FromAddress"],
                    FromName = Configuration["CustomeMailServer:FromName"],
                    Login = Configuration["CustomeMailServer:Login"],
                    Password = Configuration["CustomeMailServer:Password"],
                    Port = port,
                    Server = Configuration["CustomeMailServer:Server"],
                };
            }
            catch (Exception ex)
            {
                smtpServer = null;
            }

            if (smtpServer == null)
            {
                //add log and return
                log.logMessage += $" Invalid smtp details configured!";
                log.AddLogsToFile(DateTime.UtcNow);
                return null;
            }

            return new SendOutReport(smtpServer, log);
        }

        public async Task RunReportTask(DateTime StartDate, int reportFor, double hourlyDelay, SendOutReport sendOutReport, double RunUploadEvery)
        {
            try
            {
                ViaMongoDB via = new ViaMongoDB(Configuration);
                bool IsScheduleReport = schedule.ScheduleReport;

                AccountConfiguration a = await via.GetAccountConfiguration();

                bool IsValidEmail(string email)
                {
                    try
                    {
                        var mail = new System.Net.Mail.MailAddress(email);
                        return true;
                    }
                        return false;
                    }
                }

                DataUpload d = new DataUpload(Configuration, log);

                DateTime NextLockCheck = DateTime.UtcNow;
                var OnDemand = await via.GetOnDemandModel();
                NextLockCheck = NextLockCheck.AddMinutes(2);
                
                DateTime NextUpload = DateTime.UtcNow;
                NextUpload = NextUpload.AddMinutes(RunUploadEvery);

                while (true)
                {
                    try
                    {
                        if (IsScheduleReport && StartDate < DateTime.UtcNow)
                        {
                            string bearer = null;

                            string responseBody = await hTTPWrapper.GetLoginToken(a.WXMAdminUser, a.WXMAPIKey);
                            if (!string.IsNullOrEmpty(responseBody))
                            {
                                BearerToken loginToken = Newtonsoft.Json.JsonConvert.DeserializeObject<BearerToken>(responseBody);
                                bearer = "Bearer " + loginToken.AccessToken;
                            }

                            ReportCreator report = new ReportCreator(Configuration, log, bearer);

                            //flow for start date reached or passed
                            FilterBy filter = new FilterBy() { afterdate = DateTime.Today.AddDays(-reportFor), beforedate = DateTime.Now };

                            string Emails = null;
                            if (!a.ExtendedProperties?.Keys?.Contains("ReportRecipients") == true)
                            {
                                log.logMessage += $"No To emails present";
                                log.AddLogsToFile(DateTime.UtcNow);
                                continue;
                            }
                            else if (a.ExtendedProperties?.Keys?.Contains("ReportRecipients") == true && string.IsNullOrEmpty(a.ExtendedProperties["ReportRecipients"]))
                            {
                                log.logMessage += $"No To emails present";
                                log.AddLogsToFile(DateTime.UtcNow);
                                continue;
                            }
                            else
                            {
                                Emails = a.ExtendedProperties["ReportRecipients"];
                            }

                            List<string> toEmails = new List<string>();

                            foreach (string email in Emails.Split(";"))
                            {
                                string e = Regex.Replace(email, @"\s+", "");
                                if (IsValidEmail(e))
                                    toEmails.Add(e);
                            }

                            Tuple<byte[], bool> reportJob = await report.GetOperationMetricsReport(filter);

                            if (reportJob == null)
                            {
                                log.logMessage += "Error in generating the report or no data present to generate report";
                                log.AddLogsToFile(DateTime.UtcNow);
                            }

                            byte[] reportBytes = reportJob?.Item1;

                            StartDate = DateTime.UtcNow.AddHours(hourlyDelay);

                            if (reportBytes?.Count() > 0)
                            {
                                var toAddress = new MailAddress(toEmails.First());

                                MailMessage mailMessage = new MailMessage(new MailAddress(smtpServer.FromAddress), toAddress);

                                if (toEmails.Count() > 1)
                                {
                                    foreach (var toemail in toEmails.Skip(1))
                                        mailMessage.CC.Add(toemail);
                                }

                                string filename = null;

                                if (reportBytes?.Count() > 0)
                                {
                                    ReportFileManagement store = new ReportFileManagement(Configuration["ReportPath"], log);
                                    filename = store.SaveReportFile(reportBytes, OnDemand.Filter.afterdate.AddMinutes(OnDemand.TimeOffSet), OnDemand.Filter.beforedate.AddMinutes(OnDemand.TimeOffSet));
                                }

                                if (filename == null || Configuration["PathToEmail"] == null)
                                {
                                    log.logMessage += $"unable to store report file or PathToEmail not set";
                                    log.AddLogsToFile(DateTime.UtcNow);

                                    await via.UnLockOnDemand();

                                    OnDemand = await via.GetOnDemandModel();

                                    continue;
                                }

                                #region mail content

                                string emailBody = null;

                                if (reportJob.Item2)
                                {
                                    emailBody = "Hello, \n\nHere's the survey dispatch report for the time period- ";
                                    emailBody += DateTime.Today.AddDays(-reportFor).ToString("yyyy-MM-dd") + " - " + DateTime.Now.ToString("yyyy-MM-dd") + ".\n\n";
                                    emailBody += Configuration["PathToEmail"] + filename + "\n\n";
                                    emailBody += "Thanks,\n";
                                    emailBody += "Cisco Webex Team\n\n";
                                    emailBody += "We are here to help. Contact us anytime at webexxm-support@cisco.com";
                                }
                                else
                                {
                                    emailBody = "Hello, \n\nHere's the survey dispatch report for the time period- ";
                                    emailBody += DateTime.Today.AddDays(-reportFor).ToString("yyyy-MM-dd") + " - " + DateTime.Now.ToString("yyyy-MM-dd") + ".\n\n";
                                    emailBody += Configuration["PathToEmail"] + filename + "\n\n";
                                    emailBody += "Please note, the attached file does not contain data splits and pivot table because no invitations were sent during the selected date range for this report.. To view data splits and pivot tables, generate a report for a different date range during which invites were sent." + ".\n\n";
                                    emailBody += "Thanks,\n";
                                    emailBody += "Cisco Webex Team\n\n";
                                    emailBody += "We are here to help. Contact us anytime at webexxm-support@cisco.com";
                                }

                                string emailSubject = "[Cisco WXM] Survey dispatch performance report";

                                #endregion

                                mailMessage.Subject = emailSubject;
                                mailMessage.Body = emailBody;

                                await sendOutReport.SendOutEmails(mailMessage);

                                log.logMessage += "Successfully completed report dispatch on- " + DateTime.UtcNow.ToString() + "\n";
                                log.logMessage += "Next dispatch on- " + StartDate.ToString();
                                log.AddLogsToFile(DateTime.UtcNow);
                            }
                            else
                            {
                                log.logMessage += "Could not dispatch since no data was present";
                                log.AddLogsToFile(DateTime.UtcNow);
                            }

                            //save startdate in a file if app restarts
                            List<Dictionary<string, string>> _data = new List<Dictionary<string, string>>()
                    {
                        new Dictionary<string, string>() {
                            { "StartDate", StartDate.ToString("yyyy-MM-ddTHH:mm:ss")}
                        }
                    };

                            string json = JsonConvert.SerializeObject(_data.ToArray());

                            //write string to file
                            System.IO.File.WriteAllText(Configuration["LogFilePath"] + "/startdate.json", json);
                        }
                    }
                    catch (Exception ex)
                    {
                        bool unlock = await via.UnLockOnDemand();

                        log.logMessage += $"Error in scheduled report task {ex.Message}    {ex.StackTrace}";
                        continue;
                    }
                    try
                    {
                        if (NextLockCheck < DateTime.UtcNow)
                        {
                            OnDemand = await via.GetOnDemandModel();
                            NextLockCheck = NextLockCheck.AddMinutes(2);
                        }
                        
                        if (OnDemand != null && OnDemand?.IsLocked == true)
                        {
                            a = await via.GetAccountConfiguration();

                            string Emails = null;
                            if (!a.ExtendedProperties?.Keys?.Contains("ReportRecipients") == true)
                            {
                                await via.UnLockOnDemand();

                                log.logMessage += $"No To emails present";
                                log.AddLogsToFile(DateTime.UtcNow);
                                continue;
                            }
                            else if ((a.ExtendedProperties?.Keys?.Contains("ReportRecipients") == true) && string.IsNullOrEmpty(a.ExtendedProperties["ReportRecipients"]))
                            {
                                await via.UnLockOnDemand();

                                log.logMessage += $"No To emails present";
                                log.AddLogsToFile(DateTime.UtcNow);
                                continue;
                            }
                            else
                            {
                                Emails = a.ExtendedProperties["ReportRecipients"];
                            }

                            List<string> toEmails = new List<string>();

                            foreach (string email in Emails.Split(";"))
                            {
                                string e = Regex.Replace(email, @"\s+", "");
                                if (IsValidEmail(e))
                                    toEmails.Add(e);
                            }

                            string bearer = null;

                            string responseBody = await hTTPWrapper.GetLoginToken(a.WXMAdminUser, a.WXMAPIKey);
                            if (!string.IsNullOrEmpty(responseBody))
                            {
                                BearerToken loginToken = Newtonsoft.Json.JsonConvert.DeserializeObject<BearerToken>(responseBody);
                                bearer = "Bearer " + loginToken.AccessToken;
                            }

                            ReportCreator report = new ReportCreator(Configuration, log, bearer);

                            Tuple<byte[], bool> reportJob = await report.GetOperationMetricsReport(OnDemand.Filter);

                            if (reportJob == null)
                            {
                                log.logMessage += "Error in generating the report or no data present to generate report";
                                log.AddLogsToFile(DateTime.UtcNow);
                            }

                            byte[] reportBytes = reportJob?.Item1;

                            if (reportBytes == null)
                            {
                                log.logMessage += "Error in generating the report;";
                                log.AddLogsToFile(DateTime.UtcNow);
                            }

                            if (reportBytes?.Count() > 0)
                            {
                                //var toAddress = new MailAddress(toEmails.First());

                                //MailMessage mailMessage = new MailMessage(new MailAddress(smtpServer.FromAddress), toAddress);

                                MailMessage mailMessage = new MailMessage();
                                mailMessage.From = new MailAddress(smtpServer.FromAddress, smtpServer.FromName);

                                mailMessage.To.Add(toEmails.First());

                                if (toEmails.Count() > 1)
                                {
                                    foreach (var toemail in toEmails.Skip(1))
                                        mailMessage.CC.Add(toemail);
                                }

                                string filename = null;

                                if (reportBytes?.Count() > 0)
                                {
                                    ReportFileManagement store = new ReportFileManagement(Configuration["ReportPath"], log);
                                    filename = store.SaveReportFile(reportBytes, OnDemand.Filter.afterdate.AddMinutes(OnDemand.TimeOffSet), OnDemand.Filter.beforedate.AddMinutes(OnDemand.TimeOffSet));
                                }

                                if (filename == null || Configuration["PathToEmail"] == null)
                                {
                                    log.logMessage += $"unable to store report file or PathToEmail not set";
                                    log.AddLogsToFile(DateTime.UtcNow);

                                    await via.UnLockOnDemand();

                                    OnDemand = await via.GetOnDemandModel();

                                    continue;
                                }

                                #region email content

                                string emailBody = null;

                                if (reportJob.Item2)
                                {
                                    emailBody = "Hello, \n\nHere's the survey dispatch report for the time period- ";
                                    emailBody += OnDemand.Filter.afterdate.AddMinutes(OnDemand.TimeOffSet).ToString("yyyy-MM-dd") + " - " + OnDemand.Filter.beforedate.AddMinutes(OnDemand.TimeOffSet).ToString("yyyy-MM-dd") + ".\n\n";
                                    emailBody += Configuration["PathToEmail"] + filename + "\n\n";
                                    emailBody += "Thanks,\n";
                                    emailBody += "Cisco Webex Team\n\n";
                                    emailBody += "We are here to help. Contact us anytime at webexxm-support@cisco.com";
                                }
                                else
                                {
                                    emailBody = "Hello, \n\nHere's the survey dispatch report for the time period- ";
                                    emailBody += OnDemand.Filter.afterdate.AddMinutes(OnDemand.TimeOffSet).ToString("yyyy-MM-dd") + " - " + OnDemand.Filter.beforedate.AddMinutes(OnDemand.TimeOffSet).ToString("yyyy-MM-dd") + ".\n\n";
                                    emailBody += Configuration["PathToEmail"] + filename + "\n\n";
                                    emailBody += "Please note, the attached file does not contain data splits and pivot table because no invitations were sent during the selected date range for this report.. To view data splits and pivot tables, generate a report for a different date range during which invites were sent." + ".\n\n";
                                    emailBody += "Thanks,\n";
                                    emailBody += "Cisco Webex Team\n\n";
                                    emailBody += "We are here to help. Contact us anytime at webexxm-support@cisco.com";
                                }

                                string emailSubject = "[Cisco WXM] Survey dispatch performance report";

                                #endregion

                                mailMessage.Subject = emailSubject;
                                mailMessage.Body = emailBody;
                                
                                await sendOutReport.SendOutEmails(mailMessage);
                                
                                log.logMessage += "Successfully completed on demand report dispatch on- " + DateTime.UtcNow.ToString() + "\n";
                                log.AddLogsToFile(DateTime.UtcNow);
                            }

                            bool unlock = await via.UnLockOnDemand();

                            if (unlock == false)
                            {
                                log.logMessage += $"Unable to unlock on demand report";
                            }
                            else
                                OnDemand = await via.GetOnDemandModel();
                        }
                    }
                    catch (Exception ex)
                    {
                        bool unlock = await via.UnLockOnDemand();

                        OnDemand = await via.GetOnDemandModel();

                        log.logMessage += $"Error in on demand report task {ex.Message}    {ex.StackTrace}";
                        log.AddLogsToFile(DateTime.UtcNow);
                        
                        continue;
                    }
                    try
                    {
                        if (NextUpload < DateTime.UtcNow)
                        {
                            await d.DataUploadTask();
                            NextUpload = NextUpload.AddMinutes(RunUploadEvery);
                        }
                    }
                    catch (Exception ex)
                    {
                        bool unlock = await via.UnLockOnDemand();

                        OnDemand = await via.GetOnDemandModel();

                        log.logMessage += $"Error in data upload process {ex.Message}    {ex.StackTrace}";
                        log.AddLogsToFile(DateTime.UtcNow);
                        continue;
                    }
                }
            }
            catch (Exception ex)
            {
                log.logMessage += $"Error in report task {ex.Message}    {ex.StackTrace}";
                log.AddLogsToFile(DateTime.UtcNow);
                return;
            }

        }

    }
}
