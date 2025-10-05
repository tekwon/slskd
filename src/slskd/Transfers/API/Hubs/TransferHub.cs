// <copyright file="TransferHub.cs" company="slskd Team">
//     Copyright (c) slskd Team. All rights reserved.
//
//     This program is free software: you can redistribute it and/or modify
//     it under the terms of the GNU Affero General Public License as published
//     by the Free Software Foundation, either version 3 of the License, or
//     (at your option) any later version.
//
//     This program is distributed in the hope that it will be useful,
//     but WITHOUT ANY WARRANTY; without even the implied warranty of
//     MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//     GNU Affero General Public License for more details.
//
//     You should have received a copy of the GNU Affero General Public License
//     along with this program.  If not, see https://www.gnu.org/licenses/.
// </copyright>

namespace slskd.Transfers.API
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.SignalR;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;

    public static class TransferHubMethods
    {
        public static readonly string List = "LIST";
        public static readonly string Update = "UPDATE";
        public static readonly string Create = "CREATE";
    }

    /// <summary>
    ///     Extension methods for the transfer SignalR hub.
    /// </summary>
    public static class TransferHubExtensions
    {
        /// <summary>
        ///     Broadcast an update for a transfer.
        /// </summary>
        /// <param name="hub">The hub.</param>
        /// <param name="transfer">The transfer to broadcast.</param>
        /// <returns>The operation context.</returns>
        public static Task BroadcastUpdateAsync(this IHubContext<TransferHub> hub, Transfer transfer)
        {
            return hub.Clients.All.SendAsync(TransferHubMethods.Update, transfer);
        }

        /// <summary>
        ///     Broadcast the creation of a new transfer.
        /// </summary>
        /// <param name="hub">The hub.</param>
        /// <param name="transfer">The transfer to broadcast.</param>
        /// <returns>The operation context.</returns>
        public static Task BroadcastCreateAsync(this IHubContext<TransferHub> hub, Transfer transfer)
        {
            return hub.Clients.All.SendAsync(TransferHubMethods.Create, transfer);
        }
    }

    /// <summary>
    ///     The transfer SignalR hub.
    /// </summary>
    [Authorize(Policy = AuthPolicy.Any)]
    public class TransferHub : Hub
    {
        public TransferHub(ITransferService transferService, TransferEventHandler eventHandler, ILogger<TransferHub> logger)
        {
            Transfers = transferService;
            EventHandler = eventHandler; // Forces DI to create it
            Logger = logger;

            Logger.LogInformation("TransferHub created with TransferEventHandler");
        }

        private ITransferService Transfers { get; }
        private TransferEventHandler EventHandler { get; }
        private ILogger<TransferHub> Logger { get; }

        public override async Task OnConnectedAsync()
        {
            Logger.LogInformation("Client connected to TransferHub");
            var downloads = Transfers.Downloads.List();
            await Clients.Caller.SendAsync(TransferHubMethods.List, downloads);
        }
    }
}
