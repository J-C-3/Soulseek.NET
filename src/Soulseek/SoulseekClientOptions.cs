﻿// <copyright file="SoulseekClientOptions.cs" company="JP Dillingham">
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

namespace Soulseek
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Threading.Tasks;
    using Soulseek.Diagnostics;
    using Soulseek.Exceptions;
    using Soulseek.Messaging.Messages;

    /// <summary>
    ///     Options for SoulseekClient.
    /// </summary>
    public class SoulseekClientOptions
    {
        private readonly Func<string, IPEndPoint, Task<IEnumerable<Directory>>> defaultBrowseResponse =
            (u, i) => Task.FromResult(Enumerable.Empty<Directory>());

        private readonly Func<string, IPEndPoint, string, Task> defaultEnqueueDownloadAction =
            (u, i, f) => Task.CompletedTask;

        private readonly Func<string, IPEndPoint, string, Task<int?>> defaultPlaceInQueueResponse =
            (u, i, f) => Task.FromResult<int?>(null);

        private readonly Func<string, IPEndPoint, Task<UserInfo>> defaultUserInfoResponse =
            (u, i) => Task.FromResult(new UserInfo(string.Empty, 0, 0, false));

        /// <summary>
        ///     Initializes a new instance of the <see cref="SoulseekClientOptions"/> class.
        /// </summary>
        /// <param name="listenPort">The port on which to listen for incoming connections.</param>
        /// <param name="enableDistributedNetwork">A value indicating whether to establish distributed network connections.</param>
        /// <param name="acceptDistributedChildren">A value indicating whether to accept distributed child connections.</param>
        /// <param name="distributedChildLimit">The number of allowed distributed children.</param>
        /// <param name="messageTimeout">
        ///     The message timeout, in milliseconds, used when waiting for a response from the server.
        /// </param>
        /// <param name="autoAcknowledgePrivateMessages">
        ///     A value indicating whether to automatically send a private message acknowledgement upon receipt.
        /// </param>
        /// <param name="autoAcknowledgePrivilegeNotifications">
        ///     A value indicating whether to automatically send a privilege notification acknowledgement upon receipt.
        /// </param>
        /// <param name="minimumDiagnosticLevel">The minimum level of diagnostic messages to be generated by the client.</param>
        /// <param name="startingToken">The starting value for download and search tokens.</param>
        /// <param name="serverConnectionOptions">The options for the server message connection.</param>
        /// <param name="peerConnectionOptions">The options for peer message connections.</param>
        /// <param name="transferConnectionOptions">The options for peer transfer connections.</param>
        /// <param name="incomingConnectionOptions">The options for incoming connections.</param>
        /// <param name="distributedConnectionOptions">The options for distributed message connections.</param>
        /// <param name="searchResponseResolver">
        ///     The delegate used to resolve the <see cref="SearchResponse"/> for an incoming <see cref="SearchRequest"/>.
        /// </param>
        /// <param name="browseResponseResolver">
        ///     The delegate used to resolve the <see cref="BrowseResponse"/> for an incoming <see cref="BrowseRequest"/>.
        /// </param>
        /// <param name="directoryContentsResponver">
        ///     The delegate used to resolve the <see cref="FolderContentsResponse"/> for an incoming <see cref="FolderContentsRequest"/>.
        /// </param>
        /// <param name="userInfoResponseResolver">
        ///     The delegate used to resolve the <see cref="UserInfo"/> for an incoming <see cref="UserInfoRequest"/>.
        /// </param>
        /// <param name="enqueueDownloadAction">The delegate invoked upon an receipt of an incoming <see cref="EnqueueDownloadRequest"/>.</param>
        /// <param name="placeInQueueResponseResolver">
        ///     The delegate used to resolve the <see cref="PlaceInQueueResponse"/> for an incoming request.
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException">
        ///     Thrown when the value supplied for <paramref name="distributedChildLimit"/> is less than zero.
        /// </exception>
        public SoulseekClientOptions(
            int? listenPort = null,
            bool enableDistributedNetwork = true,
            bool acceptDistributedChildren = true,
            int distributedChildLimit = 25,
            int messageTimeout = 5000,
            bool autoAcknowledgePrivateMessages = true,
            bool autoAcknowledgePrivilegeNotifications = true,
            DiagnosticLevel minimumDiagnosticLevel = DiagnosticLevel.Info,
            int startingToken = 0,
            ConnectionOptions serverConnectionOptions = null,
            ConnectionOptions peerConnectionOptions = null,
            ConnectionOptions transferConnectionOptions = null,
            ConnectionOptions incomingConnectionOptions = null,
            ConnectionOptions distributedConnectionOptions = null,
            Func<string, int, SearchQuery, Task<SearchResponse>> searchResponseResolver = null,
            Func<string, IPEndPoint, Task<IEnumerable<Directory>>> browseResponseResolver = null,
            Func<string, IPEndPoint, int, string, Task<Directory>> directoryContentsResponver = null,
            Func<string, IPEndPoint, Task<UserInfo>> userInfoResponseResolver = null,
            Func<string, IPEndPoint, string, Task> enqueueDownloadAction = null,
            Func<string, IPEndPoint, string, Task<int?>> placeInQueueResponseResolver = null)
        {
            ListenPort = listenPort;

            EnableDistributedNetwork = enableDistributedNetwork;
            AcceptDistributedChildren = acceptDistributedChildren;
            DistributedChildLimit = distributedChildLimit;

            if (DistributedChildLimit < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(distributedChildLimit), "Must be greater than or equal to zero");
            }

            MessageTimeout = messageTimeout;
            AutoAcknowledgePrivateMessages = autoAcknowledgePrivateMessages;
            AutoAcknowledgePrivilegeNotifications = autoAcknowledgePrivilegeNotifications;
            MinimumDiagnosticLevel = minimumDiagnosticLevel;
            StartingToken = startingToken;

            ServerConnectionOptions = serverConnectionOptions ?? new ConnectionOptions();
            PeerConnectionOptions = peerConnectionOptions ?? new ConnectionOptions();
            TransferConnectionOptions = transferConnectionOptions ?? new ConnectionOptions();
            IncomingConnectionOptions = incomingConnectionOptions ?? new ConnectionOptions();
            DistributedConnectionOptions = distributedConnectionOptions ?? new ConnectionOptions();

            SearchResponseResolver = searchResponseResolver;
            BrowseResponseResolver = browseResponseResolver ?? defaultBrowseResponse;
            DirectoryContentsResolver = directoryContentsResponver;

            UserInfoResponseResolver = userInfoResponseResolver ?? defaultUserInfoResponse;
            EnqueueDownloadAction = enqueueDownloadAction ?? defaultEnqueueDownloadAction;
            PlaceInQueueResponseResolver = placeInQueueResponseResolver ?? defaultPlaceInQueueResponse;
        }

        /// <summary>
        ///     Gets a value indicating whether to accept distributed child connections. (Default = accept).
        /// </summary>
        public bool AcceptDistributedChildren { get; }

        /// <summary>
        ///     Gets a value indicating whether to automatically send a private message acknowledgement upon receipt. (Default = true).
        /// </summary>
        public bool AutoAcknowledgePrivateMessages { get; }

        /// <summary>
        ///     Gets a value indicating whether to automatically send a privilege notification acknowledgement upon receipt.
        ///     (Default = true).
        /// </summary>
        public bool AutoAcknowledgePrivilegeNotifications { get; }

        /// <summary>
        ///     Gets the delegate used to resolve the response for an incoming browse request. (Default = a response with no files
        ///     or directories).
        /// </summary>
        public Func<string, IPEndPoint, Task<IEnumerable<Directory>>> BrowseResponseResolver { get; }

        /// <summary>
        ///     Gets the delegate used to resolve the response for an incoming directory contents request. (Default = a response
        ///     with an empty directory).
        /// </summary>
        public Func<string, IPEndPoint, int, string, Task<Directory>> DirectoryContentsResolver { get; }

        /// <summary>
        ///     Gets the number of allowed distributed children. (Default = 100).
        /// </summary>
        public int DistributedChildLimit { get; }

        /// <summary>
        ///     Gets the options for distributed message connections.
        /// </summary>
        public ConnectionOptions DistributedConnectionOptions { get; }

        /// <summary>
        ///     Gets a value indicating whether to establish distributed network connections. (Default = enabled).
        /// </summary>
        public bool EnableDistributedNetwork { get; }

        /// <summary>
        ///     Gets the delegate invoked upon an receipt of an incoming <see cref="EnqueueDownloadRequest"/>. (Default = do nothing).
        /// </summary>
        /// <remarks>
        ///     This delegate must throw an Exception to indicate a rejected download. If the thrown Exception is of type
        ///     <see cref="DownloadEnqueueException"/> the message will be sent to the client, otherwise a default message will be sent.
        /// </remarks>
        public Func<string, IPEndPoint, string, Task> EnqueueDownloadAction { get; }

        /// <summary>
        ///     Gets the options for incoming connections.
        /// </summary>
        public ConnectionOptions IncomingConnectionOptions { get; }

        /// <summary>
        ///     Gets the port on which to listen for incoming connections. (Default = null; do not listen).
        /// </summary>
        public int? ListenPort { get; }

        /// <summary>
        ///     Gets the message timeout, in milliseconds, used when waiting for a response from the server or peer. (Default = 5000).
        /// </summary>
        public int MessageTimeout { get; }

        /// <summary>
        ///     Gets the minimum level of diagnostic messages to be generated by the client. (Default = None).
        /// </summary>
        public DiagnosticLevel MinimumDiagnosticLevel { get; }

        /// <summary>
        ///     Gets the options for peer message connections.
        /// </summary>
        public ConnectionOptions PeerConnectionOptions { get; }

        /// <summary>
        ///     Gets the delegate used to resolve the <see cref="PlaceInQueueResponse"/> for an incoming request.
        /// </summary>
        public Func<string, IPEndPoint, string, Task<int?>> PlaceInQueueResponseResolver { get; }

        /// <summary>
        ///     Gets the delegate used to resolve the <see cref="SearchResponse"/> for an incoming request. (Default = do not respond).
        /// </summary>
        public Func<string, int, SearchQuery, Task<SearchResponse>> SearchResponseResolver { get; }

        /// <summary>
        ///     Gets the options for the server message connection.
        /// </summary>
        public ConnectionOptions ServerConnectionOptions { get; }

        /// <summary>
        ///     Gets the starting value for download and search tokens. (Default = 0).
        /// </summary>
        public int StartingToken { get; }

        /// <summary>
        ///     Gets the options for peer transfer connections.
        /// </summary>
        public ConnectionOptions TransferConnectionOptions { get; }

        /// <summary>
        ///     Gets the delegate used to resolve the <see cref="UserInfo"/> for an incoming request. (Default = a blank/zeroed response).
        /// </summary>
        public Func<string, IPEndPoint, Task<UserInfo>> UserInfoResponseResolver { get; }
    }
}