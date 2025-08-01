﻿using DevSpaceWeb.Data;
using DevSpaceWeb.Data.Teams;
using DevSpaceWeb.Data.Users;
using MailKit.Net.Smtp;
using MimeKit;

namespace DevSpaceWeb.Services;

public class EmailService
{
    public EmailService(HealthCheckService health)
    {
        HealthService = health;
    }
    public HealthCheckService HealthService;

    private HttpClient? ManagedEmailSystem;
    public async Task<SmtpClient> CreateSmtpAsync()
    {
        SmtpClient Client = new SmtpClient();
        try
        {
            await Client.ConnectAsync(_Data.Config.Email.SmtpHost, _Data.Config.Email.SmtpPort);
            await Client.AuthenticateAsync(_Data.Config.Email.SmtpUser, _Data.Config.Email.SmtpPassword);
        }
        catch
        {
            if (HealthService.EmailDownCount != 3)
                HealthService.EmailDownCount += 1;

            throw;
        }

        if (HealthService.EmailDownCount != 0)
            HealthService.EmailDownCount = 0;

        return Client;

    }

    public bool CanSendEmail()
    {
        if (_Data.Config.Email.Type == ConfigEmailType.None)
            return false;

        if (_Data.Config.Email.Type == ConfigEmailType.FluxpointManaged)
            return !string.IsNullOrEmpty(_Data.Config.Email.ManagedEmailToken);
        else
            return !string.IsNullOrEmpty(_Data.Config.Email.SmtpHost);
    }

    public Task<bool> SendAccountConfirm(AuthUser user, string action)
        => Send(EmailTemplateType.AccountConfirm, user, _Data.Config.Email.GetActiveTemplateOrDefault(EmailTemplateType.AccountConfirm), action: action);

    public Task<bool> SendAccountInvited(AuthUser user, string action)
        => Send(EmailTemplateType.AccountInvited, user, _Data.Config.Email.GetActiveTemplateOrDefault(EmailTemplateType.AccountInvited), action: action);

    public Task<bool> SendAccountDeleted(AuthUser user)
        => Send(EmailTemplateType.AccountDeleted, user, _Data.Config.Email.GetActiveTemplateOrDefault(EmailTemplateType.AccountDeleted));

    public Task<bool> SendPasswordChangeRequest(AuthUser user, string action)
        => Send(EmailTemplateType.AccountPasswordChangeRequest, user, _Data.Config.Email.GetActiveTemplateOrDefault(EmailTemplateType.AccountPasswordChangeRequest), action: action);

    public Task<bool> SendPasswordChanged(AuthUser user, string action)
        => Send(EmailTemplateType.AccountPasswordChanged, user, _Data.Config.Email.GetActiveTemplateOrDefault(EmailTemplateType.AccountPasswordChanged), action: action);

    public Task<bool> Send2FAEnabled(AuthUser user)
        => Send(EmailTemplateType.Account2FAEnabled, user, _Data.Config.Email.GetActiveTemplateOrDefault(EmailTemplateType.Account2FADisabled));

    public Task<bool> Send2FADisabled(AuthUser user)
        => Send(EmailTemplateType.Account2FADisabled, user, _Data.Config.Email.GetActiveTemplateOrDefault(EmailTemplateType.Account2FADisabled));

    public Task<bool> SendAccountEnabled(AuthUser user, string reason)
        => Send(EmailTemplateType.AccountEnabled, user, _Data.Config.Email.GetActiveTemplateOrDefault(EmailTemplateType.AccountEnabled), reason: reason);

    public Task<bool> SendAccountDisabled(AuthUser user, string reason)
        => Send(EmailTemplateType.AccountDisabled, user, _Data.Config.Email.GetActiveTemplateOrDefault(EmailTemplateType.AccountDisabled), reason: reason);

    public Task<bool> SendAccessCode(AuthUser user, string reason, string code)
        => Send(EmailTemplateType.AccessCode, user, _Data.Config.Email.GetActiveTemplateOrDefault(EmailTemplateType.AccessCode), code: code, reason: reason);

    public Task<bool> SendEmailChangeCode(AuthUser user, string other_email, string code)
        => Send(EmailTemplateType.AccountEmailChangeCode, user, _Data.Config.Email.GetActiveTemplateOrDefault(EmailTemplateType.AccountEmailChangeCode), code: code, other_email: other_email);

    public Task<bool> SendEmailChangeRequest(AuthUser user, string action)
        => Send(EmailTemplateType.AccountEmailChangeRequest, user, _Data.Config.Email.GetActiveTemplateOrDefault(EmailTemplateType.AccountEmailChangeRequest), action: action);

    public Task<bool> SendEmailChanged(AuthUser user, string other_email)
        => Send(EmailTemplateType.AccountEmailChanged, user, _Data.Config.Email.GetActiveTemplateOrDefault(EmailTemplateType.AccountEmailChanged), other_email: other_email);

    public Task<bool> SendNewSessionIP(AuthUser user, string ip, string country)
        => Send(EmailTemplateType.NewSessionIp, user, _Data.Config.Email.GetActiveTemplateOrDefault(EmailTemplateType.NewSessionIp), ip: ip, country: country);

    public async Task<bool> Send(EmailTemplateType type, AuthUser user, EmailTemplateData? template, string body = "", string other_email = "", string action = "", string code = "", string reason = "", string ip = "", string country = "", string team_name = "")
    {
        if (Program.IsPreviewMode)
            return true;

        if (_Data.Config.Email.Type == ConfigEmailType.FluxpointManaged)
        {
            if (string.IsNullOrEmpty(_Data.Config.Email.ManagedEmailToken))
                return false;

            ManagedEmailSystem ??= new HttpClient();

            HttpRequestMessage message = new HttpRequestMessage(HttpMethod.Get, "https://devspacesmtp.fluxpoint.dev/send")
            {
                Content = JsonContent.Create(new SendMailJson
                {
                    type = type,
                    name = user.UserName,
                    email = user.Email,
                    email_other = other_email,
                    instance_name = _Data.Config.Instance.Name,
                    instance_icon = _Data.Config.Instance.GetIconOrDefault(true),
                    url = string.IsNullOrEmpty(action) ? _Data.Config.Instance.GetPublicUrl() : action,
                    code = code,
                    ip = ip,
                    country = country,
                    team_name = team_name,
                    reason = reason
                })
            };
            message.Headers.TryAddWithoutValidation("Authorization", _Data.Config.Email.ManagedEmailToken);
            HttpResponseMessage Res = await ManagedEmailSystem.SendAsync(message);

            if (Res.IsSuccessStatusCode)
            {
                if (HealthService.EmailFailCount != 0)
                    HealthService.EmailFailCount = 0;
            }
            else
            {
                if (HealthService.EmailFailCount != 3)
                    HealthService.EmailFailCount += 1;
            }

            return Res.IsSuccessStatusCode;
        }
        else
        {
            if (string.IsNullOrEmpty(_Data.Config.Email.SmtpHost) || _Data.Config.Email.Type == ConfigEmailType.None)
                return false;


            try
            {
                using (SmtpClient smtp = await CreateSmtpAsync())
                {
                    try
                    {
                        if (template == null)
                        {
                            MimeMessage message = new MimeMessage();
                            message.From.Add(new MailboxAddress(_Data.Config.Instance.Name, _Data.Config.Email.SenderEmailAddress));
                            message.To.Add(new MailboxAddress(user.UserName, user.Email));
                            message.Subject = "Test Email";
                            message.Body = new TextPart("html")
                            {
                                Text = body
                            };

                            await smtp.SendAsync(message);
                        }
                        else
                            await SendTemplate(smtp, user, template, other_email, action, code, reason, ip, country, team_name);

                        await smtp.DisconnectAsync(true);

                        if (HealthService.EmailFailCount != 0)
                            HealthService.EmailFailCount = 0;

                    }
                    catch
                    {
                        if (HealthService.EmailFailCount != 3)
                            HealthService.EmailFailCount += 1;

                        return false;
                    }


                }
                return true;
            }
            catch (Exception ex)
            {
                Logger.LogMessage(ex.ToString(), LogSeverity.Error);
                return false;
            }
        }

    }

    private async Task SendTemplate(SmtpClient client, AuthUser user, EmailTemplateData template, string other_email, string action = "", string code = "", string reason = "", string ip = "", string country = "", string team_name = "")
    {
        MimeMessage message = new MimeMessage();
        message.From.Add(new MailboxAddress(_Data.Config.Instance.Name, _Data.Config.Email.SmtpUser));
        message.To.Add(new MailboxAddress(user.UserName, user.Email));
        message.Subject = $"{template.GetTypeName()} - {user.UserName} | {_Data.Config.Instance.Name}";

        string Body = template.ParseHtml(user, action, other_email, code, reason, ip, country);

        message.Body = new TextPart("html")
        {
            Text = Body
        };

        await client.SendAsync(message);
    }

    public string RandomCodeGenerator()
    {
        string[] page = new string[6];
        Random rnd = new Random();
        for (int i = 0; i < page.Length; ++i)
        {
            int Number = rnd.Next(10);
            page[i] = Number.ToString();
            Task.Delay(TimeSpan.FromMilliseconds(Number));
        }

        return string.Join("", page);
    }


    public class SendMailJson
    {
        public string? name { get; set; }
        public string? email { get; set; }
        public string? email_other { get; set; }
        public string? code { get; set; }
        public string? instance_name { get; set; }
        public string? instance_icon { get; set; }
        public string? team_name { get; set; }
        public string? reason { get; set; }
        public string? url { get; set; }
        public string? ip { get; set; }
        public string? country { get; set; }
        public EmailTemplateType type { get; set; }
    }
}
