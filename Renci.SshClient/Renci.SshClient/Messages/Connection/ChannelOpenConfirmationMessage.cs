﻿namespace Renci.SshClient.Messages.Connection
{
    public class ChannelOpenConfirmationMessage : ChannelMessage
    {
        public override MessageTypes MessageType
        {
            get { return MessageTypes.ChannelOpenConfirmation; }
        }

        public uint RemoteChannelNumber { get; private set; }

        public uint InitialWindowSize { get; private set; }

        public uint MaximumPacketSize { get; private set; }

        public ChannelOpenConfirmationMessage()
        {

        }

        public ChannelOpenConfirmationMessage(uint localChannelNumber, uint initialWindowSize, uint maximumPacketSize, uint remoteChannelNumber)
        {
            this.LocalChannelNumber = localChannelNumber;
            this.InitialWindowSize = initialWindowSize;
            this.MaximumPacketSize = maximumPacketSize;
            this.RemoteChannelNumber = remoteChannelNumber;
        }

        protected override void LoadData()
        {
            base.LoadData();
            this.RemoteChannelNumber = this.ReadUInt32();
            this.InitialWindowSize = this.ReadUInt32();
            this.MaximumPacketSize = this.ReadUInt32();
        }

        protected override void SaveData()
        {
            base.SaveData();
            this.Write(this.RemoteChannelNumber);
            this.Write(this.InitialWindowSize);
            this.Write(this.MaximumPacketSize);
        }
    }
}
