
using Cove.Server.Actor;

namespace Cove.Server.Plugins
{
    public class PillaPLugin : CovePlugin
    {
        
    private CoveServer Server { get; set; } // lol
    public PillaPLugin(CoveServer server) : base(server)
    {
        Server = server;
    }
        public void initPila(List<WFPlayer> players)
        {   
            Random rand = new Random();
            WFPlayer randomPlayer = players[rand.Next(players.Count)];
            Server.messageGlobal("A " + randomPlayer.Username + " le toca pillar!!!! Escapad");
        }

    }
}