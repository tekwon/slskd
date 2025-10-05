// <copyright file="TransferEventHandler.cs" company="slskd Team">
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
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.SignalR;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;
    using slskd.Events;

    /// <summary>
    ///     Handles transfer events and broadcasts them to SignalR clients.
    /// </summary>
    public class TransferEventHandler
    {
        public TransferEventHandler(IHubContext<TransferHub> transferHub, EventBus eventBus, ILogger<TransferEventHandler> logger)
        {
            TransferHub = transferHub;
            EventBus = eventBus;
            Logger = logger;

            Logger.LogInformation("TransferEventHandler initialized, subscribing to DownloadFileCompleteEvent");

            // Subscribe to download events
            EventBus.Subscribe<DownloadFileCompleteEvent>("TransferEventHandler", OnDownloadFileComplete);

            Logger.LogInformation("TransferEventHandler subscription completed");
        }

        private IHubContext<TransferHub> TransferHub { get; }
        private EventBus EventBus { get; }
        private ILogger<TransferEventHandler> Logger { get; }

        private async Task OnDownloadFileComplete(DownloadFileCompleteEvent eventData)
        {
            Logger.LogInformation("DownloadFileCompleteEvent received for file: {Filename}", eventData.Transfer.Filename);

            try
            {
                // Convert database Transfer to API Transfer DTO
                var apiTransfer = ConvertToApiTransfer(eventData.Transfer);

                Logger.LogInformation("Broadcasting UPDATE event for: {Filename}", apiTransfer.Filename);
                await TransferHub.Clients.All.SendAsync("UPDATE", apiTransfer);

                Logger.LogInformation("UPDATE event broadcast completed");
            }
            catch (System.Exception ex)
            {
                Logger.LogError(ex, "Error broadcasting transfer completion event");
            }
        }

        private Transfer ConvertToApiTransfer(slskd.Transfers.Transfer dbTransfer)
        {
            return new Transfer
            {
                Username = dbTransfer.Username,
                Filename = dbTransfer.Filename,
                Size = dbTransfer.Size,
                Direction = dbTransfer.Direction,
                State = dbTransfer.State,
                StartOffset = dbTransfer.StartOffset,
                BytesTransferred = dbTransfer.BytesTransferred,
                AverageSpeed = dbTransfer.AverageSpeed,
                PercentComplete = dbTransfer.PercentComplete,
                BytesRemaining = dbTransfer.BytesRemaining,
                StartTime = dbTransfer.StartedAt,
                EndTime = dbTransfer.EndedAt,
                ElapsedTime = dbTransfer.ElapsedTime?.TotalMilliseconds,
                RemainingTime = dbTransfer.RemainingTime?.TotalMilliseconds,
                PlaceInQueue = dbTransfer.PlaceInQueue,
                Exception = dbTransfer.Exception,
            };
        }
    }
}
