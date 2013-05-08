﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Renci.SshNet.Sftp.Messages;

namespace Renci.SshNet.Sftp
{
    internal class SetStatusCommand : SftpCommand
    {
        private string _path;
        private byte[] _handle;
        private SftpFileAttributes _attributes;

        public SetStatusCommand(SftpSession sftpSession, string path, SftpFileAttributes attributes)
            : base(sftpSession)
        {
            this._path = path;
            this._attributes = attributes;
        }

        public SetStatusCommand(SftpSession sftpSession, byte[] handle, SftpFileAttributes attributes)
            : base(sftpSession)
        {
            this._handle = handle;
            this._attributes = attributes;
        }

        protected override void OnExecute()
        {
            if (this._handle != null)
            {
                this.SendSetStatMessage(this._handle, this._attributes);
            }
            else if (this._path != null)
            {
                this.SendSetStatMessage(this._path, this._attributes);
            }
        }

        protected override void OnStatus(StatusCodes statusCode, string errorMessage, string language)
        {
            base.OnStatus(statusCode, errorMessage, language);

            if (statusCode == StatusCodes.Ok)
            {
                this.CompleteExecution();
            }
        }
   }
}
