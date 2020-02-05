﻿// <copyright file="ListenerHandler.cs" company="JP Dillingham">
//     Copyright (c) JP Dillingham. All rights reserved.
//
//     This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License
//     as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
//
//     This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty
//     of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.See the GNU General Public License for more details.
//
//     You should have received a copy of the GNU General Public License along with this program. If not, see https://www.gnu.org/licenses/.
// </copyright>

namespace Soulseek.Network
{
    using System;
    using System.Linq;
    using Soulseek.Diagnostics;
    using Soulseek.Exceptions;
    using Soulseek.Messaging.Messages;
    using Soulseek.Network.Tcp;

    /// <summary>
    ///     Handles incoming connections established by the <see cref="IListener"/>.
    /// </summary>
    internal sealed class ListenerHandler : IListenerHandler
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="ListenerHandler"/> class.
        /// </summary>
        /// <param name="soulseekClient">The ISoulseekClient instance to use.</param>
        /// <param name="diagnosticFactory">The IDiagnosticFactory instance to use.</param>
        public ListenerHandler(
            SoulseekClient soulseekClient,
            IDiagnosticFactory diagnosticFactory = null)
        {
            SoulseekClient = soulseekClient ?? throw new ArgumentNullException(nameof(soulseekClient));
            Diagnostic = diagnosticFactory ??
                new DiagnosticFactory(this, SoulseekClient?.Options?.MinimumDiagnosticLevel ?? new SoulseekClientOptions().MinimumDiagnosticLevel, (e) => DiagnosticGenerated?.Invoke(this, e));
        }

        /// <summary>
        ///     Occurs when an internal diagnostic message is generated.
        /// </summary>
        public event EventHandler<DiagnosticEventArgs> DiagnosticGenerated;

        private IDiagnosticFactory Diagnostic { get; }
        private SoulseekClient SoulseekClient { get; }

        /// <summary>
        ///     Handle <see cref="IListener.Accepted"/> events.
        /// </summary>
        /// <param name="sender">The originating <see cref="IListener"/> instance.</param>
        /// <param name="connection">The accepted connection.</param>
        public async void HandleConnection(object sender, IConnection connection)
        {
            Diagnostic.Debug($"Accepted incoming connection from {connection.IPEndPoint.Address}:{SoulseekClient.Listener.Port} (id: {connection.Id})");

            try
            {
                var lengthBytes = await connection.ReadAsync(4).ConfigureAwait(false);
                var length = BitConverter.ToInt32(lengthBytes, 0);

                var bodyBytes = await connection.ReadAsync(length).ConfigureAwait(false);
                byte[] message = lengthBytes.Concat(bodyBytes).ToArray();

                if (PeerInit.TryFromByteArray(message, out var peerInit))
                {
                    // this connection is the result of an unsolicited connection from the remote peer, either to request info or
                    // browse, or to send a file.
                    Diagnostic.Debug($"PeerInit for connection type {peerInit.ConnectionType} received from {peerInit.Username} ({connection.IPEndPoint.Address}:{SoulseekClient.Listener.Port}) (id: {connection.Id})");

                    if (peerInit.ConnectionType == Constants.ConnectionType.Peer)
                    {
                        Diagnostic.Debug($"Handing incoming message connection to {peerInit.Username} off (id: {connection.Id})");
                        await SoulseekClient.PeerConnectionManager.AddMessageConnectionAsync(
                            peerInit.Username,
                            connection.HandoffTcpClient()).ConfigureAwait(false);
                    }
                    else if (peerInit.ConnectionType == Constants.ConnectionType.Transfer)
                    {
                        Diagnostic.Debug($"Handing incoming transfer connection to {peerInit.Username} with token {peerInit.Token} off (id: {connection.Id})");
                        await SoulseekClient.PeerConnectionManager.AddTransferConnectionAsync(
                            peerInit.Username,
                            peerInit.Token,
                            connection.HandoffTcpClient()).ConfigureAwait(false);
                    }
                    else if (peerInit.ConnectionType == Constants.ConnectionType.Distributed)
                    {
                        Diagnostic.Debug($"Handing incoming distributed child connection to {peerInit.Username} off (id: {connection.Id})");
                        await SoulseekClient.DistributedConnectionManager.AddChildConnectionAsync(
                            peerInit.Username,
                            connection.HandoffTcpClient()).ConfigureAwait(false);
                    }
                }
                else if (PierceFirewall.TryFromByteArray(message, out var pierceFirewall))
                {
                    // this connection is the result of a ConnectToPeer request sent to the user, and the incoming message will
                    // contain the token that was provided in the request. Ensure this token is among those expected, and use it
                    // to determine the username of the remote user.
                    if (SoulseekClient.PeerConnectionManager.PendingSolicitations.TryGetValue(pierceFirewall.Token, out var peerUsername))
                    {
                        Diagnostic.Debug($"Peer PierceFirewall with token {pierceFirewall.Token} received from {peerUsername} ({connection.IPEndPoint.Address}:{SoulseekClient.Listener.Port}) (id: {connection.Id})");
                        SoulseekClient.Waiter.Complete(new WaitKey(Constants.WaitKey.SolicitedPeerConnection, peerUsername, pierceFirewall.Token), connection);
                    }
                    else if (SoulseekClient.DistributedConnectionManager.PendingSolicitations.TryGetValue(pierceFirewall.Token, out var distributedUsername))
                    {
                        Diagnostic.Debug($"Distributed PierceFirewall with token {pierceFirewall.Token} received from {distributedUsername} ({connection.IPEndPoint.Address}:{SoulseekClient.Listener.Port}) (id: {connection.Id})");
                        SoulseekClient.Waiter.Complete(new WaitKey(Constants.WaitKey.SolicitedDistributedConnection, distributedUsername, pierceFirewall.Token), connection);
                    }
                    else
                    {
                        throw new ConnectionException($"Unknown PierceFirewall attempt with token {pierceFirewall.Token} from {connection.IPEndPoint.Address}:{connection.IPEndPoint.Port} (id: {connection.Id})");
                    }
                }
                else
                {
                    throw new ConnectionException($"Unrecognized initialization message: {BitConverter.ToString(message)} ({message.Length} bytes, id: {connection.Id})");
                }
            }
            catch (Exception ex)
            {
                Diagnostic.Debug($"Failed to initialize direct connection from {connection.IPEndPoint.Address}:{connection.IPEndPoint.Port}: {ex.Message} (id: {connection.Id})");
                connection.Disconnect(exception: ex);
                connection.Dispose();
            }
        }
    }
}