﻿// <copyright file="SoulseekClientOptions.cs" company="JP Dillingham">
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

namespace Soulseek
{
    using System;
    using System.Linq;
    using System.Net;
    using System.Threading.Tasks;
    using Soulseek.Diagnostics;
    using Soulseek.Messaging.Messages;

    /// <summary>
    ///     Options for SoulseekClient.
    /// </summary>
    public class SoulseekClientOptions
    {
        private readonly Func<string, IPEndPoint, Task<BrowseResponse>> defaultBrowseResponse =
            (u, i) => Task.FromResult(new BrowseResponse(Enumerable.Empty<Directory>()));

        private readonly Func<string, IPEndPoint, string, Task> defaultEnqueueDownloadAction =
            (u, i, f) => Task.CompletedTask;

        private readonly Func<string, IPEndPoint, string, Task<int?>> defaultPlaceInQueueResponse =
            (u, i, f) => Task.FromResult<int?>(null);

        private readonly Func<string, IPEndPoint, Task<UserInfo>> defaultUserInfoResponse =
            (u, i) => Task.FromResult(new UserInfo(string.Empty, 0, 0, false));

        /// <summary>
        ///     Initializes a new instance of the <see cref="SoulseekClientOptions"/> class.
        /// </summary>
        /// <param name="enableListener">A value indicating whether to listen for incoming connections.</param>
        /// <param name="listenPort">The port on which to listen for incoming connections.</param>
        /// <param name="enableDistributedNetwork">A value indicating whether to establish distributed network connections.</param>
        /// <param name="acceptDistributedChildren">A value indicating whether to accept distributed child connections.</param>
        /// <param name="distributedChildLimit">The number of allowed distributed children.</param>
        /// <param name="maximumConcurrentUploads">The number of allowed concurrent uploads.</param>
        /// <param name="deduplicateSearchRequests">
        ///     A value indicating whether duplicated distributed search requests should be discarded.
        /// </param>
        /// <param name="messageTimeout">
        ///     The message timeout, in milliseconds, used when waiting for a response from the server.
        /// </param>
        /// <param name="autoAcknowledgePrivateMessages">
        ///     A value indicating whether to automatically send a private message acknowledgement upon receipt.
        /// </param>
        /// <param name="autoAcknowledgePrivilegeNotifications">
        ///     A value indicating whether to automatically send a privilege notification acknowledgement upon receipt.
        /// </param>
        /// <param name="acceptPrivateRoomInvitations">A value indicating whether to accept private room invitations.</param>
        /// <param name="minimumDiagnosticLevel">The minimum level of diagnostic messages to be generated by the client.</param>
        /// <param name="startingToken">The starting value for download and search tokens.</param>
        /// <param name="serverConnectionOptions">The options for the server message connection.</param>
        /// <param name="peerConnectionOptions">The options for peer message connections.</param>
        /// <param name="transferConnectionOptions">The options for peer transfer connections.</param>
        /// <param name="incomingConnectionOptions">The options for incoming connections.</param>
        /// <param name="distributedConnectionOptions">The options for distributed message connections.</param>
        /// <param name="userEndPointCache">The user endpoint cache to use when resolving user endpoints.</param>
        /// <param name="searchResponseResolver">
        ///     The delegate used to resolve the <see cref="SearchResponse"/> for an incoming <see cref="SearchRequest"/>.
        /// </param>
        /// <param name="searchResponseCache">
        ///     The search response cache to use when a response is not able to be delivered immediately.
        /// </param>
        /// <param name="browseResponseResolver">
        ///     The delegate used to resolve the <see cref="BrowseResponse"/> for an incoming <see cref="BrowseRequest"/>.
        /// </param>
        /// <param name="directoryContentsResponseResolver">
        ///     The delegate used to resolve the <see cref="FolderContentsResponse"/> for an incoming <see cref="FolderContentsRequest"/>.
        /// </param>
        /// <param name="userInfoResponseResolver">
        ///     The delegate used to resolve the <see cref="UserInfo"/> for an incoming <see cref="UserInfoRequest"/>.
        /// </param>
        /// <param name="enqueueDownloadAction">The delegate invoked upon an receipt of an incoming <see cref="QueueDownloadRequest"/>.</param>
        /// <param name="placeInQueueResponseResolver">
        ///     The delegate used to resolve the <see cref="PlaceInQueueResponse"/> for an incoming request.
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException">
        ///     Thrown when the value supplied for <paramref name="listenPort"/> is not between 1024 and 65535.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///     Thrown when the value supplied for <paramref name="distributedChildLimit"/> is less than zero.
        /// </exception>
        public SoulseekClientOptions(
            bool enableListener = true,
            int listenPort = 50000,
            bool enableDistributedNetwork = true,
            bool acceptDistributedChildren = true,
            int distributedChildLimit = 25,
            int maximumConcurrentUploads = 10,
            bool deduplicateSearchRequests = true,
            int messageTimeout = 5000,
            bool autoAcknowledgePrivateMessages = true,
            bool autoAcknowledgePrivilegeNotifications = true,
            bool acceptPrivateRoomInvitations = false,
            DiagnosticLevel minimumDiagnosticLevel = DiagnosticLevel.Info,
            int startingToken = 0,
            ConnectionOptions serverConnectionOptions = null,
            ConnectionOptions peerConnectionOptions = null,
            ConnectionOptions transferConnectionOptions = null,
            ConnectionOptions incomingConnectionOptions = null,
            ConnectionOptions distributedConnectionOptions = null,
            IUserEndPointCache userEndPointCache = null,
            Func<string, int, SearchQuery, Task<SearchResponse>> searchResponseResolver = null,
            ISearchResponseCache searchResponseCache = null,
            Func<string, IPEndPoint, Task<BrowseResponse>> browseResponseResolver = null,
            Func<string, IPEndPoint, int, string, Task<Directory>> directoryContentsResponseResolver = null,
            Func<string, IPEndPoint, Task<UserInfo>> userInfoResponseResolver = null,
            Func<string, IPEndPoint, string, Task> enqueueDownloadAction = null,
            Func<string, IPEndPoint, string, Task<int?>> placeInQueueResponseResolver = null)
        {
            EnableListener = enableListener;
            ListenPort = listenPort;

            if (ListenPort < 1024 || ListenPort > IPEndPoint.MaxPort)
            {
                throw new ArgumentOutOfRangeException(nameof(listenPort), $"Must be between 1024 and {IPEndPoint.MaxPort}");
            }

            EnableDistributedNetwork = enableDistributedNetwork;
            AcceptDistributedChildren = acceptDistributedChildren;
            DistributedChildLimit = distributedChildLimit;

            if (DistributedChildLimit < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(distributedChildLimit), "Must be greater than or equal to zero");
            }

            MaximumConcurrentUploads = maximumConcurrentUploads;

            if (MaximumConcurrentUploads < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(maximumConcurrentUploads), "Must be greater than or equal to one");
            }

            DeduplicateSearchRequests = deduplicateSearchRequests;

            MessageTimeout = messageTimeout;
            AutoAcknowledgePrivateMessages = autoAcknowledgePrivateMessages;
            AutoAcknowledgePrivilegeNotifications = autoAcknowledgePrivilegeNotifications;
            AcceptPrivateRoomInvitations = acceptPrivateRoomInvitations;
            MinimumDiagnosticLevel = minimumDiagnosticLevel;
            StartingToken = startingToken;

            ServerConnectionOptions = (serverConnectionOptions ?? new ConnectionOptions()).WithoutInactivityTimeout();
            PeerConnectionOptions = peerConnectionOptions ?? new ConnectionOptions();
            TransferConnectionOptions = (transferConnectionOptions ?? new ConnectionOptions()).WithoutInactivityTimeout();
            IncomingConnectionOptions = incomingConnectionOptions ?? new ConnectionOptions();
            DistributedConnectionOptions = distributedConnectionOptions ?? new ConnectionOptions();

            UserEndPointCache = userEndPointCache;

            SearchResponseResolver = searchResponseResolver;
            SearchResponseCache = searchResponseCache;

            BrowseResponseResolver = browseResponseResolver ?? defaultBrowseResponse;
            DirectoryContentsResponseResolver = directoryContentsResponseResolver;

            UserInfoResponseResolver = userInfoResponseResolver ?? defaultUserInfoResponse;
            EnqueueDownloadAction = enqueueDownloadAction ?? defaultEnqueueDownloadAction;
            PlaceInQueueResponseResolver = placeInQueueResponseResolver ?? defaultPlaceInQueueResponse;
        }

        /// <summary>
        ///     Gets a value indicating whether to accept distributed child connections. (Default = accept).
        /// </summary>
        public bool AcceptDistributedChildren { get; }

        /// <summary>
        ///     Gets a value indicating whether to accept private room invitations. (Default = false).
        /// </summary>
        public bool AcceptPrivateRoomInvitations { get; }

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
        public Func<string, IPEndPoint, Task<BrowseResponse>> BrowseResponseResolver { get; }

        /// <summary>
        ///     Gets a value indicating whether duplicated distributed search requests should be discarded. (Default = discard duplicates).
        /// </summary>
        public bool DeduplicateSearchRequests { get; }

        /// <summary>
        ///     Gets the delegate used to resolve the response for an incoming directory contents request. (Default = a response
        ///     with an empty directory).
        /// </summary>
        public Func<string, IPEndPoint, int, string, Task<Directory>> DirectoryContentsResponseResolver { get; }

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
        ///     Gets a value indicating whether to listen for incoming connections. (Default = true).
        /// </summary>
        public bool EnableListener { get; }

        /// <summary>
        ///     Gets the delegate invoked upon an receipt of an incoming <see cref="QueueDownloadRequest"/>. (Default = do nothing).
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
        ///     Gets the port on which to listen for incoming connections. (Default = 50000).
        /// </summary>
        public int ListenPort { get; }

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
        ///     Gets the search response cache to use when a response is not able to be delivered immediately.
        /// </summary>
        public ISearchResponseCache SearchResponseCache { get; }

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
        ///     Gets the number of allowed concurrent uploads. (Default = 5).
        /// </summary>
        public int MaximumConcurrentUploads { get; }

        /// <summary>
        ///     Gets the number of upload slots per user.
        /// </summary>
        /// <remarks>
        ///     This can be set with reflection for experimentation.  It needs to remain 1 in production
        ///     to avoid causing problems with Soulseek NS.
        /// </remarks>
        public int MaximumConcurrentUploadsPerUser { get; private set; } = 1;

        /// <summary>
        ///     Gets the user endpoint cache to use when resolving user endpoints.
        /// </summary>
        public IUserEndPointCache UserEndPointCache { get; }

        /// <summary>
        ///     Gets the delegate used to resolve the <see cref="UserInfo"/> for an incoming request. (Default = a blank/zeroed response).
        /// </summary>
        public Func<string, IPEndPoint, Task<UserInfo>> UserInfoResponseResolver { get; }

        /// <summary>
        ///     Creates a clone of this instance with the substitutions in the specified <paramref name="patch"/> applied.
        /// </summary>
        /// <param name="patch">The patch containing the desired substitutions.</param>
        /// <returns>The cloned instance.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the specified <paramref name="patch"/> is null.</exception>
        public SoulseekClientOptions With(SoulseekClientOptionsPatch patch)
        {
            if (patch == null)
            {
                throw new ArgumentNullException(nameof(patch), "Must not be null");
            }

            return With(
                enableListener: patch.EnableListener,
                listenPort: patch.ListenPort,
                enableDistributedNetwork: patch.EnableDistributedNetwork,
                acceptDistributedChildren: patch.AcceptDistributedChildren,
                distributedChildLimit: patch.DistributedChildLimit,
                deduplicateSearchRequests: patch.DeduplicateSearchRequests,
                autoAcknowledgePrivateMessages: patch.AutoAcknowledgePrivateMessages,
                autoAcknowledgePrivilegeNotifications: patch.AutoAcknowledgePrivilegeNotifications,
                acceptPrivateRoomInvitations: patch.AcceptPrivateRoomInvitations,
                serverConnectionOptions: patch.ServerConnectionOptions,
                peerConnectionOptions: patch.PeerConnectionOptions,
                transferConnectionOptions: patch.TransferConnectionOptions,
                incomingConnectionOptions: patch.IncomingConnectionOptions,
                distributedConnectionOptions: patch.DistributedConnectionOptions,
                userEndPointCache: patch.UserEndPointCache,
                searchResponseResolver: patch.SearchResponseResolver,
                searchResponseCache: patch.SearchResponseCache,
                browseResponseResolver: patch.BrowseResponseResolver,
                directoryContentsResponseResolver: patch.DirectoryContentsResponseResolver,
                userInfoResponseResolver: patch.UserInfoResponseResolver,
                enqueueDownloadAction: patch.EnqueueDownloadAction,
                placeInQueueResponseResolver: patch.PlaceInQueueResponseResolver);
        }

        /// <summary>
        ///     Creates a clone of this instance with the specified substitutions.
        /// </summary>
        /// <param name="enableListener">A value indicating whether to listen for incoming connections.</param>
        /// <param name="listenPort">The port on which to listen for incoming connections.</param>
        /// <param name="enableDistributedNetwork">A value indicating whether to establish distributed network connections.</param>
        /// <param name="acceptDistributedChildren">A value indicating whether to accept distributed child connections.</param>
        /// <param name="distributedChildLimit">The number of allowed distributed children.</param>
        /// <param name="deduplicateSearchRequests">
        ///     A value indicating whether duplicated distributed search requests should be discarded.
        /// </param>
        /// <param name="autoAcknowledgePrivateMessages">
        ///     A value indicating whether to automatically send a private message acknowledgement upon receipt.
        /// </param>
        /// <param name="autoAcknowledgePrivilegeNotifications">
        ///     A value indicating whether to automatically send a privilege notification acknowledgement upon receipt.
        /// </param>
        /// <param name="acceptPrivateRoomInvitations">A value indicating whether to accept private room invitations.</param>
        /// <param name="serverConnectionOptions">The options for the server message connection.</param>
        /// <param name="peerConnectionOptions">The options for peer message connections.</param>
        /// <param name="transferConnectionOptions">The options for peer transfer connections.</param>
        /// <param name="incomingConnectionOptions">The options for incoming connections.</param>
        /// <param name="distributedConnectionOptions">The options for distributed message connections.</param>
        /// <param name="userEndPointCache">The user endpoint cache to use when resolving user endpoints.</param>
        /// <param name="searchResponseResolver">
        ///     The delegate used to resolve the <see cref="SearchResponse"/> for an incoming <see cref="SearchRequest"/>.
        /// </param>
        /// <param name="searchResponseCache">
        ///     The search response cache to use when a response is not able to be delivered immediately.
        /// </param>
        /// <param name="browseResponseResolver">
        ///     The delegate used to resolve the <see cref="BrowseResponse"/> for an incoming <see cref="BrowseRequest"/>.
        /// </param>
        /// <param name="directoryContentsResponseResolver">
        ///     The delegate used to resolve the <see cref="FolderContentsResponse"/> for an incoming <see cref="FolderContentsRequest"/>.
        /// </param>
        /// <param name="userInfoResponseResolver">
        ///     The delegate used to resolve the <see cref="UserInfo"/> for an incoming <see cref="UserInfoRequest"/>.
        /// </param>
        /// <param name="enqueueDownloadAction">The delegate invoked upon an receipt of an incoming <see cref="QueueDownloadRequest"/>.</param>
        /// <param name="placeInQueueResponseResolver">
        ///     The delegate used to resolve the <see cref="PlaceInQueueResponse"/> for an incoming request.
        /// </param>
        /// <returns>The cloned instance.</returns>
        internal SoulseekClientOptions With(
            bool? enableListener = null,
            int? listenPort = null,
            bool? enableDistributedNetwork = null,
            bool? acceptDistributedChildren = null,
            int? distributedChildLimit = null,
            bool? deduplicateSearchRequests = null,
            bool? autoAcknowledgePrivateMessages = null,
            bool? autoAcknowledgePrivilegeNotifications = null,
            bool? acceptPrivateRoomInvitations = null,
            ConnectionOptions serverConnectionOptions = null,
            ConnectionOptions peerConnectionOptions = null,
            ConnectionOptions transferConnectionOptions = null,
            ConnectionOptions incomingConnectionOptions = null,
            ConnectionOptions distributedConnectionOptions = null,
            IUserEndPointCache userEndPointCache = null,
            Func<string, int, SearchQuery, Task<SearchResponse>> searchResponseResolver = null,
            ISearchResponseCache searchResponseCache = null,
            Func<string, IPEndPoint, Task<BrowseResponse>> browseResponseResolver = null,
            Func<string, IPEndPoint, int, string, Task<Directory>> directoryContentsResponseResolver = null,
            Func<string, IPEndPoint, Task<UserInfo>> userInfoResponseResolver = null,
            Func<string, IPEndPoint, string, Task> enqueueDownloadAction = null,
            Func<string, IPEndPoint, string, Task<int?>> placeInQueueResponseResolver = null)
        {
            return new SoulseekClientOptions(
                enableListener: enableListener ?? EnableListener,
                listenPort: listenPort ?? ListenPort,
                enableDistributedNetwork: enableDistributedNetwork ?? EnableDistributedNetwork,
                acceptDistributedChildren: acceptDistributedChildren ?? AcceptDistributedChildren,
                distributedChildLimit: distributedChildLimit ?? DistributedChildLimit,
                maximumConcurrentUploads: MaximumConcurrentUploads,
                deduplicateSearchRequests: deduplicateSearchRequests ?? DeduplicateSearchRequests,
                messageTimeout: MessageTimeout,
                autoAcknowledgePrivateMessages: autoAcknowledgePrivateMessages ?? AutoAcknowledgePrivateMessages,
                autoAcknowledgePrivilegeNotifications: autoAcknowledgePrivilegeNotifications ?? AutoAcknowledgePrivilegeNotifications,
                acceptPrivateRoomInvitations: acceptPrivateRoomInvitations ?? AcceptPrivateRoomInvitations,
                minimumDiagnosticLevel: MinimumDiagnosticLevel,
                startingToken: StartingToken,
                serverConnectionOptions: (serverConnectionOptions ?? ServerConnectionOptions).WithoutInactivityTimeout(),
                peerConnectionOptions: peerConnectionOptions ?? PeerConnectionOptions,
                transferConnectionOptions: (transferConnectionOptions ?? TransferConnectionOptions).WithoutInactivityTimeout(),
                incomingConnectionOptions: incomingConnectionOptions ?? IncomingConnectionOptions,
                distributedConnectionOptions: distributedConnectionOptions ?? DistributedConnectionOptions,
                userEndPointCache: userEndPointCache ?? UserEndPointCache,
                searchResponseResolver: searchResponseResolver ?? SearchResponseResolver,
                searchResponseCache: searchResponseCache ?? SearchResponseCache,
                browseResponseResolver: browseResponseResolver ?? BrowseResponseResolver,
                directoryContentsResponseResolver: directoryContentsResponseResolver ?? DirectoryContentsResponseResolver,
                userInfoResponseResolver: userInfoResponseResolver ?? UserInfoResponseResolver,
                enqueueDownloadAction: enqueueDownloadAction ?? EnqueueDownloadAction,
                placeInQueueResponseResolver: placeInQueueResponseResolver ?? PlaceInQueueResponseResolver);
        }
    }
}