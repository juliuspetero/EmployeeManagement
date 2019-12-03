using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Options;
using MimeKit;
using MailKit.Net.Smtp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EmployeeManagement.Utilities
{
    // For dependency injection
    public interface IEmailSender
    {
        Task SendEmailAsync(string email, string subject, string message);
    }

    public class EmailSender : IEmailSender
    {
        private readonly EmailSettings _emailSettings;
        private readonly IHostingEnvironment _env;

        public EmailSender(
            IOptions<EmailSettings> emailSettings,
            IHostingEnvironment env)
        {
            _emailSettings = emailSettings.Value;
            _env = env;
        }

        public async Task SendEmailAsync(string email, string subject, string message)
        {
            try
            {
                MimeMessage mimeMessage = new MimeMessage();

                // Add the sender of the email, this information is fixed
                mimeMessage.From.Add(new MailboxAddress(_emailSettings.SenderName, _emailSettings.Sender));

                // This varies depending on the situation
                mimeMessage.To.Add(new MailboxAddress(email));

                mimeMessage.Subject = subject;

                //mimeMessage.Body = new TextPart("html")
                //{
                //    // Populate with the message the sender needs
                //    Text = message
                //};

                BodyBuilder bodyBuilder = new BodyBuilder();
                bodyBuilder.HtmlBody = $"<h3>Please click this to confirm your                              email</h3>" +   
                               $"<small>{message}</small>";
                //bodyBuilder.TextBody = message;

                // You can also add attachments
                //bodyBuilder.Attachments.Add(_env.WebRootPath + "\\file.png");

                //Once the BodyBuilder is ready we can generate a MimeMessage body from it as follows:
                mimeMessage.Body = bodyBuilder.ToMessageBody();

                using (SmtpClient smtpClient = new SmtpClient())
                {
                    // For demo-purposes, accept all SSL certificates (in case the server supports STARTTLS)
                    smtpClient.ServerCertificateValidationCallback = (s, c, h, e) => true;

                    if (_env.IsDevelopment())
                    {
                        // The third parameter is useSSL (true if the client should make an SSL-wrapped
                        // connection to the server; otherwise, false).
                        await smtpClient.ConnectAsync(_emailSettings.MailServer, _emailSettings.MailPort, MailKit.Security.SecureSocketOptions.Auto);
                    }
                    else
                    {
                        await smtpClient.ConnectAsync(_emailSettings.MailServer);
                    }

                    // Note: only needed if the SMTP server requires authentication
                    await smtpClient.AuthenticateAsync(_emailSettings.Sender, _emailSettings.Password);

                    await smtpClient.SendAsync(mimeMessage);

                    await smtpClient.DisconnectAsync(true);
                    smtpClient.Dispose();
                }

            }
            catch (Exception ex)
            {
                // TODO: handle exception
                throw new InvalidOperationException(ex.Message);
            }
        }

    }
}
