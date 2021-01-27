using Microsoft.Extensions.Configuration;
//using models;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using OfficeOpenXml.Table.PivotTable;
using FluentDateTime;
using XM.ID.Net;
using XM.ID.Invitations.Net;
using Newtonsoft.Json;

namespace DPReporting
{
    public class ReportCreator
    {
        IConfigurationRoot Configuration;
        readonly string WXM_BASE_URL;
        ApplicationLog log;
        private readonly ViaMongoDB via;
        private readonly List<Question> questions;
        private readonly List<Location> QuestionnairesWXM;
        private readonly UserProfile profile;
        private readonly Settings settings;
        private readonly List<ContentTemplate> templates;
        readonly HTTPWrapper hTTPWrapper;

        Regex NumberTypeRegEx = new Regex(@"^(?i)metric(?i)|^(?i)scale(?i)$|^(?i)slider(?i)$|(-\d)$|^(?i)number(?i)$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        public ReportCreator(IConfigurationRoot configuration, ApplicationLog applog, string WXMBearer)
        {
            Configuration = configuration;
            log = applog;

            via = new ViaMongoDB((IConfiguration)configuration);

            WXM_BASE_URL = Configuration["WXM_BASE_URL"];

            hTTPWrapper = new HTTPWrapper();

            string q = InvitationsMemoryCache.GetInstance().GetActiveQuestionsFromMemoryCache(WXMBearer, hTTPWrapper);
            if (!string.IsNullOrEmpty(q))
                questions = JsonConvert.DeserializeObject<List<Question>>(q);

            string questionnaires = InvitationsMemoryCache.GetInstance().GetQuestionnaireFromMemoryCache(WXMBearer, hTTPWrapper);
            if (!string.IsNullOrEmpty(questionnaires))
                QuestionnairesWXM = JsonConvert.DeserializeObject<List<Location>>(questionnaires);

            string p = InvitationsMemoryCache.GetInstance().GetUserProfileFromMemoryCache(WXMBearer, hTTPWrapper);
            if (!string.IsNullOrEmpty(p))
                profile = JsonConvert.DeserializeObject<UserProfile>(p);

            string s = InvitationsMemoryCache.GetInstance().GetSettingsFromMemoryCache(WXMBearer, hTTPWrapper);
            if (!string.IsNullOrEmpty(s))
                settings = JsonConvert.DeserializeObject<Settings>(s);

            string t = InvitationsMemoryCache.GetInstance().GetContentTemplatesFromMemoryCache(WXMBearer, hTTPWrapper);
            if (!string.IsNullOrEmpty(t))
                templates = JsonConvert.DeserializeObject<List<ContentTemplate>>(t);
        }

        public async Task<Tuple<byte[], bool>> GetOperationMetricsReport(FilterBy filter)
        {
            if (filter == null)
                return null;

            try
            {
                if (questions == null || QuestionnairesWXM == null || profile == null || settings == null || templates == null)
                    return null;

                AccountConfiguration a = await via.GetAccountConfiguration();
                List<string> Questionnaires = await via.GetQuestionnairesUsed();

                int TimeZoneOffset = (int)(profile.TimeZoneOffset == null ? settings.TimeZoneOffset : profile.TimeZoneOffset);
                
                string UTCTZD = TimeZoneOffset >= 0 ? "UTC+" : "UTC-";
                UTCTZD = UTCTZD + Math.Abs(Convert.ToInt32(TimeZoneOffset/60)).ToString() + ":" + Math.Abs(TimeZoneOffset%60).ToString();

                Question ZoneQuestion = questions.Where(x => x.QuestionTags.Contains("cc_zone"))?.FirstOrDefault();
                Question TouchPointQuestion = questions.Where(x => x.QuestionTags.Contains("cc_touchpoint"))?.FirstOrDefault();
                Question LocationQuestion = questions.Where(x => x.QuestionTags.Contains("cc_location"))?.FirstOrDefault();

                List<WXMPartnerMerged> MergedData = await via.GetMergedDataFromDb(filter);

                if (MergedData?.Count() == 0)
                    return null;

                ExcelPackage package = new ExcelPackage();

                #region Detailed Logs

                var sheet1 = package.Workbook.Worksheets.Add("Detailed Logs");

                sheet1.Cells[1, 1].Value = "Date Range: " + filter.afterdate.AddMinutes(TimeZoneOffset).ToString("dd/MM/yyyy h:mm tt") + " " + UTCTZD + " - " + filter.beforedate.AddMinutes(TimeZoneOffset).ToString("dd/MM/yyyy h:mm tt") + " " + UTCTZD;
                sheet1.Cells[1, 1].Style.Font.Bold = true;

                sheet1.Cells[2, 1].Value = "DeliveryWorkFlowId";
                sheet1.Cells[2, 1].Style.Font.Bold = true;
                sheet1.Cells[2, 2].Value = "TimeStamp";
                sheet1.Cells[2, 2].Style.Font.Bold = true;
                sheet1.Cells[2, 3].Value = "Questionnaire";
                sheet1.Cells[2, 3].Style.Font.Bold = true;
                sheet1.Cells[2, 4].Value = "Channel";
                sheet1.Cells[2, 4].Style.Font.Bold = true;
                sheet1.Cells[2, 5].Value = "Action";
                sheet1.Cells[2, 5].Style.Font.Bold = true;
                sheet1.Cells[2, 5].AddComment("Possible values for action: \r\n" +
                                              "Unsubscribe- User has clicked on unsubscribe \r\n" +
                                              "Unsubscribed- User has already unsubscribed from getting survey invites \r\n" +
                                              "Bounced- User did not receive invite as it was bounced \r\n" +
                                              "Exception- User did not receive invite due to an error \r\n" +
                                              "Displayed- User clicked on the survey link and it was displayed \r\n" +
                                              "Sent- Invite was sent to the user \r\n" +
                                              "Throttled- User did not receive invite due to the throttling logic \r\n" +
                                              "Answered- User answered the survey \r\n" +
                                              "Requested- Token creation has been requested \r\n" +
                                              "Rejected- Token creation has been rejected \r\n" +
                                              "Tokencreated- Survey token was created for the user to answer the survey \r\n" +
                                              "Error- User did not receive invite due to an error \r\n" +
                                              "Supressed- User did not receive invite as it was supressed \r\n" +
                                              "DispatchSuccessful- Invite was dispatched successfully to the user \r\n" +
                                              "DispatchUnsuccessful- Invite was not dispatched to the user due to some error", "WXM Team");
                sheet1.Cells[2, 5].Comment.AutoFit = true;
                sheet1.Cells[2, 6].Value = "Message";
                sheet1.Cells[2, 6].Style.Font.Bold = true;
                sheet1.Cells[2, 7].Value = "DispatchID";
                sheet1.Cells[2, 7].Style.Font.Bold = true;
                sheet1.Cells[2, 8].Value = "TargetHashed";
                sheet1.Cells[2, 8].Style.Font.Bold = true;
                sheet1.Cells[2, 9].Value = "Message Sequence";
                sheet1.Cells[2, 9].Style.Font.Bold = true;
                sheet1.Cells[2, 10].Value = "Message template";
                sheet1.Cells[2, 10].Style.Font.Bold = true;
                sheet1.Cells[2, 11].Value = "Token ID";
                sheet1.Cells[2, 11].Style.Font.Bold = true;

                FormatHeader(sheet1.Cells["A2:K2"], 3);

                int RowNo = 3;

                ExcelWorksheet DoDefaultValues(ExcelWorksheet sheet, WXMPartnerMerged data, int row)
                {
                    sheet.Cells[row, 1].Value = data.DeliveryWorkFlowId;
                    if (QuestionnairesWXM.Where(x => x.Name == data.Questionnaire)?.Count() > 0)
                        sheet1.Cells[row, 3].Value = QuestionnairesWXM.Where(x => x.Name == data.Questionnaire)?.FirstOrDefault().DisplayName + " (" + data.Questionnaire + ")";
                    else
                        sheet1.Cells[row, 3].Value = data.Questionnaire + " (Questionnaire not present)";
                    sheet.Cells[row, 11].Value = data._id;
                    if (a.DispatchChannels.Where(x => x.DispatchId == data.DispatchId)?.Count() > 0)
                        sheet.Cells[row, 7].Value = a.DispatchChannels.Where(x => x.DispatchId == data.DispatchId).FirstOrDefault().DispatchName + " (" + data.DispatchId + ")";
                    else
                        sheet.Cells[row, 7].Value = data.DispatchId + " (Dispatch not present)";

                    sheet.Cells[row, 8].Value = data.TargetHashed;

                    return sheet;
                }

                foreach (WXMPartnerMerged o in MergedData)
                {
                    try
                    {
                        foreach(DeliveryEvent d in o.Events)
                        {
                            if (d.Action == "Sent")
                            {
                                sheet1 = DoDefaultValues(sheet1, o, RowNo);
                                sheet1.Cells[RowNo, 4].Value = d.Channel;
                                sheet1.Cells[RowNo, 5].Value = "Sent";
                                sheet1.Cells[RowNo, 6].Value = d.Message;

                                string TemplateName = templates?.Where(x => x.Id == d.MessageTemplate)?.FirstOrDefault()?.Name;

                                string messagetemplate = null;

                                if (string.IsNullOrEmpty(TemplateName) && d.MessageTemplate == "Not Sent")
                                    messagetemplate = d.MessageTemplate;
                                else
                                {
                                    if (string.IsNullOrEmpty(TemplateName))
                                    {
                                        messagetemplate = d.MessageTemplate + " (Template not present)";
                                    }
                                    else
                                    {
                                        messagetemplate = TemplateName + " (" + d.MessageTemplate + ")";
                                    }
                                }

                                sheet1.Cells[RowNo, 10].Value = messagetemplate;
                                sheet1.Cells[RowNo, 9].Value = d.SentSequence == 0 ? "Message 1" : d.SentSequence != null ? "Message " + (d.SentSequence + 1)?.ToString() : null;
                                sheet1.Cells[RowNo, 2].Value = d.TimeStamp.AddMinutes(TimeZoneOffset).ToString("dd/MM/yyyy h:mm tt") + " " + UTCTZD;

                                RowNo++;
                            }
                            else
                            {
                                sheet1 = DoDefaultValues(sheet1, o, RowNo);
                                sheet1.Cells[RowNo, 4].Value = d.Channel;
                                sheet1.Cells[RowNo, 5].Value = d.Action;
                                //in case of dispatch status, need to add log message
                                sheet1.Cells[RowNo, 6].Value = d.Action?.ToLower() == "dispatchsuccessful" ||
                                    d.Action?.ToLower() == "dispatchunsuccessful" ?
                                    d.LogMessage : d.Message;
                                sheet1.Cells[RowNo, 2].Value = d.TimeStamp.Year == 0001 ? o.CreatedAt.AddMinutes(TimeZoneOffset).ToString("dd/MM/yyyy h:mm tt") + " " + UTCTZD :
                                d.TimeStamp.AddMinutes(TimeZoneOffset).ToString("dd/MM/yyyy h:mm tt") + " " + UTCTZD;

                                RowNo++;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        log.logMessage += $"Error in adding a log to the detailed logs excel sheet {ex.Message}    {ex.StackTrace}";
                        continue;
                    }

                }

                sheet1.Cells["A2:G2"].AutoFitColumns(10, 60);

                #endregion

                DateTime StartDate = filter.afterdate.AddMinutes(TimeZoneOffset);
                DateTime EndDate = filter.beforedate.AddMinutes(TimeZoneOffset);


                //needed to parse the months correctly in the raw data of the report
                Dictionary<string, string> ValidMonthLimits = new Dictionary<string, string>();

                if (StartDate.Month != EndDate.Month)
                {
                    for (int i = StartDate.Month; i <= EndDate.Month; i++)
                    {
                        if (i == StartDate.Month && StartDate.Day != 1)
                        {
                            ValidMonthLimits.Add(new DateTime(2015, i, 1).ToString("MMMM"),
                                new DateTime(2015, i, 1).ToString("MMMM") + " (From " + AddOrdinal(StartDate.Day) + " to " + AddOrdinal(StartDate.EndOfMonth().Day) + ")");
                        }
                        else if (i == EndDate.Month && EndDate.Day != EndDate.EndOfMonth().Day)
                        {
                            ValidMonthLimits.Add(new DateTime(2015, i, 1).ToString("MMMM"),
                                new DateTime(2015, i, 1).ToString("MMMM") + " (From " + AddOrdinal(1) + " to " + AddOrdinal(EndDate.Day) + ")");
                        }
                        else
                            ValidMonthLimits.Add(new DateTime(2015, i, 1).ToString("MMMM"), 
                                new DateTime(2015, i, 1).ToString("MMMM") + " (From " + AddOrdinal(1) + " to " + 
                                AddOrdinal(new DateTime(2015, i, 1).LastDayOfMonth().Day) + ")");
                    }
                }
                else
                {
                    ValidMonthLimits.Add(StartDate.ToString("MMMM"),
                        StartDate.ToString("MMMM") + " (From " + AddOrdinal(StartDate.Day) + " to " + AddOrdinal(EndDate.Day) + ")");
                }

                #region Datatable definition

                DataTable dt = new DataTable();
                dt.Clear();
                dt.Columns.Add("Questionnaire");
                dt.Columns.Add("Response Status");
                dt.Columns.Add("Message Sequence");
                dt.Columns.Add("Batch ID");
                dt.Columns.Add("Token ID");
                dt.Columns.Add("DeliveryWorkFlowId");
                dt.Columns.Add("Response Timestamp");
                dt.Columns.Add("Sent Month");
                dt.Columns.Add("Answered Month");
                dt.Columns.Add("Requested At");
                dt.Columns.Add("Last Updated");
                dt.Columns.Add("Requested");
                dt.Columns.Add("RequestedChannel");
                dt.Columns.Add("Token Created Status");
                dt.Columns.Add("TokenCreatedChannel");
                dt.Columns.Add("Sent Status");
                dt.Columns.Add("Channel");
                dt.Columns.Add("SentMessage");
                dt.Columns.Add("Message Template");
                dt.Columns.Add("Completion Status");
                dt.Columns.Add("Rejected");
                dt.Columns.Add("RejectedChannel");
                dt.Columns.Add("Error Status");
                dt.Columns.Add("ErrorChannel");
                dt.Columns.Add("ErrorMessage");
                dt.Columns.Add("Supressed Status");
                dt.Columns.Add("SupressedChannel");
                dt.Columns.Add("DispatchStatus");
                dt.Columns.Add("DispatchStatusChannel");
                dt.Columns.Add("DispatchStatusMessage");
                dt.Columns.Add("Throttling Status");
                dt.Columns.Add("Clicked Unsubscribe");
                dt.Columns.Add("UnsubscribeChannel");
                dt.Columns.Add("Unsubscribed Status");
                dt.Columns.Add("Bounced Status");
                dt.Columns.Add("BouncedChannel");
                dt.Columns.Add("Exception Status");
                dt.Columns.Add("ExceptionCount");
                dt.Columns.Add("ExceptionChannel");
                dt.Columns.Add("ExceptionMessage");
                dt.Columns.Add("Displayed Status");
                dt.Columns.Add("DispatchId");
                dt.Columns.Add("TargetHashed");
                dt.Columns.Add("RejectedMessage");

                Dictionary<string, string> QuestionIdTextMapping = new Dictionary<string, string>();

                //take the questions present in the dp related qnrs and create the headers. make sure you specify which ones are number type or not 
                foreach (Question q in questions)
                {
                    QuestionIdTextMapping.Add(q.Id, q.Text);

                    if (q.Text?.ToLower()?.Contains("batchid") == true ||
                                    q.Text?.ToLower()?.Contains("deliveryplanid") == true ||
                                    q.Text?.ToLower()?.Contains("token id") == true || 
                                    q.QuestionTags.Select(x => x.ToLower())?.Contains("cc_channel") == true)
                        continue;

                    if (q.DisplayLocation?.Intersect(Questionnaires)?.Count() == 0 && (q.DisplayLocation != null && q.DisplayLocation?.Count() != 0))
                        continue;

                    if (q.Text != null)
                    {
                        if (!dt.Columns?.Contains(q.Text) == true)
                            dt.Columns.Add(q.Text);

                        if (NumberTypeRegEx.IsMatch(q.DisplayType))
                            dt.Columns[q.Text].DataType = typeof(float);
                    }
                }

                List<string> QuestionHeaderColumn = new List<string>();

                //token level variables
                int SentCount = 0;
                int ThrottledCount = 0;
                int UnsubscribedCount = 0;
                int BouncedCount = 0;
                int ExceptionCount = 0;
                int AnsweredCount = 0;
                int CompletedCount = 0;
                int ErrorCount = 0;


                foreach (WXMPartnerMerged m in MergedData)
                {
                    int RemindersSent = 0;

                    if (m.Sent)
                    {
                        RemindersSent = m.Events.Where(x => x.SentSequence != null)?.Select(x => x.SentSequence)?.Max() == null ? 0 
                            : (int) m.Events.Where(x => x.SentSequence != null)?.Select(x => x.SentSequence)?.Max(); //Convert.ToInt32(m.SentSequence.Split(" ").LastOrDefault()) - 1;
                    }

                    List<int> ExceptionSequences = new List<int>();

                    DateTime? LastSentTime = m.Events.Where(x => x.SentSequence == RemindersSent &&
                                            x.Action?.ToLower() == "sent")?.FirstOrDefault()?.TimeStamp;

                    if (LastSentTime == null)
                    {
                        RemindersSent = m.Events.Where(x => x.Action?.ToLower() == "exception")?.Count() == null ? 0 :
                                            m.Events.Where(x => x.Action?.ToLower() == "exception").Count() == 0 ? 0 :
                                            m.Events.Where(x => x.Action?.ToLower() == "exception").Count() - 1;

                    }
                    else
                    {
                        var ExceptionAfterSent = m.Events.Where(x => x.Action?.ToLower() == "exception" && x.TimeStamp > LastSentTime);

                        if (ExceptionAfterSent != null && ExceptionAfterSent?.Count() > 0)
                        {
                            RemindersSent = RemindersSent + ExceptionAfterSent.Count(); //starts from 0
                        }
                    }

                    if (m.Exception)
                    {
                        for (int i = 0; i <= RemindersSent; i++)
                        {
                            if (m.Events.Where(x => x.SentSequence == i && x.Action?.ToLower() == "sent")?.FirstOrDefault() == null)
                            {
                                ExceptionSequences.Add(i);
                            }
                        }
                    }

                    for (int i = 0; i <= RemindersSent; i++)
                    {
                        try
                        {
                            DataRow row = dt.NewRow();

                            if (QuestionnairesWXM.Where(x => x.Name == m.Questionnaire)?.Count() > 0)
                                row["Questionnaire"] = QuestionnairesWXM.Where(x => x.Name == m.Questionnaire)?.FirstOrDefault().DisplayName + " (" + m.Questionnaire + ")";
                            else
                                row["Questionnaire"] = m.Questionnaire + " (Questionnaire not present)";
                            row["Response Status"] = i == RemindersSent && m.Answered ? "Answered" :
                                m.Events?.Where(x => x.Action?.ToLower()?.Contains("sent") == true && 
                                x.SentSequence == i)?.FirstOrDefault() == null ? "Not Sent" : "Unanswered";
                            row["Batch ID"] = m.BatchId;
                            row["Token ID"] = m._id;
                            row["DeliveryWorkFlowId"] = m.DeliveryWorkFlowId;
                            row["Response Timestamp"] = m.AnsweredAt.Year == 0001 ? null : i == RemindersSent && m.Answered ? m.AnsweredAt.AddMinutes(TimeZoneOffset).ToString("dd/MM/yyyy h:mm tt") + " " + UTCTZD : null;

                            string SentMonth = m.Events?.Where(x => x.Action?.ToLower()?.Contains("sent") == true
                                && x.SentSequence == i)?.FirstOrDefault() == null ? "Not Sent" :
                                "Sent in " + m.Events?.Where(x => x.Action?.ToLower()?.Contains("sent") == true
                                && x.SentSequence == i)?.FirstOrDefault()?.TimeStamp.AddMinutes(TimeZoneOffset).ToString("MMMM");
                            

                            string AnsweredMonth = i == RemindersSent && m.Answered ? "Answered in " + m.AnsweredAt.AddMinutes(TimeZoneOffset).ToString("MMMM") : "Unanswered";

                            if (ValidMonthLimits?.Keys?.Contains(SentMonth) == true)
                                SentMonth = ValidMonthLimits[SentMonth];

                            row["Sent Month"] = SentMonth;
                            row["Answered Month"] = AnsweredMonth;

                            row["Requested At"] = m.CreatedAt.Year == 0001 ? null : m.CreatedAt.AddMinutes(TimeZoneOffset).ToString("dd/MM/yyyy h:mm tt") + " " + UTCTZD;
                            row["Last Updated"] = m.LastUpdated.Year == 0001 ? null : m.LastUpdated.AddMinutes(TimeZoneOffset).ToString("dd/MM/yyyy h:mm tt") + " " + UTCTZD;
                            row["Requested"] = m.Requested ? "Requested" : "Not Requested";
                            row["RequestedChannel"] = m.RequestedChannel;
                            row["Token Created Status"] = m.TokenCreated ? "Token Created" : "Token Not Created";
                            row["TokenCreatedChannel"] = m.TokenCreatedChannel;
                            row["Sent Status"] = m.Events?.Where(x => x.Action?.ToLower()?.Contains("sent") == true &&
                            x.SentSequence == i)?.FirstOrDefault() == null ? "Not Sent" : "Sent";
                            row["Channel"] = m.Sent ? m.Events?.Where(x => x.Action?.ToLower()?.Contains("sent") == true && 
                            x.SentSequence == i)?.FirstOrDefault() == null ? "Not Sent" : m.Events?.Where(x => x.Action?.ToLower()?.Contains("sent") == true &&
                            x.SentSequence == i)?.FirstOrDefault()?.Channel?.Split(":")?.FirstOrDefault() : "Not Sent";
                            row["SentMessage"] = m.Sent ? m.Events?.Where(x => x.Action?.ToLower()?.Contains("sent") == true &&
                            x.SentSequence == i)?.FirstOrDefault() == null ? "Not Sent" : m.Events?.Where(x => x.Action?.ToLower()?.Contains("sent") == true &&
                            x.SentSequence == i)?.FirstOrDefault()?.Message : "Not Sent";
                            
                            string TemplateId = m.Sent ? m.Events?.Where(x => x.Action?.ToLower()?.Contains("sent") == true &&
                            x.SentSequence == i)?.FirstOrDefault() == null ? "Not Sent" : m.Events?.Where(x => x.Action?.ToLower()?.Contains("sent") == true &&
                            x.SentSequence == i)?.FirstOrDefault()?.MessageTemplate : "Not Sent";

                            string TemplateName = templates?.Where(x => x.Id == TemplateId)?.FirstOrDefault()?.Name;

                            string messagetemplate = null;

                            if (string.IsNullOrEmpty(TemplateName) && TemplateId == "Not Sent")
                                messagetemplate = TemplateId;
                            else 
                            {
                                if (string.IsNullOrEmpty(TemplateName))
                                {
                                    messagetemplate = TemplateId + " (Template not present)";
                                }
                                else
                                {
                                    messagetemplate = TemplateName + " (" + TemplateId + ")";
                                }
                            }

                            row["Message Template"] = messagetemplate;
                            row["Completion Status"] = i == RemindersSent ? m.Partial ? "Partial" : m.Answered ? "Completed" : "Unanswered" : "Unanswered";
                            row["Rejected"] = m.Rejected ? "Rejected" : "Not Rejected";
                            row["RejectedChannel"] = m.RejectedChannel;
                            row["RejectedMessage"] = m.RejectedMessage;
                            row["Error Status"] = m.Error ? "Error" : "No Error";
                            row["ErrorChannel"] = m.ErrorChannel;
                            row["ErrorMessage"] = m.ErrorMessage;
                            row["Supressed Status"] = m.Supressed ? "Supressed" : "Not Supressed";
                            row["SupressedChannel"] = m.SupressedChannel;

                            row["DispatchStatus"] = m.Events?.Where(x => x.Action?.ToLower()?
                            .Contains("dispatchsuccessful") == true && 
                            i.ToString() == x.Message?.Split("=")?.LastOrDefault())?.FirstOrDefault() != null 
                            ? "Successful" : m.Events?.Where(x => x.Action?.ToLower()?
                            .Contains("dispatchunsuccessful") == true &&
                            i.ToString() == x.Message?.Split("=")?.LastOrDefault())?.FirstOrDefault() != null ? "Unsuccessful" 
                            : "Unsuccessful";
                            row["DispatchStatusChannel"] = m.Events?.Where(x => (x.Action?.ToLower()?
                            .Contains("dispatchsuccessful") == true || x.Action?.ToLower()?
                            .Contains("dispatchunsuccessful") == true) && 
                            i.ToString() == x.Message?.Split("=")?.LastOrDefault())?.FirstOrDefault()?.Channel;
                            row["DispatchStatusMessage"] = m.Events?.Where(x => (x.Action?.ToLower()?
                            .Contains("dispatchsuccessful") == true || x.Action?.ToLower()?
                            .Contains("dispatchunsuccessful") == true) && 
                            i.ToString() == x.Message?.Split("=")?.LastOrDefault())?.FirstOrDefault()?.LogMessage;

                            row["Throttling Status"] = m.Throttled ? "Throttled" : "Not Throttled";
                            if (a.DispatchChannels.Where(x => x.DispatchId == m.DispatchId)?.Count() > 0)
                                row["DispatchId"] = a.DispatchChannels.Where(x => x.DispatchId == m.DispatchId).FirstOrDefault().DispatchName + " (" + m.DispatchId + ")";
                            else
                                row["DispatchId"] = m.DispatchId + " (Dispatch not present)";
                            row["TargetHashed"] = m.TargetHashed;
                            row["Clicked Unsubscribe"] = m.Unsubscribe ? "Yes" : "No";
                            row["UnsubscribeChannel"] = m.UnsubscribeChannel;
                            row["Unsubscribed Status"] = m.Unsubscribed ? "Unsubscribed" : "Not Unsubscribed";
                            row["Bounced Status"] = m.Bounced ? "Bounced" : "Not Bounced";
                            row["BouncedChannel"] = m.BouncedChannel;
                            row["Exception Status"] = ExceptionSequences?.Contains(i) == true ?
                                "Exception" : "No Exception";
                            row["ExceptionMessage"] = ExceptionSequences?.Contains(i) == true ? 
                                m.Events.Where(x => x.Action?.ToLower()?.Contains("exception") == true)?.ToList()[ExceptionSequences.IndexOf(i)]?.Message : 
                                null;
                            row["ExceptionCount"] = m.ExceptionCount;
                            row["ExceptionChannel"] = ExceptionSequences?.Contains(i) == true ?
                                m.Events.Where(x => x.Action?.ToLower()?.Contains("exception") == true)?.ToList()[ExceptionSequences.IndexOf(i)]?.Channel :
                                null;
                            row["Displayed Status"] = m.Displayed ? "Displayed" : "Not Displayed";
                            row["Message Sequence"] = i == 0 ? "Message 1" : "Message " + (i + 1).ToString();

                            //check if reponses are present and then add them to the respective row
                            if ((m.Responses != null && m.Answered && i == RemindersSent) || (!m.Answered && m.Responses != null) || (m.Answered && i != RemindersSent))
                            {
                                foreach (Response r in m.Responses)
                                {
                                    if (m.Answered && i != RemindersSent && 
                                        (questions.Where(x => x.Id == r.QuestionId)?.FirstOrDefault()?.StaffFill == true || 
                                        questions.Where(x => x.Id == r.QuestionId)?.FirstOrDefault()?.QuestionTags?.
                                        Intersect(new List<string> { "cc_zone", "cc_location", "cc_touchpoint" })?.Count() != 0))
                                    {
                                        if (r.QuestionText?.ToLower()?.Contains("batchid") == true ||
                                            r.QuestionText?.ToLower()?.Contains("deliveryplanid") == true ||
                                            r.QuestionText?.ToLower()?.Contains("token id") == true)
                                            continue;

                                        if (QuestionIdTextMapping.Keys.Contains(r.QuestionId) || (r.QuestionText != null && dt.Columns.Contains(r.QuestionText)))
                                        {
                                            if (r.TextInput == null)
                                                row[QuestionIdTextMapping[r.QuestionId]] = (float)r.NumberInput;
                                            else
                                                row[QuestionIdTextMapping[r.QuestionId]] = r.TextInput;
                                        }
                                        else if (QuestionHeaderColumn.Contains(r.QuestionId) || (r.QuestionId != null && dt.Columns.Contains(r.QuestionId)))
                                        {
                                            //in case certain questions are not present- this condition is used- down side is data in excel would be text format

                                            row[r.QuestionId] = r.TextInput == null ? r.NumberInput.ToString() : r.TextInput;
                                        }
                                        else
                                        {
                                            dt.Columns.Add(r.QuestionText == null ? r.QuestionId : r.QuestionText);

                                            QuestionHeaderColumn.Add(r.QuestionId);

                                            row[r.QuestionText == null ? r.QuestionId : r.QuestionText] = r.TextInput == null ? r.NumberInput.ToString() : r.TextInput;
                                        }
                                    }
                                    else if ((m.Responses != null && m.Answered && i == RemindersSent) || (!m.Answered && m.Responses != null))
                                    {
                                        if (r.QuestionText?.ToLower()?.Contains("batchid") == true ||
                                            r.QuestionText?.ToLower()?.Contains("deliveryplanid") == true ||
                                            r.QuestionText?.ToLower()?.Contains("token id") == true)
                                            continue;

                                        if (QuestionIdTextMapping.Keys.Contains(r.QuestionId) || (r.QuestionText != null && dt.Columns.Contains(r.QuestionText)))
                                        {
                                            if (r.TextInput == null)
                                                row[QuestionIdTextMapping[r.QuestionId]] = (float)r.NumberInput;
                                            else
                                                row[QuestionIdTextMapping[r.QuestionId]] = r.TextInput;
                                        }
                                        else if (QuestionHeaderColumn.Contains(r.QuestionId) || (r.QuestionId != null && dt.Columns.Contains(r.QuestionId)))
                                        {
                                            //in case certain questions are not present- this condition is used- down side is data in excel would be text format

                                            row[r.QuestionId] = r.TextInput == null ? r.NumberInput.ToString() : r.TextInput;
                                        }
                                        else
                                        {
                                            dt.Columns.Add(r.QuestionText == null ? r.QuestionId : r.QuestionText);

                                            QuestionHeaderColumn.Add(r.QuestionId);

                                            row[r.QuestionText == null ? r.QuestionId : r.QuestionText] = r.TextInput == null ? r.NumberInput.ToString() : r.TextInput;
                                        }
                                    }
                                }
                            }

                            dt.Rows.Add(row);
                        }
                        catch (Exception ex)
                        {
                            continue;
                        }
                    }

                    if (m.Sent)
                        SentCount++;
                    if (m.Throttled && !m.Sent)
                        ThrottledCount++;
                    if (m.Unsubscribed && !m.Sent)
                        UnsubscribedCount++;
                    if (m.Bounced && !m.Sent)
                        BouncedCount++;
                    if (m.Exception && !m.Sent)
                        ExceptionCount++;
                    if (m.Error && !m.Sent)
                        ErrorCount++;
                    if (m.Answered)
                        AnsweredCount++;
                    if (!m.Partial && m.Answered)
                        CompletedCount++;
                }

                List<string> DataForMonths = dt.AsEnumerable().Select(x => x["Sent Month"]?.ToString())?.Distinct()?.ToList();

                string DataNotPresentForMonthsMessage = "";

                foreach (string mon in ValidMonthLimits.Values)
                {
                    if (DataForMonths?.Where(x => x?.Contains(mon?.Split(" ")?.FirstOrDefault()) == true)?.Count() != 0)
                        continue;
                    else
                        DataNotPresentForMonthsMessage = DataNotPresentForMonthsMessage + mon + ", ";
                }

                dt.DefaultView.Sort = "Token ID";
                dt = dt.DefaultView.ToTable();

                DataTable dt2 = new DataTable();

                try
                {
                   dt2 = dt.AsEnumerable().
                   Where(r => r.Field<string>("Sent Status") == "Sent").
                   OrderByDescending(y => y.Field<String>("Questionnaire")).
                   CopyToDataTable();
                }
                catch
                {
                    //flow for in case there is no sent data at all.. it'll hit an exception here
                    var sh = package.Workbook.Worksheets.Add("Raw Data All");

                    sh.Cells["A1"].LoadFromDataTable(dt, true, OfficeOpenXml.Table.TableStyles.Medium6);

                    sh.Cells["C1"].AddComment("Message 1 is the first invite and the reminders follow", "WXM team");
                    sh.Cells["C1"].Comment.AutoFit = true;

                    sh.Cells["T1"].AddComment("Whether the survey was completed or not if answered", "WXM team");
                    sh.Cells["T1"].Comment.AutoFit = true;

                    sh.Cells["AG1"].AddComment("Whether the clicked on unsubscribe", "WXM team");
                    sh.Cells["AG1"].Comment.AutoFit = true;

                    sh.Cells["AI1"].AddComment("Whether the survey was sent to an unsubscribed user", "WXM team");
                    sh.Cells["AI1"].Comment.AutoFit = true;

                    sh.Cells["AM1"].AddComment("Total exceptions for this particular token", "WXM team");
                    sh.Cells["AM1"].Comment.AutoFit = true;

                    return new Tuple<byte[], bool>( package.GetAsByteArray(), false);
                }

                dt2.DefaultView.Sort = "Token ID";
                dt2 = dt2.DefaultView.ToTable();

                DataTable dt3 = dt.AsEnumerable().
                    Where(r => r.Field<string>("Message Sequence") == "Message 1" &&
                    r.Field<string>("Sent Status") == "Sent").
                    OrderByDescending(y => y.Field<String>("Questionnaire")).
                    CopyToDataTable();

                dt3.DefaultView.Sort = "Token ID";
                dt3 = dt3.DefaultView.ToTable();

                DataTable dt4 = dt.AsEnumerable().
                    Where(r => r.Field<string>("Message Sequence") == "Message 1").
                    OrderByDescending(y => y.Field<String>("Questionnaire")).
                    CopyToDataTable();

                dt4.DefaultView.Sort = "Token ID";
                dt4 = dt4.DefaultView.ToTable();

                #endregion

                var sheet2 = package.Workbook.Worksheets.Add("Raw Data Invites Sent");

                sheet2.Cells["A1"].LoadFromDataTable(dt2, true, OfficeOpenXml.Table.TableStyles.Medium6);

                sheet2.Cells["C1"].AddComment("Message 1 is the first invite and the reminders follow", "WXM team");
                sheet2.Cells["C1"].Comment.AutoFit = true;

                sheet2.Cells["T1"].AddComment("Whether the survey was completed or not if answered", "WXM team");
                sheet2.Cells["T1"].Comment.AutoFit = true;

                sheet2.Cells["AF1"].AddComment("Whether the clicked on unsubscribe", "WXM team");
                sheet2.Cells["AF1"].Comment.AutoFit = true;

                sheet2.Cells["AH1"].AddComment("Whether the survey was sent to an unsubscribed user", "WXM team");
                sheet2.Cells["AH1"].Comment.AutoFit = true;

                sheet2.Cells["AM1"].AddComment("Total exceptions for this particular token", "WXM team");
                sheet2.Cells["AM1"].Comment.AutoFit = true;

                var sheet5 = package.Workbook.Worksheets.Add("Raw Data All");

                sheet5.Cells["A1"].LoadFromDataTable(dt, true, OfficeOpenXml.Table.TableStyles.Medium6);

                sheet5.Cells["C1"].AddComment("Message 1 is the first invite and the reminders follow", "WXM team");
                sheet5.Cells["C1"].Comment.AutoFit = true;

                sheet5.Cells["T1"].AddComment("Whether the survey was completed or not if answered", "WXM team");
                sheet5.Cells["T1"].Comment.AutoFit = true;

                sheet5.Cells["AG1"].AddComment("Whether the clicked on unsubscribe", "WXM team");
                sheet5.Cells["AG1"].Comment.AutoFit = true;

                sheet5.Cells["AI1"].AddComment("Whether the survey was sent to an unsubscribed user", "WXM team");
                sheet5.Cells["AI1"].Comment.AutoFit = true;

                sheet5.Cells["AM1"].AddComment("Total exceptions for this particular token", "WXM team");
                sheet5.Cells["AM1"].Comment.AutoFit = true;

                #region Overview sheet

                var OverviewSheet = package.Workbook.Worksheets.Add("Overview");

                OverviewSheet.Cells[1, 1, 1, 8].Merge = true;
                OverviewSheet.Cells[1, 1, 1, 8].Value = "Overall Performance Report";
                OverviewSheet.Cells[1, 1, 1, 8].Style.Font.Bold = true;
                FormatHeader(OverviewSheet.Cells[1, 1, 1, 8], 2);
                OverviewSheet.Cells[2, 1, 2, 8].Merge = true;
                OverviewSheet.Cells[2, 1, 2, 8].Value = "Date Range: " + filter.afterdate.AddMinutes(TimeZoneOffset).ToString("dd/MM/yyyy h:mm tt") + " " + UTCTZD + " - " + filter.beforedate.AddMinutes(TimeZoneOffset).ToString("dd/MM/yyyy h:mm tt") + " " + UTCTZD;
                FormatHeader(OverviewSheet.Cells[2, 1, 2, 8], 4);

                int total = SentCount + ThrottledCount + UnsubscribedCount + BouncedCount + ExceptionCount;

                OverviewSheet.Cells["B4"].Value = "Total Invites Requested";
                OverviewSheet.Column(9).Width = 16;
                OverviewSheet.Cells["B5"].Value = SentCount + ThrottledCount + UnsubscribedCount + BouncedCount + ExceptionCount;
                OverviewSheet.Cells["B4"].Style.Fill.PatternType = ExcelFillStyle.Solid;
                OverviewSheet.Cells["B4"].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(79, 129, 189));
                OverviewSheet.Cells["B4"].Style.Font.Color.SetColor(System.Drawing.Color.FromArgb(255, 255, 255));
                OverviewSheet.Cells[4, 2, 4, 9].Style.Font.Bold = true;
                OverviewSheet.Cells[4, 2, 4, 9].Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                OverviewSheet.Cells[4, 2, 4, 9].Style.WrapText = true;

                OverviewSheet.Cells["C4"].Value = "Throttled";
                OverviewSheet.Column(5).Width = 16;
                OverviewSheet.Cells["C5"].Value = ThrottledCount;
                OverviewSheet.Cells["D4"].Value = "Unsubscribed";
                OverviewSheet.Column(6).Width = 16;
                OverviewSheet.Cells["D5"].Value = UnsubscribedCount;
                OverviewSheet.Cells["E4"].Value = "Bounced";
                OverviewSheet.Column(7).Width = 16;
                OverviewSheet.Cells["E5"].Value = BouncedCount;
                OverviewSheet.Cells["F4"].Value = "Exception";
                OverviewSheet.Column(8).Width = 16;
                OverviewSheet.Cells["F5"].Value = ExceptionCount;

                OverviewSheet.Cells[4, 3, 4, 6].Style.Fill.PatternType = ExcelFillStyle.Solid;
                OverviewSheet.Cells[4, 3, 4, 6].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(192, 0, 0));
                OverviewSheet.Cells[4, 3, 4, 6].Style.Font.Color.SetColor(System.Drawing.Color.FromArgb(255, 255, 255));
                OverviewSheet.Cells[4, 3, 4, 6].Style.Font.Bold = true;
                OverviewSheet.Cells[4, 3, 4, 6].Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                OverviewSheet.Cells[4, 3, 4, 6].Style.WrapText = true;

                OverviewSheet.Cells["G4"].Value = "Total Invites Sent";
                OverviewSheet.Column(2).Width = 16;
                OverviewSheet.Cells["G5"].Value = SentCount;
                OverviewSheet.Cells["H4"].Value = "Total Invites Answered(Out of Total Sent)";
                OverviewSheet.Column(3).Width = 16;
                OverviewSheet.Cells["H5"].Value = AnsweredCount;
                OverviewSheet.Cells["I4"].Value = "Completed Responses(Out of Total Answered)";
                OverviewSheet.Column(4).Width = 16;
                OverviewSheet.Cells["I5"].Value = CompletedCount;

                OverviewSheet.Cells[4, 7, 4, 9].Style.Fill.PatternType = ExcelFillStyle.Solid;
                OverviewSheet.Cells[4, 7, 4, 9].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(84, 130, 53));
                OverviewSheet.Cells[4, 7, 4, 9].Style.Font.Color.SetColor(System.Drawing.Color.FromArgb(255, 255, 255));
                OverviewSheet.Cells[4, 7, 4, 9].Style.Font.Bold = true;
                OverviewSheet.Cells[4, 7, 4, 9].Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                OverviewSheet.Cells[4, 7, 4, 9].Style.WrapText = true;

                OverviewSheet.Row(4).Height = 45;

                if (total != 0)
                {
                    OverviewSheet.Cells["G6"].Value = (double)SentCount / total;
                    OverviewSheet.Cells["H6"].Value = (double)AnsweredCount / SentCount;
                    OverviewSheet.Cells["I6"].Value = (double)CompletedCount / AnsweredCount;
                    OverviewSheet.Cells["C6"].Value = (double)ThrottledCount / total;
                    OverviewSheet.Cells["D6"].Value = (double)UnsubscribedCount / total;
                    OverviewSheet.Cells["E6"].Value = (double)BouncedCount / total;
                    OverviewSheet.Cells["F6"].Value = (double)ExceptionCount / total;
                    OverviewSheet.Cells["B6"].Value = (double)(SentCount + ThrottledCount + UnsubscribedCount + BouncedCount + ExceptionCount) / total;
                }

                OverviewSheet.Cells[6, 2, 6, 9].Style.Numberformat.Format = "#0.00%";

                OverviewSheet.Cells["A5"].Value = "Count";
                OverviewSheet.Column(1).Width = 21;
                OverviewSheet.Cells["A5"].Style.Font.Bold = true;
                OverviewSheet.Cells["A6"].Value = "Percentage";
                OverviewSheet.Cells["A6"].Style.Font.Bold = true;
                OverviewSheet.Cells[5, 1, 6, 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
                OverviewSheet.Cells[5,1,6,1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(255, 242, 204));

                #endregion

                var dataRange = sheet2.Cells[sheet2.Dimension.Address];
                ExcelPivotTable pivotTable = null;

                #region Pivot 1

                try
                {
                    //pivot 1
                    var wsPivot1 = package.Workbook.Worksheets.Add("Split by Channel");

                    wsPivot1.Cells[1, 1, 1, 8].Merge = true;
                    wsPivot1.Cells[1, 1, 1, 8].Value = "Channel Performance Report";
                    wsPivot1.Cells[1, 1, 1, 8].Style.Font.Bold = true;
                    FormatHeader(wsPivot1.Cells[1, 1, 1, 8], 2);

                    wsPivot1.Cells[2, 1, 2, 8].Merge = true;
                    wsPivot1.Cells[2, 1, 2, 8].Value = "Date Range: " + filter.afterdate.AddMinutes(TimeZoneOffset).ToString("dd/MM/yyyy h:mm tt") + " " + UTCTZD + " - " + filter.beforedate.AddMinutes(TimeZoneOffset).ToString("dd/MM/yyyy h:mm tt") + " " + UTCTZD;
                    FormatHeader(wsPivot1.Cells[2, 1, 2, 8], 4);

                    wsPivot1.Cells[3, 1, 3, 8].Merge = true;
                    wsPivot1.Cells[3, 1, 3, 8].Value = "If you are unable to see the Pivot tables below, please click \"Enable Editing\" on the bar above to view them.";
                    FormatHeader(wsPivot1.Cells[3, 1, 3, 8], 4);
                    wsPivot1.Cells[3, 1, 3, 8].Style.Font.Italic = true;
                    wsPivot1.Cells[3, 1, 3, 8].Style.Font.Color.SetColor(System.Drawing.Color.FromArgb(255, 0, 0));
                    wsPivot1.Cells[3, 1, 3, 8].Style.Font.Bold = false;
                    wsPivot1.Cells[3, 1, 3, 8].Style.Font.Size = 8;

                    //dataRange.AutoFitColumns();
                    pivotTable = wsPivot1.PivotTables.Add(wsPivot1.Cells["A7"], dataRange, "AnsweredByChannel");

                    pivotTable.ConfigurePivot("Questionnaire", "Channel", "Channel");

                    #region copy text

                    wsPivot1.Cells[7, 5, 14, 14].Merge = true;
                    wsPivot1.Cells[7, 5, 14, 14].Value = "This pivot table contains data of total invites that " +
                        "were sent during the set date range split by Channels. " +
                        "The total invites sent excludes requests that were throttled OR unsubscribed. " +
                        "The total invites sent for each channel include multiple messages sent for the " +
                        "same token as follow up messages, and the total number of invites sent may be " +
                        "more than actual unique invites (tokens) sent. \r\n" +
                        "The data is further split as Answered or Unanswered to show the overall " +
                        "response rate based on invites that were Answered. If partial response " +
                        "collection is switched ON, then Answered responses will be further split into " +
                        "Completed and Partial, that will indicate the completion rates for Invites that " +
                        "were completely answered.";
                    wsPivot1.Cells[7, 5, 14, 14].Style.WrapText = true;
                    wsPivot1.Cells[7, 5, 14, 14].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    wsPivot1.Cells[7, 5, 14, 14].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;

                    wsPivot1.Cells[16, 5, 17, 14].Merge = true;
                    wsPivot1.Cells[16, 5, 17, 14].Value = "This pivot table is linked to data in  " +
                        "the sheet \"Raw Data Invites Sent\". Please do not edit that sheet. " +
                        "The following columns are being used from the \"Raw Data Invites Sent\" " +
                        "sheet for this pivot table";
                    wsPivot1.Cells[16, 5, 17, 14].Style.Font.Italic = true;
                    wsPivot1.Cells[16, 5, 17, 14].Style.Font.Color.SetColor(System.Drawing.Color.FromArgb(255, 0, 0));
                    wsPivot1.Cells[16, 5, 17, 14].Style.WrapText = true;
                    wsPivot1.Cells[16, 5, 17, 14].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    wsPivot1.Cells[16, 5, 17, 14].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;

                    wsPivot1.Cells[19, 5, 20, 5].Merge = true;
                    wsPivot1.Cells[19, 5, 20, 5].Value = "1st level";
                    wsPivot1.Cells[19, 5, 20, 5].Style.WrapText = true;
                    wsPivot1.Cells[19, 5, 20, 5].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    wsPivot1.Cells[19, 5, 20, 5].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                    wsPivot1.Cells[19, 6, 20, 14].Merge = true;
                    wsPivot1.Cells[19, 6, 20, 14].Value = "Channel- The medium through which the Invite was sent. For example- \"SMS\" or \"Email\"";
                    wsPivot1.Cells[19, 6, 20, 14].Style.WrapText = true;
                    wsPivot1.Cells[19, 6, 20, 14].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    wsPivot1.Cells[19, 6, 20, 14].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;

                    wsPivot1.Cells[21, 5, 22, 5].Merge = true;
                    wsPivot1.Cells[21, 5, 22, 5].Value = "2nd level";
                    wsPivot1.Cells[21, 5, 22, 5].Style.WrapText = true;
                    wsPivot1.Cells[21, 5, 22, 5].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    wsPivot1.Cells[21, 5, 22, 5].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                    wsPivot1.Cells[21, 6, 22, 14].Merge = true;
                    wsPivot1.Cells[21, 6, 22, 14].Value = "Response Status- Tells you whether an invite sent was \"Answered\" or \"Unanswered\"";
                    wsPivot1.Cells[21, 6, 22, 14].Style.WrapText = true;
                    wsPivot1.Cells[21, 6, 22, 14].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    wsPivot1.Cells[21, 6, 22, 14].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;

                    wsPivot1.Cells[23, 5, 24, 5].Merge = true;
                    wsPivot1.Cells[23, 5, 24, 5].Value = "3rd level";
                    wsPivot1.Cells[23, 5, 24, 5].Style.WrapText = true;
                    wsPivot1.Cells[23, 5, 24, 5].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    wsPivot1.Cells[23, 5, 24, 5].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                    wsPivot1.Cells[23, 6, 24, 14].Merge = true;
                    wsPivot1.Cells[23, 6, 24, 14].Value = "Completion Status- Tells you whether an invite sent was \"Completed\", \"Partial\" or \"Unanswered\"";
                    wsPivot1.Cells[23, 6, 24, 14].Style.WrapText = true;
                    wsPivot1.Cells[23, 6, 24, 14].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    wsPivot1.Cells[23, 6, 24, 14].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;

                    #endregion
                }
                catch (Exception ex)
                {
                    log.logMessage += $"Error generating the excel sheet with channel metrics {ex.Message}    {ex.StackTrace}";
                }
                #endregion

                ExcelWorksheet DoDefaultCopy(ExcelWorksheet sheet, string SplitBy)
                {
                    sheet.Cells[7, 5, 14, 14].Merge = true;
                    sheet.Cells[7, 5, 14, 14].Value = "This pivot table contains data of total invites that " +
                        "were sent during the set date range split by " + SplitBy +
                        "The total invites sent excludes requests that were throttled OR unsubscribed. \r\n" +
                        "The data is further split as Answered or Unanswered to show the overall " +
                        "response rate based on invites that were Answered. If partial response " +
                        "collection is switched ON, then Answered responses will be further split into " +
                        "Completed and Partial, that will indicate the completion rates for Invites that " +
                        "were completely answered.";
                    sheet.Cells[7, 5, 14, 14].Style.WrapText = true;
                    sheet.Cells[7, 5, 14, 14].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    sheet.Cells[7, 5, 14, 14].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;

                    sheet.Cells[16, 5, 17, 14].Merge = true;
                    sheet.Cells[16, 5, 17, 14].Value = "This pivot table is linked to data in  " +
                        "the sheet \"Raw Data Invites Sent\". Please do not edit that sheet. " +
                        "The following columns are being used from the \"Raw Data Invites Sent\" " +
                        "sheet for this pivot table";
                    sheet.Cells[16, 5, 17, 14].Style.Font.Italic = true;
                    sheet.Cells[16, 5, 17, 14].Style.Font.Color.SetColor(System.Drawing.Color.FromArgb(255, 0, 0));
                    sheet.Cells[16, 5, 17, 14].Style.WrapText = true;
                    sheet.Cells[16, 5, 17, 14].Style.VerticalAlignment = ExcelVerticalAlignment.Top;

                    string firstLevel = "";

                    switch (SplitBy.ToLower())
                    {
                        case "questionnaire":
                            firstLevel = "Questionnaire- The questionnaire configured in WXM linked to the invite sent";
                            break;
                        case "dispatch":
                            firstLevel = "DispatchId- The unique dispatches for invites sent using WXM";
                            break;
                        case "message template":
                            firstLevel = "Message Template- The message template used in the invite";
                            break;
                        case "zone":
                            firstLevel = "Zone- The zone configured in WXM for which the invite was sent";
                            break;
                        case "location":
                            firstLevel = "Locations- The location configured in WXM for which the invite was sent";
                            break;
                        case "touch point":
                            firstLevel = "Touch points- The touchpoint configured in WXM for which the invite was sent";
                            break;
                        case "sent sequence":
                            firstLevel = "Message Sequence- The sequence at which the invite was sent (Whether initial invite or reminder)";
                            break;
                        default:
                            break;
                    }

                    sheet.Cells[19, 5, 20, 5].Merge = true;
                    sheet.Cells[19, 5, 20, 5].Value = "1st level";
                    sheet.Cells[19, 5, 20, 5].Style.WrapText = true;
                    sheet.Cells[19, 5, 20, 5].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    sheet.Cells[19, 5, 20, 5].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                    sheet.Cells[19, 6, 20, 14].Merge = true;
                    sheet.Cells[19, 6, 20, 14].Value = firstLevel;
                    sheet.Cells[19, 6, 20, 14].Style.WrapText = true;
                    sheet.Cells[19, 6, 20, 14].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    sheet.Cells[19, 6, 20, 14].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;

                    sheet.Cells[21, 5, 22, 5].Merge = true;
                    sheet.Cells[21, 5, 22, 5].Value = "2nd level";
                    sheet.Cells[21, 5, 22, 5].Style.WrapText = true;
                    sheet.Cells[21, 5, 22, 5].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    sheet.Cells[21, 5, 22, 5].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                    sheet.Cells[21, 6, 22, 14].Merge = true;
                    sheet.Cells[21, 6, 22, 14].Value = "Response Status - Tells you whether an invite sent was \"Answered\" or \"Unanswered\"";
                    sheet.Cells[21, 6, 22, 14].Style.WrapText = true;
                    sheet.Cells[21, 6, 22, 14].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    sheet.Cells[21, 6, 22, 14].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;

                    sheet.Cells[23, 5, 24, 5].Merge = true;
                    sheet.Cells[23, 5, 24, 5].Value = "3rd level";
                    sheet.Cells[23, 5, 24, 5].Style.WrapText = true;
                    sheet.Cells[23, 5, 24, 5].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    sheet.Cells[23, 5, 24, 5].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                    sheet.Cells[23, 6, 24, 14].Merge = true;
                    sheet.Cells[23, 6, 24, 14].Value = "Completion Status - Tells you whether an invite sent was \"Completed\", \"Partial\" or \"Unanswered\"";
                    sheet.Cells[23, 6, 24, 14].Style.WrapText = true;
                    sheet.Cells[23, 6, 24, 14].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    sheet.Cells[23, 6, 24, 14].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;

                    return sheet;
                }

                #region Pivot 2

                try
                {
                    //pivot 2
                    var wsPivot2 = package.Workbook.Worksheets.Add("Split by Questionnaires");

                    wsPivot2.Cells[1, 1, 1, 8].Merge = true;
                    wsPivot2.Cells[1, 1, 1, 8].Value = "Questionnaires Performance Report";
                    wsPivot2.Cells[1, 1, 1, 8].Style.Font.Bold = true;
                    FormatHeader(wsPivot2.Cells[1, 1, 1, 8], 2);

                    wsPivot2.Cells[2, 1, 2, 8].Merge = true;
                    wsPivot2.Cells[2, 1, 2, 8].Value = "Date Range: " + filter.afterdate.AddMinutes(TimeZoneOffset).ToString("dd/MM/yyyy h:mm tt") + " " + UTCTZD + " - " + filter.beforedate.AddMinutes(TimeZoneOffset).ToString("dd/MM/yyyy h:mm tt") + " " + UTCTZD;
                    FormatHeader(wsPivot2.Cells[2, 1, 2, 8], 4);

                    wsPivot2.Cells[3, 1, 3, 8].Merge = true;
                    wsPivot2.Cells[3, 1, 3, 8].Value = "If you are unable to see the Pivot tables below, please click \"Enable Editing\" on the bar above to view them.";
                    FormatHeader(wsPivot2.Cells[3, 1, 3, 8], 4);
                    wsPivot2.Cells[3, 1, 3, 8].Style.Font.Italic = true;
                    wsPivot2.Cells[3, 1, 3, 8].Style.Font.Color.SetColor(System.Drawing.Color.FromArgb(255, 0, 0));
                    wsPivot2.Cells[3, 1, 3, 8].Style.Font.Bold = false;
                    wsPivot2.Cells[3, 1, 3, 8].Style.Font.Size = 8;

                    //dataRange.AutoFitColumns();
                    pivotTable = wsPivot2.PivotTables.Add(wsPivot2.Cells["A7"], dataRange, "AnsweredByQnr");

                    pivotTable.ConfigurePivot("DeliveryWorkFlowId", "Questionnaire", "Questionnaire");

                    wsPivot2 = DoDefaultCopy(wsPivot2, "Questionnaire");
                }
                catch (Exception ex)
                {
                    log.logMessage += $"Error generating the excel sheet with Questionnaire metrics {ex.Message}    {ex.StackTrace}";
                }
                #endregion

                #region Pivot 3

                try
                {
                    //pivot 3
                    var wsPivot3 = package.Workbook.Worksheets.Add("Split by Month");

                    wsPivot3.Cells[1, 1, 1, 8].Merge = true;
                    wsPivot3.Cells[1, 1, 1, 8].Value = "Monthly Performance Report";
                    wsPivot3.Cells[1, 1, 1, 8].Style.Font.Bold = true;
                    FormatHeader(wsPivot3.Cells[1, 1, 1, 8], 2);

                    wsPivot3.Cells[2, 1, 2, 8].Merge = true;
                    wsPivot3.Cells[2, 1, 2, 8].Value = "Date Range: " + filter.afterdate.AddMinutes(TimeZoneOffset).ToString("dd/MM/yyyy h:mm tt") + " " + UTCTZD + " - " + filter.beforedate.AddMinutes(TimeZoneOffset).ToString("dd/MM/yyyy h:mm tt") + " " + UTCTZD;
                    FormatHeader(wsPivot3.Cells[2, 1, 2, 8], 4);

                    wsPivot3.Cells[3, 1, 3, 8].Merge = true;
                    wsPivot3.Cells[3, 1, 3, 8].Value = "If you are unable to see the Pivot tables below, please click \"Enable Editing\" on the bar above to view them.";
                    FormatHeader(wsPivot3.Cells[3, 1, 3, 8], 4);
                    wsPivot3.Cells[3, 1, 3, 8].Style.Font.Italic = true;
                    wsPivot3.Cells[3, 1, 3, 8].Style.Font.Color.SetColor(System.Drawing.Color.FromArgb(255, 0, 0));
                    wsPivot3.Cells[3, 1, 3, 8].Style.Font.Bold = false;
                    wsPivot3.Cells[3, 1, 3, 8].Style.Font.Size = 8;

                    //dataRange.AutoFitColumns();
                    pivotTable = wsPivot3.PivotTables.Add(wsPivot3.Cells["A7"], dataRange, "AnsweredByMonth");

                    pivotTable.ConfigurePivot("Questionnaire", "Sent Month", "Month", false, true, "Total Sent", "Answered Month");

                    #region copy text

                    if (!string.IsNullOrEmpty(DataNotPresentForMonthsMessage))
                    {
                        wsPivot3.Cells[5, 5, 5, 14].Merge = true;
                        wsPivot3.Cells[5, 5, 5, 14].Value = "* No data available for " + DataNotPresentForMonthsMessage;
                        wsPivot3.Cells[5, 5, 5, 14].Style.Font.Italic = true;
                        wsPivot3.Cells[5, 5, 5, 14].Style.Font.Color.SetColor(System.Drawing.Color.FromArgb(255, 0, 0));
                    }

                    wsPivot3.Cells[7, 5, 14, 14].Merge = true;
                    wsPivot3.Cells[7, 5, 14, 14].Value = "This pivot table contains data of total invites that " +
                        "were sent during the set date range split by Months. The total invites sent " +
                        "excludes requests that were throttled OR unsubscribed. Some months in the " +
                        "selected date range may not have full month data. See table for details. \r\n" +
                        "The data is further split as Answered or Unanswered to show the overall " +
                        "response rate based on invites that were Answered. If partial response collection " +
                        "is switched ON, then Answered responses will be further split into Completed and " +
                        "Partial, that will indicate the completion rates for Invites that were completely " +
                        "answered.";
                    wsPivot3.Cells[7, 5, 14, 14].Style.WrapText = true;
                    wsPivot3.Cells[7, 5, 14, 14].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    wsPivot3.Cells[7, 5, 14, 14].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;

                    wsPivot3.Cells[16, 5, 17, 14].Merge = true;
                    wsPivot3.Cells[16, 5, 17, 14].Value = "This pivot table is linked to data in  " +
                        "the sheet \"Raw Data Invites Sent\". Please do not edit that sheet. " +
                        "The following columns are being used from the \"Raw Data Invites Sent\" " +
                        "sheet for this pivot table";
                    wsPivot3.Cells[16, 5, 17, 14].Style.Font.Italic = true;
                    wsPivot3.Cells[16, 5, 17, 14].Style.Font.Color.SetColor(System.Drawing.Color.FromArgb(255, 0, 0));
                    wsPivot3.Cells[16, 5, 17, 14].Style.WrapText = true;
                    wsPivot3.Cells[16, 5, 17, 14].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    wsPivot3.Cells[16, 5, 17, 14].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;

                    wsPivot3.Cells[19, 5, 20, 5].Merge = true;
                    wsPivot3.Cells[19, 5, 20, 5].Value = "1st level";
                    wsPivot3.Cells[19, 5, 20, 5].Style.WrapText = true;
                    wsPivot3.Cells[19, 5, 20, 5].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    wsPivot3.Cells[19, 5, 20, 5].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                    wsPivot3.Cells[19, 6, 20, 14].Merge = true;
                    wsPivot3.Cells[19, 6, 20, 14].Value = "Month- The month at which the invite was sent";
                    wsPivot3.Cells[19, 6, 20, 14].Style.WrapText = true;
                    wsPivot3.Cells[19, 6, 20, 14].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    wsPivot3.Cells[19, 6, 20, 14].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;

                    wsPivot3.Cells[21, 5, 22, 5].Merge = true;
                    wsPivot3.Cells[21, 5, 22, 5].Value = "2nd level";
                    wsPivot3.Cells[21, 5, 22, 5].Style.WrapText = true;
                    wsPivot3.Cells[21, 5, 22, 5].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    wsPivot3.Cells[21, 5, 22, 5].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                    wsPivot3.Cells[21, 6, 22, 14].Merge = true;
                    wsPivot3.Cells[21, 6, 22, 14].Value = "Response Status- Tells you whether an invite sent was \"Answered\" or \"Unanswered\"";
                    wsPivot3.Cells[21, 6, 22, 14].Style.WrapText = true;
                    wsPivot3.Cells[21, 6, 22, 14].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    wsPivot3.Cells[21, 6, 22, 14].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;

                    wsPivot3.Cells[23, 5, 24, 5].Merge = true;
                    wsPivot3.Cells[23, 5, 24, 5].Value = "3rd level";
                    wsPivot3.Cells[23, 5, 24, 5].Style.WrapText = true;
                    wsPivot3.Cells[23, 5, 24, 5].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    wsPivot3.Cells[23, 5, 24, 5].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                    wsPivot3.Cells[23, 6, 24, 14].Merge = true;
                    wsPivot3.Cells[23, 6, 24, 14].Value = "Completion Status- Tells you whether an invite sent was \"Completed\", \"Partial\" or \"Unanswered\"";
                    wsPivot3.Cells[23, 6, 24, 14].Style.WrapText = true;
                    wsPivot3.Cells[23, 6, 24, 14].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    wsPivot3.Cells[23, 6, 24, 14].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;

                    #endregion
                }
                catch (Exception ex)
                {
                    log.logMessage += $"Error generating the excel sheet with Month metrics {ex.Message}    {ex.StackTrace}";
                }


                #endregion

                #region Pivot 4

                try
                {
                    //pivot 4
                    var wsPivot4 = package.Workbook.Worksheets.Add("Split by Dispatch");

                    wsPivot4.Cells[1, 1, 1, 8].Merge = true;
                    wsPivot4.Cells[1, 1, 1, 8].Value = "Dispatch Performance Report";
                    wsPivot4.Cells[1, 1, 1, 8].Style.Font.Bold = true;
                    FormatHeader(wsPivot4.Cells[1, 1, 1, 8], 2);

                    wsPivot4.Cells[2, 1, 2, 8].Merge = true;
                    wsPivot4.Cells[2, 1, 2, 8].Value = "Date Range: " + filter.afterdate.AddMinutes(TimeZoneOffset).ToString("dd/MM/yyyy h:mm tt") + " " + UTCTZD + " - " + filter.beforedate.AddMinutes(TimeZoneOffset).ToString("dd/MM/yyyy h:mm tt") + " " + UTCTZD;
                    FormatHeader(wsPivot4.Cells[2, 1, 2, 8], 4);

                    wsPivot4.Cells[3, 1, 3, 8].Merge = true;
                    wsPivot4.Cells[3, 1, 3, 8].Value = "If you are unable to see the Pivot tables below, please click \"Enable Editing\" on the bar above to view them.";
                    FormatHeader(wsPivot4.Cells[3, 1, 3, 8], 4);
                    wsPivot4.Cells[3, 1, 3, 8].Style.Font.Italic = true;
                    wsPivot4.Cells[3, 1, 3, 8].Style.Font.Color.SetColor(System.Drawing.Color.FromArgb(255, 0, 0));
                    wsPivot4.Cells[3, 1, 3, 8].Style.Font.Bold = false;
                    wsPivot4.Cells[3, 1, 3, 8].Style.Font.Size = 8;

                    //dataRange.AutoFitColumns();
                    pivotTable = wsPivot4.PivotTables.Add(wsPivot4.Cells["A7"], dataRange, "AnsweredByDispatch");

                    pivotTable.ConfigurePivot("Questionnaire", "DispatchId", "DispatchId");

                    List<string> UniqueDispatches = dt.AsEnumerable().Select(x => x["DispatchId"]?.ToString())?.Distinct()?.ToList();

                    wsPivot4 = DoDefaultCopy(wsPivot4, "dispatch");
                }
                catch (Exception ex)
                {
                    log.logMessage += $"Error generating the excel sheet with DispatchId metrics {ex.Message}    {ex.StackTrace}";
                }

                #endregion

                #region Pivot 5

                try
                {
                    //pivot 5
                    var wsPivot5 = package.Workbook.Worksheets.Add("Split by Message Template");

                    wsPivot5.Cells[1, 1, 1, 8].Merge = true;
                    wsPivot5.Cells[1, 1, 1, 8].Value = "Message Template Performance Report";
                    wsPivot5.Cells[1, 1, 1, 8].Style.Font.Bold = true;
                    FormatHeader(wsPivot5.Cells[1, 1, 1, 8], 2);

                    wsPivot5.Cells[2, 1, 2, 8].Merge = true;
                    wsPivot5.Cells[2, 1, 2, 8].Value = "Date Range: " + filter.afterdate.AddMinutes(TimeZoneOffset).ToString("dd/MM/yyyy h:mm tt") + " " + UTCTZD + " - " + filter.beforedate.AddMinutes(TimeZoneOffset).ToString("dd/MM/yyyy h:mm tt") + " " + UTCTZD;
                    FormatHeader(wsPivot5.Cells[2, 1, 2, 8], 4);

                    wsPivot5.Cells[3, 1, 3, 8].Merge = true;
                    wsPivot5.Cells[3, 1, 3, 8].Value = "If you are unable to see the Pivot tables below, please click \"Enable Editing\" on the bar above to view them.";
                    FormatHeader(wsPivot5.Cells[3, 1, 3, 8], 4);
                    wsPivot5.Cells[3, 1, 3, 8].Style.Font.Italic = true;
                    wsPivot5.Cells[3, 1, 3, 8].Style.Font.Color.SetColor(System.Drawing.Color.FromArgb(255, 0, 0));
                    wsPivot5.Cells[3, 1, 3, 8].Style.Font.Bold = false;
                    wsPivot5.Cells[3, 1, 3, 8].Style.Font.Size = 8;

                    //dataRange.AutoFitColumns();
                    pivotTable = wsPivot5.PivotTables.Add(wsPivot5.Cells["A7"], dataRange, "AnsweredByMessageTemplate");

                    pivotTable.ConfigurePivot("Questionnaire", "Message Template", "Message Template");

                    List<string> UniqueTemplates = dt.AsEnumerable().Select(x => x["Message Template"]?.ToString())?.Distinct()?.ToList();

                    wsPivot5 = DoDefaultCopy(wsPivot5, "message template");
                }
                catch (Exception ex)
                {
                    log.logMessage += $"Error generating the excel sheet with Message Template metrics {ex.Message}    {ex.StackTrace}";
                }

                #endregion

                #region Pivot 7

                try
                {
                    //pivot 7
                    var wsPivot7 = package.Workbook.Worksheets.Add("Split by Sent Sequence");

                    wsPivot7.Cells[1, 1, 1, 8].Merge = true;
                    wsPivot7.Cells[1, 1, 1, 8].Value = "Sent Sequence Performance Report";
                    wsPivot7.Cells[1, 1, 1, 8].Style.Font.Bold = true;
                    FormatHeader(wsPivot7.Cells[1, 1, 1, 8], 2);

                    wsPivot7.Cells[2, 1, 2, 8].Merge = true;
                    wsPivot7.Cells[2, 1, 2, 8].Value = "Date Range: " + filter.afterdate.AddMinutes(TimeZoneOffset).ToString("dd/MM/yyyy h:mm tt") + " " + UTCTZD + " - " + filter.beforedate.AddMinutes(TimeZoneOffset).ToString("dd/MM/yyyy h:mm tt") + " " + UTCTZD;
                    FormatHeader(wsPivot7.Cells[2, 1, 2, 8], 4);

                    wsPivot7.Cells[3, 1, 3, 8].Merge = true;
                    wsPivot7.Cells[3, 1, 3, 8].Value = "If you are unable to see the Pivot tables below, please click \"Enable Editing\" on the bar above to view them.";
                    FormatHeader(wsPivot7.Cells[3, 1, 3, 8], 4);
                    wsPivot7.Cells[3, 1, 3, 8].Style.Font.Italic = true;
                    wsPivot7.Cells[3, 1, 3, 8].Style.Font.Color.SetColor(System.Drawing.Color.FromArgb(255, 0, 0));
                    wsPivot7.Cells[3, 1, 3, 8].Style.Font.Bold = false;
                    wsPivot7.Cells[3, 1, 3, 8].Style.Font.Size = 8;

                    //dataRange.AutoFitColumns();
                    pivotTable = wsPivot7.PivotTables.Add(wsPivot7.Cells["A7"], dataRange, "AnsweredBySequence");

                    pivotTable.ConfigurePivot("Questionnaire", "Message Sequence", "Message Sequence");

                    List<string> UniqueMessageSequence = dt.AsEnumerable().Select(x => x["Message Sequence"]?.ToString())?.Distinct()?.ToList();

                    wsPivot7 = DoDefaultCopy(wsPivot7, "sent sequence");
                }
                catch (Exception ex)
                {
                    log.logMessage += $"Error generating the excel sheet with Message Sequence metrics {ex.Message}    {ex.StackTrace}";
                }

                #endregion

                #region Pivot 8

                try
                {
                    //pivot 8

                    if (LocationQuestion != null)
                    {
                        var wsPivot6 = package.Workbook.Worksheets.Add("Split by Location");

                        wsPivot6.Cells[1, 1, 1, 8].Merge = true;
                        wsPivot6.Cells[1, 1, 1, 8].Value = "Split by Location" + " Performance Report";
                        wsPivot6.Cells[1, 1, 1, 8].Style.Font.Bold = true;
                        FormatHeader(wsPivot6.Cells[1, 1, 1, 8], 2);

                        wsPivot6.Cells[2, 1, 2, 8].Merge = true;
                        wsPivot6.Cells[2, 1, 2, 8].Value = "Date Range: " + filter.afterdate.AddMinutes(TimeZoneOffset).ToString("dd/MM/yyyy h:mm tt") + " " + UTCTZD + " - " + filter.beforedate.AddMinutes(TimeZoneOffset).ToString("dd/MM/yyyy h:mm tt") + " " + UTCTZD;
                        FormatHeader(wsPivot6.Cells[2, 1, 2, 8], 4);

                        wsPivot6.Cells[3, 1, 3, 8].Merge = true;
                        wsPivot6.Cells[3, 1, 3, 8].Value = "If you are unable to see the Pivot tables below, please click \"Enable Editing\" on the bar above to view them.";
                        FormatHeader(wsPivot6.Cells[3, 1, 3, 8], 4);
                        wsPivot6.Cells[3, 1, 3, 8].Style.Font.Italic = true;
                        wsPivot6.Cells[3, 1, 3, 8].Style.Font.Color.SetColor(System.Drawing.Color.FromArgb(255, 0, 0));
                        wsPivot6.Cells[3, 1, 3, 8].Style.Font.Bold = false;
                        wsPivot6.Cells[3, 1, 3, 8].Style.Font.Size = 8;

                        //dataRange.AutoFitColumns();
                        pivotTable = wsPivot6.PivotTables.Add(wsPivot6.Cells["A7"], dataRange, "AnsweredByLocation");

                        pivotTable.ConfigurePivot("Questionnaire", LocationQuestion.Text, LocationQuestion.Text);

                        wsPivot6 = DoDefaultCopy(wsPivot6, "location");
                    }
                }
                catch (Exception ex)
                {
                    log.logMessage += $"Error generating the excel sheet with location metrics {ex.Message}    {ex.StackTrace}";
                }

                #endregion

                #region Pivot 9

                try
                {
                    //pivot 9

                    if (TouchPointQuestion != null)
                    {
                        var wsPivot6 = package.Workbook.Worksheets.Add("Split by TouchPoint");

                        wsPivot6.Cells[1, 1, 1, 8].Merge = true;
                        wsPivot6.Cells[1, 1, 1, 8].Value = "Split by TouchPoint" + " Performance Report";
                        wsPivot6.Cells[1, 1, 1, 8].Style.Font.Bold = true;
                        FormatHeader(wsPivot6.Cells[1, 1, 1, 8], 2);

                        wsPivot6.Cells[2, 1, 2, 8].Merge = true;
                        wsPivot6.Cells[2, 1, 2, 8].Value = "Date Range: " + filter.afterdate.AddMinutes(TimeZoneOffset).ToString("dd/MM/yyyy h:mm tt") + " " + UTCTZD + " - " + filter.beforedate.AddMinutes(TimeZoneOffset).ToString("dd/MM/yyyy h:mm tt") + " " + UTCTZD;
                        FormatHeader(wsPivot6.Cells[2, 1, 2, 8], 4);

                        wsPivot6.Cells[3, 1, 3, 8].Merge = true;
                        wsPivot6.Cells[3, 1, 3, 8].Value = "If you are unable to see the Pivot tables below, please click \"Enable Editing\" on the bar above to view them.";
                        FormatHeader(wsPivot6.Cells[3, 1, 3, 8], 4);
                        wsPivot6.Cells[3, 1, 3, 8].Style.Font.Italic = true;
                        wsPivot6.Cells[3, 1, 3, 8].Style.Font.Color.SetColor(System.Drawing.Color.FromArgb(255, 0, 0));
                        wsPivot6.Cells[3, 1, 3, 8].Style.Font.Bold = false;
                        wsPivot6.Cells[3, 1, 3, 8].Style.Font.Size = 8;

                        //dataRange.AutoFitColumns();
                        pivotTable = wsPivot6.PivotTables.Add(wsPivot6.Cells["A7"], dataRange, "AnsweredByTouchPoint");

                        pivotTable.ConfigurePivot("Questionnaire", TouchPointQuestion.Text, TouchPointQuestion.Text);

                        wsPivot6 = DoDefaultCopy(wsPivot6, "touch point");
                    }
                }
                catch (Exception ex)
                {
                    log.logMessage += $"Error generating the excel sheet with touchpoint metrics {ex.Message}    {ex.StackTrace}";
                }

                #endregion

                #region Pivot 6

                try
                {
                    //pivot 6

                    if (ZoneQuestion != null)
                    {
                        var wsPivot6 = package.Workbook.Worksheets.Add("Split by Zone");

                        wsPivot6.Cells[1, 1, 1, 8].Merge = true;
                        wsPivot6.Cells[1, 1, 1, 8].Value = "Split by Zone" + " Performance Report";
                        wsPivot6.Cells[1, 1, 1, 8].Style.Font.Bold = true;
                        FormatHeader(wsPivot6.Cells[1, 1, 1, 8], 2);

                        wsPivot6.Cells[2, 1, 2, 8].Merge = true;
                        wsPivot6.Cells[2, 1, 2, 8].Value = "Date Range: " + filter.afterdate.AddMinutes(TimeZoneOffset).ToString("dd/MM/yyyy h:mm tt") + " " + UTCTZD + " - " + filter.beforedate.AddMinutes(TimeZoneOffset).ToString("dd/MM/yyyy h:mm tt") + " " + UTCTZD;
                        FormatHeader(wsPivot6.Cells[2, 1, 2, 8], 4);

                        wsPivot6.Cells[3, 1, 3, 8].Merge = true;
                        wsPivot6.Cells[3, 1, 3, 8].Value = "If you are unable to see the Pivot tables below, please click \"Enable Editing\" on the bar above to view them.";
                        FormatHeader(wsPivot6.Cells[3, 1, 3, 8], 4);
                        wsPivot6.Cells[3, 1, 3, 8].Style.Font.Italic = true;
                        wsPivot6.Cells[3, 1, 3, 8].Style.Font.Color.SetColor(System.Drawing.Color.FromArgb(255, 0, 0));
                        wsPivot6.Cells[3, 1, 3, 8].Style.Font.Bold = false;
                        wsPivot6.Cells[3, 1, 3, 8].Style.Font.Size = 8;

                        //dataRange.AutoFitColumns();
                        pivotTable = wsPivot6.PivotTables.Add(wsPivot6.Cells["A7"], dataRange, "AnsweredByZone");

                        pivotTable.ConfigurePivot("Questionnaire", ZoneQuestion.Text, ZoneQuestion.Text);

                        wsPivot6 = DoDefaultCopy(wsPivot6, "zone");
                    }
                }
                catch (Exception ex)
                {
                    log.logMessage += $"Error generating the excel sheet with Zone metrics {ex.Message}    {ex.StackTrace}";
                }

                #endregion

                package.Workbook.Worksheets.MoveToEnd("Raw Data All");

                return new Tuple<byte[], bool>(package.GetAsByteArray(), true);
            }
            catch(Exception ex)
            {
                log.logMessage += $"Error generating the excel excel report {ex.Message}    {ex.StackTrace}";
                return null;
            }
        }

        void FormatHeader(ExcelRange range, int type = 1)
        {
            var color_yellow = System.Drawing.Color.FromArgb(255, 242, 204);
            var color_lightBlue = System.Drawing.Color.FromArgb(79, 129, 189);
            var color_lightGray = System.Drawing.Color.FromArgb(242, 242, 242);
            var color_darkgrey = System.Drawing.Color.FromArgb(128, 128, 128);

            switch (type)
            {
                case 1: //sheet format
                    range.AutoFitColumns(10, 50);
                    range.Style.Font.Bold = true;
                    break;
                case 2: //sheet header, and date range
                    range.Style.Font.Size = 18;
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    range.Style.Fill.BackgroundColor.SetColor(color_yellow);
                    break;
                case 3: //table header
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Font.Color.SetColor(System.Drawing.Color.FromArgb(255, 255, 255));
                    range.Style.Fill.BackgroundColor.SetColor(color_lightBlue);
                    break;
                case 4: //date range
                    range.Style.Font.Size = 12;
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    range.Style.Fill.BackgroundColor.SetColor(color_yellow);
                    break;
                case 5:
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(color_darkgrey);
                    break;
                default:
                    break;
            }

        }

        public static string AddOrdinal(int num)
        {
            if (num <= 0) return num.ToString();

            switch (num % 100)
            {
                case 11:
                case 12:
                case 13:
                    return num + "th";
            }

            switch (num % 10)
            {
                case 1:
                    return num + "st";
                case 2:
                    return num + "nd";
                case 3:
                    return num + "rd";
                default:
                    return num + "th";
            }
        }
    }
}
