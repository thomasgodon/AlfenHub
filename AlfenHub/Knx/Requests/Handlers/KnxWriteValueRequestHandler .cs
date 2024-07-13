using AlfenHub.Alfen.Modbus.Server;
using AlfenHub.Knx.Extensions;
using AlfenHub.Knx.Models;
using Knx.Falcon;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AlfenHub.Knx.Requests.Handlers
{
    internal class KnxWriteValueRequestHandler : IRequestHandler<KnxWriteValueRequest>
    {
        private readonly ILogger<KnxWriteValueRequestHandler> _logger;
        private readonly IAlfenModbusClient _alfenModbusClient;
        private readonly Dictionary<GroupAddress, string> _writeGroupAddressCapabilityMapping;

        public KnxWriteValueRequestHandler(
            IOptions<KnxOptions> options,
            ILogger<KnxWriteValueRequestHandler> logger,
            IAlfenModbusClient alfenModbusClient)
        {
            _logger = logger;
            _alfenModbusClient = alfenModbusClient;
            _writeGroupAddressCapabilityMapping = BuildWriteGroupAddressCapabilityMapping(options.Value);
        }

        public async Task Handle(KnxWriteValueRequest request, CancellationToken cancellationToken)
        {
            if (!_writeGroupAddressCapabilityMapping.TryGetValue(request.GroupAddress, out var capability))
            {
                return;
            }

            try
            {
                await ProcessCapabilityValue(capability, request.Value, cancellationToken);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "{Message}", e.Message);
            }
        }

        private static Dictionary<GroupAddress, string> BuildWriteGroupAddressCapabilityMapping(KnxOptions options)
            => options.GetWriteGroupAddressesFromOptions()
                .ToDictionary(
                    groupAddressMapping => GroupAddress.Parse(groupAddressMapping.Value),
                    groupAddressMapping => groupAddressMapping.Key);

        private Task ProcessCapabilityValue(string capability, byte[] value, CancellationToken cancellationToken)
        {
            switch (capability)
            {
                case "Socket1.SlaveMaxCurrent":
                    _alfenModbusClient.SetSlaveMaxCurrentAsync(1, BitConverter.ToSingle(value.Reverse().ToArray()), cancellationToken);
                    break;

                default:
                    _logger.LogWarning("Writing parameter '{Parameter}' not implemented", capability);
                    break;
            }

            return Task.CompletedTask;
        }
    }
}
