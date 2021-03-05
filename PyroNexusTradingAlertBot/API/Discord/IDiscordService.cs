using Discord.WebSocket;
using System.Threading.Tasks;

namespace PyroNexusTradingAlertBot.API
{
    public interface IDiscordService
    {
        public Task<bool> Ready();
        public SocketTextChannel GetSocketTextChannel(ulong channelId);
    }
}