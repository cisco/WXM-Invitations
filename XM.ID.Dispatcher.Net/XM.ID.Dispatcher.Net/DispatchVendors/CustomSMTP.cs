using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using System;
using System.Threading.Tasks;

namespace XM.ID.Dispatcher.Net.DispatchVendors
{
    internal class CustomSMTP : ISingleDispatchVendor
    {
        public Vendor Vendor { get; set; }

        public void Setup(Vendor vendor)
        {
            Vendor = vendor;
        }

        public async Task RunAsync(MessagePayload messagePayload)
        {
            try
            {
                Utils.PerformLookUps(messagePayload.AzureQueueData);
                MimeMessage mimeMessage = new MimeMessage();
                BodyBuilder bodyBuilder = new BodyBuilder();
                mimeMessage.From.Add(new MailboxAddress(Vendor.VendorDetails["senderName"], Vendor.VendorDetails["senderAddress"]));
                mimeMessage.To.Add(new MailboxAddress(messagePayload.AzureQueueData.EmailId));
                mimeMessage.Subject = messagePayload.AzureQueueData.Subject;
                bodyBuilder.TextBody = messagePayload.AzureQueueData.TextBody;
                bodyBuilder.HtmlBody = messagePayload.AzureQueueData.HTMLBody;
                mimeMessage.Body = bodyBuilder.ToMessageBody();
                lock (Resources.GetInstance().SmtpLock)
                {
                    using SmtpClient smtpClient = CreateSMTPClient();
                    smtpClient.Send(mimeMessage);
                    smtpClient.Disconnect(true);
                }
                messagePayload.LogEvents.Add(Utils.CreateLogEvent(messagePayload.AzureQueueData, IRDLM.DispatchSuccessful(Vendor.VendorName)));
                messagePayload.InvitationLogEvents.Add(Utils.CreateInvitationLogEvent(EventAction.DispatchSuccessful, EventChannel.Email,
                    messagePayload.AzureQueueData, IRDLM.DispatchSuccessful(Vendor.VendorName)));
            }
            catch (Exception ex)
            {
                messagePayload.LogEvents.Add(Utils.CreateLogEvent(messagePayload.AzureQueueData, IRDLM.DispatchUnsuccessful(Vendor.VendorName, ex)));
                messagePayload.InvitationLogEvents.Add(Utils.CreateInvitationLogEvent(EventAction.DispatchUnsuccessful, EventChannel.Email,
                    messagePayload.AzureQueueData, IRDLM.DispatchUnsuccessful(Vendor.VendorName, ex)));
            }
        }

        private SmtpClient CreateSMTPClient()
        {
            SmtpClient smtpClient = new SmtpClient
            {
                ServerCertificateValidationCallback = (s, c, h, e) => true,
                Timeout = 5 * 60 * 1000    //milli-seconds
            };
            if (Boolean.Parse(Vendor.VendorDetails["ssl"]))
                smtpClient.Connect(Vendor.VendorDetails["smtpServer"], Int32.Parse(Vendor.VendorDetails["port"]), SecureSocketOptions.StartTls);
            else
                smtpClient.Connect(Vendor.VendorDetails["smtpServer"], Int32.Parse(Vendor.VendorDetails["port"]), SecureSocketOptions.None);
            smtpClient.Authenticate(Vendor.VendorDetails["smtpUsername"], Vendor.VendorDetails["smtpPassword"]);
            return smtpClient;
        }
    }
}
