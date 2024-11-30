using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cove.Server.Actor;
using Steamworks;

namespace Cove.Server
{
    public partial class CoveServer
    {

        public void banPlayer(CSteamID id, bool saveToFile = false)
        {
            Dictionary<string, object> banPacket = new();
            banPacket["type"] = "ban";

            sendPacketToPlayer(banPacket, id);

            if (saveToFile)
                writeToBansFile(id);

            sendBlacklistPacketToAll(id.m_SteamID.ToString());
        }

        public bool isPlayerBanned(CSteamID id)
        {
            string fileDir = $"{AppDomain.CurrentDomain.BaseDirectory}bans.txt";

            string[] fileContent = File.ReadAllLines(fileDir);
            foreach (string line in fileContent)
            {
                if (line.Contains(id.m_SteamID.ToString()))
                {
                    return true;
                }
            }

            return false;
        }

        public bool isPlayerCanvasBanned(CSteamID id)
        {
            return CanvasBans.Contains(id.m_SteamID.ToString());
        }

        private void writeToBansFile(CSteamID id)
        {
            string fileDir = $"{AppDomain.CurrentDomain.BaseDirectory}bans.txt";
            WFPlayer player = AllPlayers.Find(p => p.SteamId == id);
            File.AppendAllLines(fileDir, [$"{id.m_SteamID} #{player.Username}"]);
        }

        public void kickPlayer(CSteamID id)
        {
            Dictionary<string, object> kickPacket = new();
            kickPacket["type"] = "kick";

            sendPacketToPlayer(kickPacket, id);
        }

        public void canvasBanPlayer(CSteamID id)
        {
            // add id to canvas_bans file
            string fileDir = $"{AppDomain.CurrentDomain.BaseDirectory}canvas_bans.txt";
            File.AppendAllLines(fileDir, [$"{id.m_SteamID}"]);
        }

        public void canvasUnbanPlayer(CSteamID id)
        {
            // remove id from canvas_bans file
            string fileDir = $"{AppDomain.CurrentDomain.BaseDirectory}canvas_bans.txt";
            string[] fileContent = File.ReadAllLines(fileDir);
            File.WriteAllLines(fileDir, fileContent.Where(line => !line.Contains(id.m_SteamID.ToString())));
        }
    }
}
