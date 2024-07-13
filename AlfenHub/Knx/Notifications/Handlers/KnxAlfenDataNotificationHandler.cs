using AlfenHub.Alfen.Notifications;
using AlfenHub.Knx.Client;
using AlfenHub.Knx.Models;
using AlfenHub.Knx.Services;
using MediatR;
using Microsoft.Extensions.Options;

namespace AlfenHub.Knx.Notifications.Handlers
{
    internal class KnxAlfenDataNotificationHandler : INotificationHandler<AlfenDataArrivedNotification>
    {
        private readonly IKnxClient _knxClient;
        private readonly KnxOptions _knxOptions;
        private readonly IKnxValueBufferService _knxValueBufferService;

        public KnxAlfenDataNotificationHandler(
            IKnxClient knxClient,
            IOptions<KnxOptions> options,
            IKnxValueBufferService knxValueBufferService)
        {
            _knxClient = knxClient;
            _knxOptions = options.Value;
            _knxValueBufferService = knxValueBufferService;
        }

        public async Task Handle(AlfenDataArrivedNotification notification, CancellationToken cancellationToken)
        {
            if (!_knxOptions.Enabled)
            {
                return;
            }

            var values = _knxValueBufferService.UpdateKnxValues(notification.Data);
            await _knxClient.SendValuesAsync(values, cancellationToken);
        }
    }
}
