using AlfenHub.Knx.Models;
using Knx.Falcon;
using MediatR;

namespace AlfenHub.Knx.Requests
{
    internal class KnxReadValueRequest : IRequest<KnxValue?>
    {
        public GroupAddress GroupAddress { get; init; }
    }
}
