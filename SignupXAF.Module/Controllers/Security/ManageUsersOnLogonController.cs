using System;
using DevExpress.ExpressApp;
using DevExpress.Persistent.Base;
using DevExpress.ExpressApp.Utils;
using DevExpress.ExpressApp.Actions;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.Security;
using DevExpress.ExpressApp.Templates;
using DevExpress.ExpressApp.Validation;
using DevExpress.ExpressApp.SystemModule;
using DevExpress.Persistent.Validation;
using SignupXAF.Module.BusinessObjects;

namespace SignupXAF.Module.Controllers.Security
{
    public class ManageUsersOnLogonController : ViewController<DetailView>
    {
        protected const string LogonActionParametersActiveKey = "Active for ILogonActionParameters only";
        public static LogonController _lc;
        public SimpleAction RegisterUserAction { get; private set; }
        public SimpleAction RestorePasswordAction { get; private set; }
        public SimpleAction AcceptLogonParametersAction { get; private set; }
        public SimpleAction CancelLogonParametersAction { get; private set; }


        public ManageUsersOnLogonController()
        {
            string defaultCategory = PredefinedCategory.PopupActions.ToString();

            RegisterUserAction = CreateLogonParametersAction("RegisterUser", defaultCategory, "Register User", "BO_User", "Register a new user within the system", typeof(RegisterUserParameters));
            RestorePasswordAction = CreateLogonParametersAction("RestorePassword", defaultCategory, "Restore Password", "Action_ResetPassword", "Restore forgotten login information", typeof(RestorePasswordParameters));

            AcceptLogonParametersAction = new SimpleAction(this, "AcceptLogonParameters", defaultCategory, (s, e) =>
            {
                AcceptParameters(e.CurrentObject as LogonActionParametersBase);
            })
            { Caption = "Yes" };
            CancelLogonParametersAction = new SimpleAction(this, "CancelLogonParameters", defaultCategory, (s, e) =>
            {
                CancelParameters(e.CurrentObject as LogonActionParametersBase);
            })
            { Caption = "No" };
        }

        protected override void OnViewChanging(View view)
        {
            base.OnViewChanging(view);
            Active[ControllerActiveKey] = !SecuritySystem.IsAuthenticated;
        }

        protected override void OnViewControlsCreated()
        {
            base.OnViewControlsCreated();

            bool isLogonParametersActionView = View != null && View.ObjectTypeInfo != null && View.ObjectTypeInfo.Implements<LogonActionParametersBase>();


            LogonController lc = Frame.GetController<LogonController>();
            if (lc != null)
            {
                _lc = lc;
            }

            if (lc != null)
            {

                lc.AcceptAction.Active[LogonActionParametersActiveKey] = !isLogonParametersActionView;
                lc.CancelAction.Active[LogonActionParametersActiveKey] = !isLogonParametersActionView;
            }

            AcceptLogonParametersAction.Active[LogonActionParametersActiveKey] = isLogonParametersActionView;
            CancelLogonParametersAction.Active[LogonActionParametersActiveKey] = isLogonParametersActionView;


            DialogController dc = Frame.GetController<DialogController>();
            dc.AcceptAction.Active[LogonActionParametersActiveKey] = !isLogonParametersActionView;
            dc.CancelAction.Active[LogonActionParametersActiveKey] = !isLogonParametersActionView;

            if (View.Id == "RegisterUserParameters_DetailView")
            {
                AcceptLogonParametersAction.Caption = "Register";
                CancelLogonParametersAction.Caption = "Cancel";
            }

            if ((View.Id == "RegisterUserParameters_DetailView" || View.Id == "RestorePasswordParameters_DetailView") && lc == null)
            {
                AcceptLogonParametersAction.Active["ActionPermissionGranted"] = true;
                CancelLogonParametersAction.Active["ActionPermissionGranted"] = true;
            }

            RegisterUserAction.Active[LogonActionParametersActiveKey] = !isLogonParametersActionView;
            RestorePasswordAction.Active[LogonActionParametersActiveKey] = !isLogonParametersActionView;

            if (View.Id == "MessageParameters_DetailView")
            {
                RegisterUserAction.Active[LogonActionParametersActiveKey] = false;
                RestorePasswordAction.Active[LogonActionParametersActiveKey] = false;
                dc.CancelAction.Active[LogonActionParametersActiveKey] = false;
            }
        }

        private SimpleAction CreateLogonParametersAction(string id, string category, string caption, string imageName, string toolTip, Type parametersType)
        {
            SimpleAction action = new SimpleAction(this, id, category);
            action.Caption = caption;
            action.ImageName = imageName;
            action.PaintStyle = ActionItemPaintStyle.Image;
            action.ToolTip = toolTip;
            action.Execute += (s, e) => CreateParametersViewCore(e);
            action.Tag = parametersType;
            return action;
        }

        protected virtual void CreateParametersViewCore(SimpleActionExecuteEventArgs e)
        {

            ValidationModule validationModule = Application.Modules.FindModule<ValidationModule>();
            validationModule.InitializeRuleSet();
            Type parametersType = e.Action.Tag as Type;
            Guard.ArgumentNotNull(parametersType, "parametersType");
            object logonActionParameters = Activator.CreateInstance(parametersType);
            DetailView dv = Application.CreateDetailView(ObjectSpaceInMemory.CreateNew(), logonActionParameters);
            dv.ViewEditMode = ViewEditMode.Edit;
            e.ShowViewParameters.CreatedView = dv;
            e.ShowViewParameters.TargetWindow = TargetWindow.NewModalWindow;

        }
        protected virtual void AcceptParameters(LogonActionParametersBase parameters)
        {
            ResultsHighlightController resultsHighlightController = Frame.GetController<ResultsHighlightController>();
            if (resultsHighlightController != null)
            {
                RuleSetValidationResult result = Validator.RuleSet.ValidateTarget(ObjectSpace, parameters, "RegisterUserContext");
                if (result.ValidationOutcome == ValidationOutcome.Error || result.ValidationOutcome == ValidationOutcome.Warning || result.ValidationOutcome == ValidationOutcome.Information)
                {
                    resultsHighlightController.HighlightResults(result);
                    throw new ValidationException(result);
                }
                else
                {
                    resultsHighlightController.ClearHighlighting();
                }
            }


            Guard.ArgumentNotNull(parameters, "parameters");
            parameters.ExecuteBusinessLogic(Application.CreateObjectSpace(typeof(ApplicationUser)));
            //Some additional UI-level logic is necessary depending on the parameters type.
            RegisterUserParameters registerUserParameters = parameters as RegisterUserParameters;
            RestorePasswordParameters restoreUserParameters = parameters as RestorePasswordParameters;
            if (registerUserParameters != null)
            {
                if (registerUserParameters.UserFound)
                {
                    //DetailView dv = Application.CreateDetailView(ObjectSpaceInMemory.CreateNew(), new MessageParameters { Message = "Cannot find registered users by the provided email address!" });
                    DetailView dv = Application.CreateDetailView(ObjectSpaceInMemory.CreateNew(), new MessageParameters { Message = "The login with the entered UserName or Email was already registered within the system" });
                    var sv = new ShowViewParameters();
                    //sv.Controllers.Add(Application.CreateController<DialogController>());
                    sv.TargetWindow = TargetWindow.NewModalWindow;
                    sv.CreatedView = dv;
                    Application.ShowViewStrategy.ShowView(sv, new ShowViewSource(null, null));
                    return;

                }

                if (_lc == null)
                {
                    _lc = Frame.GetController<LogonController>();
                }

                if (_lc != null)
                {
                    _lc.AcceptAction.Active.RemoveItem(LogonActionParametersActiveKey);
                    AuthenticationStandardLogonParameters securitySystemLogonParameters = (AuthenticationStandardLogonParameters)SecuritySystem.LogonParameters;
                    securitySystemLogonParameters.UserName = registerUserParameters.UserName;
                    securitySystemLogonParameters.Password = registerUserParameters.Password;
                    _lc.AcceptAction.DoExecute();
                }
            }
            else
            {

                if (restoreUserParameters != null && restoreUserParameters.UserNotFound)
                {
                    DetailView dv = Application.CreateDetailView(ObjectSpaceInMemory.CreateNew(), new MessageParameters { Message = "Cannot find registered user by the provided email address!" });
                    //DetailView dv = Application.CreateDetailView(ObjectSpaceInMemory.CreateNew(), new MessageParameters { Message = "The login with the entered UserName or Email was already registered within the system" });
                    var sv = new ShowViewParameters();
                    //sv.Controllers.Add(Application.CreateController<DialogController>());
                    sv.TargetWindow = TargetWindow.NewModalWindow;
                    sv.CreatedView = dv;
                    Application.ShowViewStrategy.ShowView(sv, new ShowViewSource(null, null));
                    return;
                }

                CloseParametersView();
            }

            // Application?.ShowViewStrategy?.ShowMessage(new MessageOptions() { Message = "User registered successfully. Please Log In" });
            CloseParametersView();
        }
        protected virtual void CancelParameters(LogonActionParametersBase parameters)
        {
            CloseParametersView();
        }
        protected virtual void CloseParametersView()
        {
            View?.Close(false);
            Application?.LogOff();

        }


    }
}
