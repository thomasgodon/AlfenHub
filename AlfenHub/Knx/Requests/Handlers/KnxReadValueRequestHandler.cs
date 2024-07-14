using AlfenHub.Knx.Extensions;
using AlfenHub.Knx.Models;
using AlfenHub.Knx.Services;
using Knx.Falcon;
using MediatR;
using Microsoft.Extensions.Options;

namespace AlfenHub.Knx.Requests.Handlers
{
    internal class KnxReadValueRequestHandler : IRequestHandler<KnxReadValueRequest, KnxValue?>
    {
        private readonly IKnxValueBufferService _knxValueBufferService;
        private readonly Dictionary<GroupAddress, string> _readGroupAddressCapabilityMapping;

        public KnxReadValueRequestHandler(
            IOptions<KnxOptions> options,
            IKnxValueBufferService knxValueBufferService)
        {
            _knxValueBufferService = knxValueBufferService;
            _readGroupAddressCapabilityMapping = BuildReadGroupAddressCapabilityMapping(options.Value);
        }

        public Task<KnxValue?> Handle(KnxReadValueRequest request, CancellationToken cancellationToken)
        {
            if (!_readGroupAddressCapabilityMapping.TryGetValue(request.GroupAddress, out var capability))
            {
                return Task.FromResult<KnxValue?>(null);
            }

            return !_knxValueBufferService.GetKnxValues().TryGetValue(capability, out var knxValue) 
                ? Task.FromResult<KnxValue?>(null) 
                : Task.FromResult<KnxValue?>(knxValue);
        }

        private static Dictionary<GroupAddress, string> BuildReadGroupAddressCapabilityMapping(KnxOptions options)
            => options.GetReadGroupAddressesFromOptions()
                .ToDictionary(
                    groupAddressMapping => GroupAddress.Parse(groupAddressMapping.Value),
                    groupAddressMapping => groupAddressMapping.Key);
    }
}
