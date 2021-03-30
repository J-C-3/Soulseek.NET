﻿// <copyright file="DistributedMessageHandler.cs" company="JP Dillingham">
//     Copyright (c) JP Dillingham. All rights reserved.
//
//     This program is free software: you can redistribute it and/or modify
//     it under the terms of the GNU General Public License as published by
//     the Free Software Foundation, either version 3 of the License, or
//     (at your option) any later version.
//
//     This program is distributed in the hope that it will be useful,
//     but WITHOUT ANY WARRANTY; without even the implied warranty of
//     MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//     GNU General Public License for more details.
//
//     You should have received a copy of the GNU General Public License
//     along with this program.  If not, see https://www.gnu.org/licenses/.
// </copyright>

namespace Soulseek.Messaging.Handlers
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Soulseek.Diagnostics;
    using Soulseek.Messaging.Messages;
    using Soulseek.Network;

    /// <summary>
    ///     Handles incoming messages from distributed connections.
    /// </summary>
    internal sealed class DistributedMessageHandler : IDistributedMessageHandler
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="DistributedMessageHandler"/> class.
        /// </summary>
        /// <param name="soulseekClient">The ISoulseekClient instance to use.</param>
        /// <param name="diagnosticFactory">The IDiagnosticFactory instance to use.</param>
        public DistributedMessageHandler(
            SoulseekClient soulseekClient,
            IDiagnosticFactory diagnosticFactory = null)
        {
            SoulseekClient = soulseekClient ?? throw new ArgumentNullException(nameof(soulseekClient));
            Diagnostic = diagnosticFactory ??
                new DiagnosticFactory(SoulseekClient.Options.MinimumDiagnosticLevel, (e) => DiagnosticGenerated?.Invoke(this, e));
        }

        /// <summary>
        ///     Occurs when an internal diagnostic message is generated.
        /// </summary>
        public event EventHandler<DiagnosticEventArgs> DiagnosticGenerated;

        private string DeduplicationHash { get; set; }
        private IDiagnosticFactory Diagnostic { get; }
        private SoulseekClient SoulseekClient { get; }

        /// <summary>
        ///     Handles incoming messages from distributed children.
        /// </summary>
        /// <param name="sender">The child <see cref="IMessageConnection"/> from which the message originated.</param>
        /// <param name="args">The message event args.</param>
        public void HandleChildMessageRead(object sender, MessageEventArgs args)
        {
            HandleChildMessageRead(sender, args.Message);
        }

        /// <summary>
        ///     Handles incoming messages from distributed children.
        /// </summary>
        /// <param name="sender">The child <see cref="IMessageConnection"/> from which the message originated.</param>
        /// <param name="message">The message.</param>
        public async void HandleChildMessageRead(object sender, byte[] message)
        {
            var connection = (IMessageConnection)sender;
            var code = new MessageReader<MessageCode.Distributed>(message).ReadCode();

            Diagnostic.Debug($"Distributed child message received: {code} from {connection.Username} ({connection.IPEndPoint}) (id: {connection.Id})");

            try
            {
                switch (code)
                {
                    case MessageCode.Distributed.Ping:
                        Diagnostic.Debug("PING?");
                        var pingResponse = new DistributedPingResponse(SoulseekClient.GetNextToken());
                        await connection.WriteAsync(pingResponse).ConfigureAwait(false);
                        Diagnostic.Debug("PONG!");
                        break;

                    default:
                        Diagnostic.Debug($"Unhandled distributed child message: {code} from {connection.Username} ({connection.IPEndPoint}); {message.Length} bytes");
                        break;
                }
            }
            catch (Exception ex)
            {
                Diagnostic.Warning($"Error handling distributed child message: {code} from {connection.Username} ({connection.IPEndPoint}); {ex.Message}", ex);
            }
        }

        /// <summary>
        ///     Handles outgoing messages to distributed children, post send.
        /// </summary>
        /// <param name="sender">The child <see cref="IMessageConnection"/> instance to which the message was sent.</param>
        /// <param name="args">The message event args.</param>
        public void HandleChildMessageWritten(object sender, MessageEventArgs args)
        {
            var connection = (IMessageConnection)sender;
            var code = new MessageReader<MessageCode.Distributed>(args.Message).ReadCode();
            Diagnostic.Debug($"Distributed child message sent: {code} to {connection.Username} ({connection.IPEndPoint}) (id: {connection.Id})");
        }

        /// <summary>
        ///     Handles incoming messages.
        /// </summary>
        /// <param name="sender">The <see cref="IMessageConnection"/> instance from which the message originated.</param>
        /// <param name="args">The message event args.</param>
        public void HandleMessageRead(object sender, MessageEventArgs args)
        {
            HandleMessageRead(sender, args.Message);
        }

        /// <summary>
        ///     Handles incoming messages.
        /// </summary>
        /// <param name="sender">The <see cref="IMessageConnection"/> instance from which the message originated.</param>
        /// <param name="message">The message.</param>
        public async void HandleMessageRead(object sender, byte[] message)
        {
            var connection = (IMessageConnection)sender;
            var code = new MessageReader<MessageCode.Distributed>(message).ReadCode();

            if (code != MessageCode.Distributed.SearchRequest && code != MessageCode.Distributed.EmbeddedMessage)
            {
                Diagnostic.Debug($"Distributed message received: {code} from {connection.Username} ({connection.IPEndPoint}) (id: {connection.Id})");
            }
            else if (SoulseekClient.Options.DeduplicateSearchRequests)
            {
                var current = Convert.ToBase64String(message);

                if (DeduplicationHash == current)
                {
                    return;
                }

                DeduplicationHash = current;
            }

            try
            {
                switch (code)
                {
                    // if we are connected to a branch root, we get search requests with code EmbeddedMessage.
                    // convert this message to a normal DistributedSearchRequest before forwarding.
                    case MessageCode.Distributed.EmbeddedMessage:
                        var embeddedMessage = EmbeddedMessage.FromByteArray(message);

                        switch (embeddedMessage.DistributedCode)
                        {
                            case MessageCode.Distributed.SearchRequest:
                                var embeddedSearchRequest = DistributedSearchRequest.FromByteArray(embeddedMessage.DistributedMessage);

                                _ = SoulseekClient.DistributedConnectionManager.BroadcastMessageAsync(embeddedMessage.DistributedMessage).ConfigureAwait(false);

                                await TrySendSearchResults(embeddedSearchRequest.Username, embeddedSearchRequest.Token, embeddedSearchRequest.Query).ConfigureAwait(false);

                                break;
                            default:
                                Diagnostic.Debug($"Unhandled embedded message: {code} from {connection.Username} ({connection.IPEndPoint}); {message.Length} bytes");
                                break;
                        }

                        break;

                    // if we are connected to anyone other than a branch root, we should get search requests with code
                    // SearchRequest. forward these requests as is.
                    case MessageCode.Distributed.SearchRequest:
                        var searchRequest = DistributedSearchRequest.FromByteArray(message);

                        _ = SoulseekClient.DistributedConnectionManager.BroadcastMessageAsync(searchRequest.ToByteArray()).ConfigureAwait(false);

                        await TrySendSearchResults(searchRequest.Username, searchRequest.Token, searchRequest.Query).ConfigureAwait(false);

                        break;

                    case MessageCode.Distributed.Ping:
                        var pingResponse = DistributedPingResponse.FromByteArray(message);
                        SoulseekClient.Waiter.Complete(new WaitKey(MessageCode.Distributed.Ping, connection.Username), pingResponse);

                        break;

                    case MessageCode.Distributed.BranchLevel:
                        var branchLevel = DistributedBranchLevel.FromByteArray(message);

                        if ((connection.Username, connection.IPEndPoint) == SoulseekClient.DistributedConnectionManager.Parent)
                        {
                            SoulseekClient.DistributedConnectionManager.SetBranchLevel(branchLevel.Level);
                        }

                        break;

                    case MessageCode.Distributed.BranchRoot:
                        var branchRoot = DistributedBranchRoot.FromByteArray(message);

                        if ((connection.Username, connection.IPEndPoint) == SoulseekClient.DistributedConnectionManager.Parent)
                        {
                            SoulseekClient.DistributedConnectionManager.SetBranchRoot(branchRoot.Username);
                        }

                        break;

                    case MessageCode.Distributed.ChildDepth:
                        var childDepth = DistributedChildDepth.FromByteArray(message);
                        SoulseekClient.Waiter.Complete(new WaitKey(Constants.WaitKey.ChildDepthMessage, connection.Key), childDepth.Depth);
                        break;

                    default:
                        Diagnostic.Debug($"Unhandled distributed message: {code} from {connection.Username} ({connection.IPEndPoint}); {message.Length} bytes");
                        break;
                }
            }
            catch (Exception ex)
            {
                Diagnostic.Warning($"Error handling distributed message: {code} from {connection.Username} ({connection.IPEndPoint}); {ex.Message}", ex);
            }
        }

        /// <summary>
        ///     Handles outgoing messages, post send.
        /// </summary>
        /// <param name="sender">The <see cref="IMessageConnection"/> instance to which the message was sent.</param>
        /// <param name="args">The message event args.</param>
        public void HandleMessageWritten(object sender, MessageEventArgs args)
        {
            var code = new MessageReader<MessageCode.Distributed>(args.Message).ReadCode();
            Diagnostic.Debug($"Distributed message sent: {code}");
        }

        /// <summary>
        ///     Handles embedded messages from the server.
        /// </summary>
        /// <param name="message">The message.</param>
        public async void HandleEmbeddedMessage(byte[] message)
        {
            MessageCode.Distributed code = default;

            try
            {
                var embeddedMessage = EmbeddedMessage.FromByteArray(message);
                code = embeddedMessage.DistributedCode;
                var distributedMessage = embeddedMessage.DistributedMessage;

                switch (code)
                {
                    case MessageCode.Distributed.SearchRequest:
                        var searchRequest = DistributedSearchRequest.FromByteArray(distributedMessage);
                        Console.Write($"Distributed search: {searchRequest.Query}");

                        _ = SoulseekClient.DistributedConnectionManager.BroadcastMessageAsync(message).ConfigureAwait(false);

                        await TrySendSearchResults(searchRequest.Username, searchRequest.Token, searchRequest.Query).ConfigureAwait(false);

                        break;
                    default:
                        Diagnostic.Debug($"Unhandled embedded message: {code}");
                        break;
                }
            }
            catch (Exception ex)
            {
                Diagnostic.Warning($"Error handling embedded message: {code}; {ex.Message}", ex);
            }
        }

        private async Task<bool> TrySendSearchResults(string username, int token, string query)
        {
            if (SoulseekClient.Options.SearchResponseResolver == default)
            {
                return false;
            }

            SearchResponse searchResponse = null;

            try
            {
                searchResponse = await SoulseekClient.Options.SearchResponseResolver(username, token, SearchQuery.FromText(query)).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Diagnostic.Warning($"Error resolving search response for query '{query}' requested by {username} with token {token}: {ex.Message}", ex);
                return false;
            }

            if (searchResponse == null)
            {
                return false;
            }

            if (searchResponse.FileCount <= 0)
            {
                return false;
            }

            try
            {
                Diagnostic.Debug($"Resolved {searchResponse.FileCount} files for query '{query}' with token {token} from {username}");

                var endpoint = await SoulseekClient.GetUserEndPointAsync(username).ConfigureAwait(false);

                var peerConnection = await SoulseekClient.PeerConnectionManager.GetOrAddMessageConnectionAsync(username, endpoint, CancellationToken.None).ConfigureAwait(false);
                await peerConnection.WriteAsync(searchResponse.ToByteArray()).ConfigureAwait(false);

                Diagnostic.Debug($"Sent response containing {searchResponse.FileCount} files to {username} for query '{query}' with token {token}");

                return true;
            }
            catch (Exception ex)
            {
                Diagnostic.Debug($"Failed to send search response for {query} to {username}: {ex.Message}", ex);
            }

            return false;
        }
    }
}