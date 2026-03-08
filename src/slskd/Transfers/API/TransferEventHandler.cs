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
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.SignalR;
    using Microsoft.Extensions.Logging;
    using slskd.Events;

    public class TransferEventHandler
    {
        private static Transfer ToApiTransfer(slskd.Transfers.Transfer dbTransfer)
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

        public TransferEventHandler(IHubContext<TransferHub> transferHub, EventBus eventBus, ILogger<TransferEventHandler> logger)
        {
            TransferHub = transferHub;
            Logger = logger;

            eventBus.Subscribe<DownloadFileCompleteEvent>(nameof(TransferEventHandler), OnDownloadFileComplete);
            eventBus.Subscribe<DownloadFileStartedEvent>(nameof(TransferEventHandler), OnDownloadFileStarted);
            eventBus.Subscribe<DownloadFileProgressEvent>(nameof(TransferEventHandler), OnDownloadFileProgress);
            eventBus.Subscribe<DownloadFileCancelledEvent>(nameof(TransferEventHandler), OnDownloadFileCancelled);
            eventBus.Subscribe<DownloadFileErroredEvent>(nameof(TransferEventHandler), OnDownloadFileErrored);
        }

        private IHubContext<TransferHub> TransferHub { get; }
        private ILogger<TransferEventHandler> Logger { get; }

        private Task OnDownloadFileComplete(DownloadFileCompleteEvent eventData)
        {
            Logger.LogInformation("Transfer UPDATE complete for {Filename}", eventData.Transfer.Filename);
            return TransferHub.BroadcastUpdateAsync(ToApiTransfer(eventData.Transfer));
        }

        private Task OnDownloadFileStarted(DownloadFileStartedEvent eventData)
        {
            Logger.LogInformation("Transfer CREATE for {Filename}", eventData.Transfer.Filename);
            return TransferHub.BroadcastCreateAsync(ToApiTransfer(eventData.Transfer));
        }

        private Task OnDownloadFileProgress(DownloadFileProgressEvent eventData)
        {
            return TransferHub.BroadcastUpdateAsync(ToApiTransfer(eventData.Transfer));
        }

        private Task OnDownloadFileCancelled(DownloadFileCancelledEvent eventData)
        {
            Logger.LogInformation("Transfer UPDATE cancelled for {Filename}", eventData.Transfer.Filename);
            return TransferHub.BroadcastUpdateAsync(ToApiTransfer(eventData.Transfer));
        }

        private Task OnDownloadFileErrored(DownloadFileErroredEvent eventData)
        {
            Logger.LogInformation("Transfer UPDATE errored for {Filename}", eventData.Transfer.Filename);
            return TransferHub.BroadcastUpdateAsync(ToApiTransfer(eventData.Transfer));
        }
    }
}
