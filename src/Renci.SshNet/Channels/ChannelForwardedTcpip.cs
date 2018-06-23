﻿using System;
using System.Net;
using System.Net.Sockets;
using Renci.SshNet.Abstractions;
using Renci.SshNet.Common;
using Renci.SshNet.Messages.Connection;

namespace Renci.SshNet.Channels
{
    /// <summary>
    /// Implements "forwarded-tcpip" SSH channel.
    /// </summary>
    internal class ChannelForwardedTcpip : ServerChannel, IChannelForwardedTcpip
    {
        private readonly object _socketShutdownAndCloseLock = new object();
        private Socket _socket;
        private IForwardedPort _forwardedPort;

        /// <summary>
        /// Initializes a new <see cref="ChannelForwardedTcpip"/> instance.
        /// </summary>
        /// <param name="session">The session.</param>
        /// <param name="localChannelNumber">The local channel number.</param>
        /// <param name="localWindowSize">Size of the window.</param>
        /// <param name="localPacketSize">Size of the packet.</param>
        /// <param name="remoteChannelNumber">The remote channel number.</param>
        /// <param name="remoteWindowSize">The window size of the remote party.</param>
        /// <param name="remotePacketSize">The maximum size of a data packet that we can send to the remote party.</param>
        internal ChannelForwardedTcpip(ISession session, uint localChannelNumber, uint localWindowSize, uint localPacketSize, uint remoteChannelNumber, uint remoteWindowSize, uint remotePacketSize)
            : base(session, localChannelNumber, localWindowSize, localPacketSize, remoteChannelNumber, remoteWindowSize, remotePacketSize)
        {
        }

        /// <summary>
        /// Gets the type of the channel.
        /// </summary>
        /// <value>
        /// The type of the channel.
        /// </value>
        public override ChannelTypes ChannelType
        {
            get { return ChannelTypes.ForwardedTcpip; }
        }

        /// <summary>
        /// Binds the channel to the specified endpoint.
        /// </summary>
        /// <param name="remoteEndpoint">The endpoint to connect to.</param>
        /// <param name="forwardedPort">The forwarded port for which the channel is opened.</param>
        public void Bind(IPEndPoint remoteEndpoint, IForwardedPort forwardedPort)
        {
            if (!IsConnected)
            {
                throw new SshException("Session is not connected.");
            }

            _forwardedPort = forwardedPort;
            _forwardedPort.Closing += ForwardedPort_Closing;

            //  Try to connect to the socket 
            try
            {
                _socket = SocketAbstraction.Connect(remoteEndpoint, ConnectionInfo.Timeout);

                // send channel open confirmation message
                SendMessage(new ChannelOpenConfirmationMessage(RemoteChannelNumber, LocalWindowSize, LocalPacketSize, LocalChannelNumber));
            }
            catch (Exception exp)
            {
                // send channel open failure message
                SendMessage(new ChannelOpenFailureMessage(RemoteChannelNumber, exp.ToString(), ChannelOpenFailureMessage.ConnectFailed, "en"));

                throw;
            }

            var buffer = new byte[RemotePacketSize];

            SocketAbstraction.ReadContinuous(_socket, buffer, 0, buffer.Length, SendData);
        }

        protected override void OnErrorOccured(Exception exp)
        {
            base.OnErrorOccured(exp);

            // signal to the server that we will not send anything anymore; this will also interrupt the
            // blocking receive in Bind if the server sends FIN/ACK in time
            //
            // if the FIN/ACK is not sent in time, the socket will be closed in Close(bool)
            ShutdownSocket(SocketShutdown.Send);
        }

        /// <summary>
        /// Occurs as the forwarded port is being stopped.
        /// </summary>
        private void ForwardedPort_Closing(object sender, EventArgs eventArgs)
        {
#if DEBUG_GERT
            Console.WriteLine("ID: " + Thread.CurrentThread.ManagedThreadId + " | ChannelForwardedTcpip.ForwardedPort_Closing");
#endif // DEBUG_GERT

            // signal to the server that we will not send anything anymore; this will also interrupt the
            // blocking receive in Bind if the server sends FIN/ACK in time
            //
            // if the FIN/ACK is not sent in time, the socket will be closed in Close(bool)
            ShutdownSocket(SocketShutdown.Send);
        }

        /// <summary>
        /// Shuts down the socket.
        /// </summary>
        /// <param name="how">One of the <see cref="SocketShutdown"/> values that specifies the operation that will no longer be allowed.</param>
        private void ShutdownSocket(SocketShutdown how)
        {
#if DEBUG_GERT
            Console.WriteLine("ID: " + Thread.CurrentThread.ManagedThreadId + " | ChannelForwardedTcpip.ShutdownSocket");
#endif // DEBUG_GERT

            if (_socket == null || !_socket.Connected)
                return;

            lock (_socketShutdownAndCloseLock)
            {
                var socket = _socket;
                if (socket == null || !socket.Connected)
                    return;

                socket.Shutdown(how);
            }
        }

        /// <summary>
        /// Closes the socket, hereby interrupting the blocking receive in <see cref="Bind(IPEndPoint,IForwardedPort)"/>.
        /// </summary>
        private void CloseSocket()
        {
            if (_socket == null)
                return;

            lock (_socketShutdownAndCloseLock)
            {
#if DEBUG_GERT
                Console.WriteLine("ID: " + Thread.CurrentThread.ManagedThreadId + " | ChannelForwardedTcpip.CloseSocket");
#endif // DEBUG_GERT

                var socket = _socket;
                if (socket != null)
                {
                    // closing a socket actually disposes the socket, so we can safely dereference
                    // the field to avoid entering the lock again later
                    socket.Dispose();
                    _socket = null;
                }
            }
        }

        /// <summary>
        /// Closes the channel, optionally waiting for the SSH_MSG_CHANNEL_CLOSE message to
        /// be received from the server.
        /// </summary>
        /// <param name="wait"><c>true</c> to wait for the SSH_MSG_CHANNEL_CLOSE message to be received from the server; otherwise, <c>false</c>.</param>
        protected override void Close(bool wait)
        {
#if DEBUG_GERT
            Console.WriteLine("ID: " + Thread.CurrentThread.ManagedThreadId + " | ChannelForwardedTcpip.Close");
#endif // DEBUG_GERT

            var forwardedPort = _forwardedPort;
            if (forwardedPort != null)
            {
                forwardedPort.Closing -= ForwardedPort_Closing;
                _forwardedPort = null;
            }

            // signal to the server that we will not send anything anymore; this will also interrupt the
            // blocking receive in Bind if the server sends FIN/ACK in time
            //
            // if the FIN/ACK is not sent in time, the socket will be closed after the channel is closed
            ShutdownSocket(SocketShutdown.Send);

            // close the SSH channel, and mark the channel closed
            base.Close(wait);

            // close the socket
            CloseSocket();
        }

#if DEBUG_GERT
        protected override void OnClose()
        {
            base.OnClose();

            Console.WriteLine("ID: " + Thread.CurrentThread.ManagedThreadId + " | OnClose");
        }
#endif // DEBUG_GERT

        /// <summary>
        /// Called when channel data is received.
        /// </summary>
        /// <param name="data">The data.</param>
        protected override void OnData(byte[] data)
        {
            base.OnData(data);

            var socket = _socket;
            if (socket != null && socket.Connected)
            {
                SocketAbstraction.Send(socket, data, 0, data.Length);
            }
        }
    }
}
