using MediatR;
using AlfenHub.Alfen.Models;

namespace AlfenHub.Alfen.Notifications
{
    internal class AlfenDataArrivedNotification : INotification
    {
        public AlfenDataArrivedNotification(AlfenData data)
        {
            Data = data;
        }

        public AlfenData Data { get; }
    }
}
