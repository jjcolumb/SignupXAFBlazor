using System;
using System.Net;
using System.Net.Mail;
using System.Security.Cryptography;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.DC;
using DevExpress.Data.Filtering;
using DevExpress.Persistent.Base;
using DevExpress.ExpressApp.Model;
using DevExpress.Persistent.Validation;
using DevExpress.Persistent.Base.Security;
using DevExpress.ExpressApp.Updating;
using DevExpress.Persistent.BaseImpl.PermissionPolicy;
using System.ComponentModel;
using SignupXAF.Module.BusinessObjects;
using DevExpress.ExpressApp.Security;

namespace SignupXAF.Module.Controllers.Security
{
    [DomainComponent]
    public class MessageParameters
    {
        [ModelDefault("AllowEdit", "False")]
        public string Message { get; set; }
    }


    [DomainComponent]
    public abstract class LogonActionParametersBase
    {
        public const string EmailPattern = @"^[_a-z0-9-]+(\.[_a-z0-9-]+)*@[a-z0-9-]+(\.[a-z0-9-]+)*(\.[a-z]{2,4})$";
        public const string ValidationContext = "RegisterUserContext";

        [RuleRequiredField(null, ValidationContext)]
        [RuleRegularExpression(null, ValidationContext, EmailPattern, "Must be a valid Email")]
        public string Email { get; set; }

        public abstract void ExecuteBusinessLogic(IObjectSpace objectSpace);
    }

    [DomainComponent]
    [ModelDefault("Caption", "Register User")]
    [ImageName("BO_User")]
    public class RegisterUserParameters : LogonActionParametersBase
    {
        [RuleRequiredField(null, ValidationContext)]
        public string UserName { get; set; }
        [ModelDefault("IsPassword", "True")]
        public string Password { get; set; }       

        [Browsable(false)]
        public bool UserFound { get; set; }
        public override void ExecuteBusinessLogic(IObjectSpace objectSpace)
        {
            IAuthenticationStandardUser user = objectSpace.FindObject<ApplicationUser>(new BinaryOperator("UserName", UserName));
            if (user != null)
            {
                UserFound = true;
                return;
                //throw new ArgumentException("The login with the entered UserName or Email was already registered within the system");
            }
            else
            {

                if (user == null)
                {                   

                    user = CreateUser(objectSpace, UserName, Email, Password, false);

                  //  EmailAccountInformation(Email);
                }
            }



        }

        public IAuthenticationStandardUser CreateUser(IObjectSpace objectSpace, string userName, string email, string password, bool isAdministrator)
        {
            if (string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(email))
            {
                throw new ArgumentException("UserName and Email address are not specified!");
            }
            ApplicationUser user = objectSpace.FindObject<ApplicationUser>(new BinaryOperator("UserName", userName));
            if (user == null)
            {
                user = objectSpace.CreateObject<ApplicationUser>();
                user.UserName = userName;
                user.IsActive = true;
                user.EmailAddress = Email;
                user.SetPassword(password);
                PermissionPolicyRole role = objectSpace.FirstOrDefault<PermissionPolicyRole>(r => r.Name == "Default"); 
                user.Roles.Add(role);
                user.Save();
                if (Validator.RuleSet.ValidateTarget(objectSpace, user, DefaultContexts.Save).State == ValidationState.Valid)
                {
                    // The UserLoginInfo object requires a user object Id (Oid).
                    // Commit the user object to the database before you create a UserLoginInfo object. This will correctly initialize the user key property.
                    objectSpace.CommitChanges();
                    ((ISecurityUserWithLoginInfo)user).CreateUserLoginInfo(SecurityDefaults.PasswordAuthentication, objectSpace.GetKeyValueAsString(user));
                    objectSpace.CommitChanges();
                }
            }
            return user;
        }

        public static void EmailAccountInformation(string emailAddress)
        {
            var fromAddress = "noreply@gmail.com";
            var toAddress = emailAddress;
            string fromPassword = "";

            string subject = "Welcome to our APP!";
            string body = "Welcome to our App!" + Environment.NewLine + Environment.NewLine + "Your new account has been created successfully. Click below to really access your profile" + Environment.NewLine + Environment.NewLine + "Link to your app" + Environment.NewLine + Environment.NewLine + "This is an automated response acknowledging your request. Please do not reply to this e-mail.";
            var smtp = new SmtpClient
            {
                Host = "smtp.gmail.com",
                Port = 587,
                EnableSsl = true,

                DeliveryMethod = SmtpDeliveryMethod.Network,
                Credentials = new NetworkCredential(fromAddress, fromPassword),
                Timeout = 20000
            };
            using (var message = new MailMessage(fromAddress, toAddress)
            {
                Subject = subject,
                Body = body ?? " "
            })
            {
                try
                {
                    message.IsBodyHtml = false;
                    smtp.Send(message);
                }
                catch (Exception ex)
                {

                }
            }
        }
    }


    [DomainComponent]
    [ModelDefault("Caption", "Restore Password")]
    [ImageName("Action_ResetPassword")]
    public class RestorePasswordParameters : LogonActionParametersBase
    {
        [Browsable(false)]
        public bool UserNotFound { get; set; }
        public override void ExecuteBusinessLogic(IObjectSpace objectSpace)
        {

            var tempuser = objectSpace.FindObject(typeof(ApplicationUser), CriteriaOperator.Parse("EmailAddress = ?", Email));

            var user = tempuser as IAuthenticationStandardUser;

            if (user == null)
            {
                UserNotFound = true;
                return;

                // throw new ArgumentException("Cannot find registered users by the provided email address!");
            }

           
            byte[] randomBytes = new byte[6];
            new RNGCryptoServiceProvider().GetBytes(randomBytes);
            string password = Convert.ToBase64String(randomBytes);

            user.SetPassword(password);
            user.ChangePasswordOnFirstLogon = true;
            objectSpace.CommitChanges();
           // EmailLoginInformation(Email, password, user.UserName);
        }
        public static void EmailLoginInformation(string emailAddress, string pwd, string username)
        {
            var fromAddress = "noreply@gmail.com";
            var toAddress = emailAddress;
            string fromPassword = "";

            string subject = "Password reset request";
            string body = "Hello " + username + "," + Environment.NewLine + Environment.NewLine + "A password reset request was made for your user. Here is your new Password " + pwd + "." + Environment.NewLine + Environment.NewLine + "Please use the link provided below to log in and you will be prompted to create a new password." + Environment.NewLine + Environment.NewLine + "Link to your app" + Environment.NewLine + Environment.NewLine + "This is an automated response acknowledging your request. Please do not reply to this e-mail.";
            var smtp = new SmtpClient
            {
                Host = "smtp.gmail.com",
                Port = 587,
                EnableSsl = true,

                DeliveryMethod = SmtpDeliveryMethod.Network,
                Credentials = new NetworkCredential(fromAddress, fromPassword),
                Timeout = 20000
            };
            using (var message = new MailMessage(fromAddress, toAddress)
            {
                Subject = subject,
                Body = body ?? " "
            })
            {
                try
                {
                    message.IsBodyHtml = false;
                    smtp.Send(message);
                }
                catch (Exception ex)
                {

                }
            }
        }

    }
}
