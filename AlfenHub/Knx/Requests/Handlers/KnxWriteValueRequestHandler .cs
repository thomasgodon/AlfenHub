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

        public Task Handle(KnxWriteValueRequest request, CancellationToken cancellationToken)
        {
            if (!_writeGroupAddressCapabilityMapping.TryGetValue(request.GroupAddress, out var capability))
            {
                return Task.CompletedTask;
            }

            try
            {
                ProcessCapabilityValue(capability, request.Value);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "{Message}", e.Message);
            }

            return Task.CompletedTask;
        }

        private static Dictionary<GroupAddress, string> BuildWriteGroupAddressCapabilityMapping(KnxOptions options)
            => options.GetWriteGroupAddressesFromOptions()
                .ToDictionary(
                    groupAddressMapping => GroupAddress.Parse(groupAddressMapping.Value),
                    groupAddressMapping => groupAddressMapping.Key);

        private void ProcessCapabilityValue(string capability, byte[] value)
        {
            switch (capability)
            {
                case "Socket1.SlaveMaxCurrent":
                    if (!_alfenModbusClient.SocketWritableData.TryGetValue(1, out var socketData))
                    {
                        _logger.LogWarning("Could not resolve socket 1 writable data");
                        break;
                    }
                    socketData.ModbusSlaveMaxCurrent = BitConverter.ToSingle(value.Reverse().ToArray());
                    break;

                default:
                    _logger.LogWarning("Writing parameter '{Parameter}' not implemented", capability);
                    break;
            }
        }
    }
}
