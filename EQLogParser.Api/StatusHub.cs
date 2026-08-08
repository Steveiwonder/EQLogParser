using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace EQLogParser.Api
{
    public class StatusHub : Hub
    {
        private readonly StatusStore _statusStore;

        public StatusHub(StatusStore statusStore)
        {
            _statusStore = statusStore;
        }

        public override async Task OnConnectedAsync()
        {
            if (_statusStore.Current != null)
            {
                await Clients.Caller.SendAsync("statusUpdated", _statusStore.Current);
            }

            await base.OnConnectedAsync();
        }
    }
}
