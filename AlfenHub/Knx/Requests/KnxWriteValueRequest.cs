using Knx.Falcon;
using MediatR;

namespace AlfenHub.Knx.Requests
{
    internal class KnxWriteValueRequest : IRequest
    {
        public GroupAddress GroupAddress { get; init; }
        public byte[] Value { get; init; } = [];
    }
}
