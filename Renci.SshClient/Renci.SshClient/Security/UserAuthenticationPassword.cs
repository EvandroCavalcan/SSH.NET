﻿using System;
using System.Threading;
using Renci.SshClient.Messages;
using Renci.SshClient.Messages.Authentication;

namespace Renci.SshClient.Security
{
    internal class UserAuthenticationPassword : UserAuthentication, IDisposable
    {
        private EventWaitHandle _authenticationCompleted = new AutoResetEvent(false);

        public override string Name
        {
            get
            {
                return "password";
            }
        }

        protected override bool Run()
        {
            //  TODO:   Handle all user authentication messages
            //Message.RegisterMessageType<PasswordChangeRequiredMessage>(MessageTypes.UserAuthenticationPasswordChangeRequired);

            if (string.IsNullOrEmpty(this.Session.ConnectionInfo.Password))
                return false;

            this.Session.SendMessage(new PasswordRequestMessage
                {
                    ServiceName = ServiceNames.Connection,
                    Username = this.Session.ConnectionInfo.Username,
                    Password = this.Session.ConnectionInfo.Password,
                });

            this.Session.WaitHandle(this._authenticationCompleted);

            return true;
        }

        protected override void Session_UserAuthenticationSuccessMessageReceived(object sender, MessageEventArgs<SuccessMessage> e)
        {
            base.Session_UserAuthenticationSuccessMessageReceived(sender, e);
            this._authenticationCompleted.Set();
        }

        protected override void Session_UserAuthenticationFailureReceived(object sender, MessageEventArgs<FailureMessage> e)
        {
            base.Session_UserAuthenticationFailureReceived(sender, e);
            this._authenticationCompleted.Set();
        }

        #region IDisposable Members

        private bool isDisposed = false;

        public void Dispose()
        {
            Dispose(true);

            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            // Check to see if Dispose has already been called.
            if (!this.isDisposed)
            {
                // If disposing equals true, dispose all managed
                // and unmanaged resources.
                if (disposing)
                {
                    // Dispose managed resources.
                    if (this._authenticationCompleted != null)
                    {
                        this._authenticationCompleted.Dispose();
                    }
                }

                // Note disposing has been done.
                isDisposed = true;
            }
        }

        ~UserAuthenticationPassword()
        {
            // Do not re-create Dispose clean-up code here.
            // Calling Dispose(false) is optimal in terms of
            // readability and maintainability.
            Dispose(false);
        }

        #endregion
    }
}
