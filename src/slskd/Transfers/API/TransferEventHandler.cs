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

            Logger.LogInformation("TransferEventHandler initialized, subscribing to transfer events");

            // Subscribe to all transfer events
            EventBus.Subscribe<DownloadFileCompleteEvent>("TransferEventHandler", OnDownloadFileComplete);
            EventBus.Subscribe<DownloadFileStartedEvent>("TransferEventHandler", OnDownloadFileStarted);
            EventBus.Subscribe<DownloadFileProgressEvent>("TransferEventHandler", OnDownloadFileProgress);
            EventBus.Subscribe<DownloadFileCancelledEvent>("TransferEventHandler", OnDownloadFileCancelled);
            EventBus.Subscribe<DownloadFileErroredEvent>("TransferEventHandler", OnDownloadFileErrored);

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

        private async Task OnDownloadFileStarted(DownloadFileStartedEvent eventData)
        {
            Logger.LogInformation("DownloadFileStartedEvent received for file: {Filename}", eventData.Transfer.Filename);

            try
            {
                var apiTransfer = ConvertToApiTransfer(eventData.Transfer);
                Logger.LogInformation("Broadcasting CREATE event for: {Filename}", apiTransfer.Filename);
                await TransferHub.Clients.All.SendAsync("CREATE", apiTransfer);
                Logger.LogInformation("CREATE event broadcast completed");
            }
            catch (System.Exception ex)
            {
                Logger.LogError(ex, "Failed to broadcast transfer started event");
            }
        }

        private async Task OnDownloadFileProgress(DownloadFileProgressEvent eventData)
        {
            Logger.LogDebug("DownloadFileProgressEvent received for file: {Filename} ({Percent}%)", 
                eventData.Transfer.Filename, eventData.Transfer.PercentComplete);

            try
            {
                var apiTransfer = ConvertToApiTransfer(eventData.Transfer);
                await TransferHub.Clients.All.SendAsync("UPDATE", apiTransfer);
            }
            catch (System.Exception ex)
            {
                Logger.LogError(ex, "Failed to broadcast transfer progress event");
            }
        }

        private async Task OnDownloadFileCancelled(DownloadFileCancelledEvent eventData)
        {
            Logger.LogInformation("DownloadFileCancelledEvent received for file: {Filename}", eventData.Transfer.Filename);

            try
            {
                var apiTransfer = ConvertToApiTransfer(eventData.Transfer);
                Logger.LogInformation("Broadcasting UPDATE event for cancelled: {Filename}", apiTransfer.Filename);
                await TransferHub.Clients.All.SendAsync("UPDATE", apiTransfer);
                Logger.LogInformation("Cancelled UPDATE event broadcast completed");
            }
            catch (System.Exception ex)
            {
                Logger.LogError(ex, "Failed to broadcast transfer cancelled event");
            }
        }

        private async Task OnDownloadFileErrored(DownloadFileErroredEvent eventData)
        {
            Logger.LogInformation("DownloadFileErroredEvent received for file: {Filename}, Error: {Error}", 
                eventData.Transfer.Filename, eventData.ErrorMessage);

            try
            {
                var apiTransfer = ConvertToApiTransfer(eventData.Transfer);
                Logger.LogInformation("Broadcasting UPDATE event for errored: {Filename}", apiTransfer.Filename);
                await TransferHub.Clients.All.SendAsync("UPDATE", apiTransfer);
                Logger.LogInformation("Error UPDATE event broadcast completed");
            }
            catch (System.Exception ex)
            {
                Logger.LogError(ex, "Failed to broadcast transfer error event");
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
