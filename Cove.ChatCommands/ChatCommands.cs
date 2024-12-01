using System.Net.Security;
using System.Numerics;
using Cove.GodotFormat;
using Cove.Server;
using Cove.Server.Actor;
using Cove.Server.Chalk;
using Cove.Server.Plugins;
using Steamworks;
using System;

public class ChatCommands : CovePlugin
{
    CoveServer Server { get; set; } // lol
    PillaPLugin PillaPLugin { get; set; }
    public ChatCommands(CoveServer server) : base(server)
    {
        PillaPLugin = new PillaPLugin(server);
        Server = server;
    }

    // save the time the server was started
    public long serverStartTime = DateTimeOffset.Now.ToUnixTimeSeconds();

    public override void onInit()
    {
        base.onInit();
    }

    public override void onPlayerJoin(WFPlayer player)
    {
        base.onPlayerJoin(player);
    }

    public override void onChatMessage(WFPlayer sender, string message)
    {
        base.onChatMessage(sender, message);

        char[] msg = message.ToCharArray();
        if (msg[0] == "!".ToCharArray()[0]) // its a command!
        {
            string command = message.Split(" ")[0].ToLower();
            switch (command)
            {
                case "!help":
                    {
                        SendPlayerChatMessage(sender, "--- HELP ---");
                        SendPlayerChatMessage(sender, "!help - Shows this message");
                        SendPlayerChatMessage(sender, "!users - Shows all players in the server (!players, !list)");
                        SendPlayerChatMessage(sender, "!uptime - Shows the server uptime (!st)");
                        if (!IsPlayerAdmin(sender)) return; // only show admins their admin commands
                        SendPlayerChatMessage(sender, "!spawn <actor> - Spawns an actor");
                        SendPlayerChatMessage(sender, "!kick <player> - Kicks a player");
                        SendPlayerChatMessage(sender, "!ban <player> - Bans a player");
                        SendPlayerChatMessage(sender, "!setjoinable <true/false> - Opens or closes the lobby (!sj)");
                        SendPlayerChatMessage(sender, "!refreshadmins - Refreshes the admins list (!ral)");
                        SendPlayerChatMessage(sender, "!savecanvas - Save all chalk (!sc)" );
                        SendPlayerChatMessage(sender, "!loadcanvas - Load all chalk (!lc)");
                        SendPlayerChatMessage(sender, "!resetcanvas - Reset all chalk (!rc)");
                        SendPlayerChatMessage(sender, "!blockdraw <player> - Block a player from drawing (!bd)");
                    }
                    break;

                case "!users":
                case "!players":
                case "!list":
                    if (!IsPlayerAdmin(sender)) return;

                    // Get the command arguments
                    string[] commandParts = message.Split(' ');

                    int pageNumber = 1;
                    int pageSize = 10;

                    // Check if a page number was provided
                    if (commandParts.Length > 1)
                    {
                        if (!int.TryParse(commandParts[1], out pageNumber) || pageNumber < 1)
                        {
                            pageNumber = 1; // Default to page 1 if parsing fails or page number is less than 1
                        }
                    }

                    var allPlayers = GetAllPlayers();
                    int totalPlayers = allPlayers.Count();
                    int totalPages = (int)Math.Ceiling((double)totalPlayers / pageSize);

                    // Ensure the page number is within the valid range
                    if (pageNumber > totalPages) pageNumber = totalPages;

                    // Get the players for the current page
                    var playersOnPage = allPlayers.Skip((pageNumber - 1) * pageSize).Take(pageSize);

                    // Build the message to send
                    string messageBody = "";
                    foreach (var player in playersOnPage)
                    {
                        messageBody += $"\n{player.Username}: {player.FisherID}";
                    }

                    messageBody += $"\nPage {pageNumber} of {totalPages}";

                    SendPlayerChatMessage(sender, "Players in the server:" + messageBody + "\nAlways here - Cove");
                    break;

                case "!spawn":
                    {
                        if (!IsPlayerAdmin(sender)) return;

                        var actorType = message.Split(" ")[1].ToLower();
                        bool spawned = false;
                        switch (actorType)
                        {
                            case "rain":
                                Server.spawnRainCloud();
                                spawned = true;
                                break;

                            case "fish":
                                Server.spawnFish();
                                spawned = true;
                                break;

                            case "meteor":
                                spawned = true;
                                Server.spawnFish("fish_spawn_alien");
                                break;

                            case "portal":
                                Server.spawnVoidPortal();
                                spawned = true;
                                break;

                            case "metal":
                                Server.spawnMetal();
                                spawned = true;
                                break;
                        }
                        if (spawned)
                        {
                            SendPlayerChatMessage(sender, $"Spawned {actorType}");
                        }
                        else
                        {
                            SendPlayerChatMessage(sender, $"\"{actorType}\" is not a spawnable actor!");
                        }
                    }
                    break;

                case "!kick":
                    {
                        if (!IsPlayerAdmin(sender)) return;
                        string playerIdent = message.Substring(command.Length + 1);
                        // try find a user with the username first
                        WFPlayer kickedplayer = GetAllPlayers().ToList().Find(p => p.Username.Equals(playerIdent, StringComparison.OrdinalIgnoreCase));
                        // if there is no player with the username try find someone with that fisher ID
                        if (kickedplayer == null)
                            kickedplayer = GetAllPlayers().ToList().Find(p => p.FisherID.Equals(playerIdent, StringComparison.OrdinalIgnoreCase));

                        if (kickedplayer == null)
                        {
                            SendPlayerChatMessage(sender, "That's not a player!");
                        }
                        else
                        {
                            Dictionary<string, object> packet = new Dictionary<string, object>();
                            packet["type"] = "kick";

                            SendPacketToPlayer(packet, kickedplayer);

                            SendPlayerChatMessage(sender, $"Kicked {kickedplayer.Username}");
                            SendGlobalChatMessage($"{kickedplayer.Username} was kicked from the lobby!");
                        }
                    }
                    break;

                case "!ban":
                    {
                        if (!IsPlayerAdmin(sender)) return;
                        // hacky fix,
                        // Extract player name from the command message
                        string playerIdent = message.Substring(command.Length + 1);
                        // try find a user with the username first
                        WFPlayer playerToBan = GetAllPlayers().ToList().Find(p => p.Username.Equals(playerIdent, StringComparison.OrdinalIgnoreCase));
                        // if there is no player with the username try find someone with that fisher ID
                        if (playerToBan == null)
                            playerToBan = GetAllPlayers().ToList().Find(p => p.FisherID.Equals(playerIdent, StringComparison.OrdinalIgnoreCase));

                        if (playerToBan == null)
                        {
                            SendPlayerChatMessage(sender, "Player not found!");
                        }
                        else
                        {
                            BanPlayer(playerToBan);
                            SendPlayerChatMessage(sender, $"Banned {playerToBan.Username}");
                            SendGlobalChatMessage($"{playerToBan.Username} has been banned from the server.");
                        }
                    }
                    break;

                case "!setjoinable":
                case "!sj":
                    {
                        if (!IsPlayerAdmin(sender)) return;
                        string arg = message.Split(" ")[1].ToLower();
                        if (arg == "true")
                        {
                            //Server.gameLobby.SetJoinable(true);
                            SteamMatchmaking.SetLobbyJoinable(Server.SteamLobby, true);
                            SendPlayerChatMessage(sender, $"Opened lobby!");
                            if (!Server.codeOnly)
                            {
                                //Server.gameLobby.SetData("type", "public");
                                SteamMatchmaking.SetLobbyData(Server.SteamLobby, "type", "public");
                                SendPlayerChatMessage(sender, $"Unhid server from server list");
                            }
                        }
                        else if (arg == "false")
                        {
                            //Server.gameLobby.SetJoinable(false);
                            SteamMatchmaking.SetLobbyJoinable(Server.SteamLobby, false);
                            SendPlayerChatMessage(sender, $"Closed lobby!");
                            if (!Server.codeOnly)
                            {
                                //Server.gameLobby.SetData("type", "code_only");
                                SteamMatchmaking.SetLobbyData(Server.SteamLobby, "type", "code_only");
                                SendPlayerChatMessage(sender, $"Hid server from server list");
                            }
                        }
                        else
                        {
                            SendPlayerChatMessage(sender, $"\"{arg}\" is not true or false!");
                        }
                    }
                    break;

                case "!refreshadmins":
                case "!ral":
                    {
                        if (!IsPlayerAdmin(sender)) return;
                        Server.readAdmins();
                    }
                    break;

                case "!uptime":
                case "!st":
                    {
                        long currentTime = DateTimeOffset.Now.ToUnixTimeSeconds();
                        long uptime = currentTime - serverStartTime;

                        TimeSpan time = TimeSpan.FromSeconds(uptime);

                        int days = time.Days;
                        int hours = time.Hours;
                        int minutes = time.Minutes;
                        int seconds = time.Seconds;

                        string uptimeString = "";
                        if (days > 0)
                        {
                            uptimeString += $"{days} Days, ";
                        }
                        if (hours > 0)
                        {
                            uptimeString += $"{hours} Hours, ";
                        }
                        if ( minutes > 0)
                        {
                            uptimeString += $"{minutes} Minutes, ";
                        }
                        if (seconds > 0)
                        {
                            uptimeString += $"{seconds} Seconds";
                        }

                        SendPlayerChatMessage(sender, $"Server uptime: {uptimeString}");

                    }
                    break;
                
                case "!pilla":
                    {
                        if (!IsPlayerAdmin(sender)) return;
                        Server.messageGlobal("Iniciando pilla pilla");
                        PillaPLugin.initPila(GetAllPlayers().ToList());
                    }
                    break;
                
                case "!savecanvas":
                case "!sc":
                {
                    if (!IsPlayerAdmin(sender)) return;
                    SendPlayerChatMessage(sender, "Saving all canvas...");
                    foreach (ChalkCanvas canvas in Server.chalkCanvas)
                    {
                        canvas.saveCanvas();
                    }
                    SendPlayerChatMessage(sender, "Saved all canvas!");
                    break;
                }

                case "!loadcanvas":
                case "!lc":
                {
                    if (!IsPlayerAdmin(sender)) return;
                    foreach (ChalkCanvas canvas in Server.chalkCanvas)
                    {
                        SendPlayerChatMessage(sender, $"Loading canvas {canvas.canvasID}...");

                        SendPlayerChatMessage(sender, $"Canvas has {canvas.getChalkImage().Count} chalks");

                        Dictionary<int, object> allChalk = canvas.getChalkPacket();

                        foreach (KeyValuePair<int, object> entry in allChalk)
                        {
                            Dictionary<int, object> arr = (Dictionary<int, object>)entry.Value;
                            Cove.GodotFormat.Vector2 vector2 = (Cove.GodotFormat.Vector2)arr[0];
                            canvas.drawChalk(vector2, -1);
                        }

                        canvas.loadCanvas();
                        allChalk = canvas.getChalkPacket();
                        
                        // split the dictionary into chunks of 100
                        List<Dictionary<int, object>> chunks = new List<Dictionary<int, object>>();
                        Dictionary<int, object> chunk = new Dictionary<int, object>();

                        int i = 0;
                        foreach (var kvp in allChalk)
                        {
                            if (i >= 1000)
                            {
                                chunks.Add(chunk);
                                chunk = new Dictionary<int, object>();
                                i = 0;
                            }
                            chunk.Add(i, kvp.Value);
                            i++;
                        }

                        for (int index = 0; index < chunks.Count; index++)
                        {
                            Dictionary<string, object> chalkPacket = new Dictionary<string, object> { { "type", "chalk_packet" }, { "canvas_id", canvas.canvasID }, { "data", chunks[index] } };
                            SendPacketToAll(chalkPacket);
                            Thread.Sleep(10);
                        }

                        SendPlayerChatMessage(sender, "Canvas finished loading! Rejoin to see changes.");
                    }
                    break;
                }

                case "!resetcanvas":
                case "!rc":
                {
                    if (!IsPlayerAdmin(sender)) return;
                    foreach (ChalkCanvas canvas in Server.chalkCanvas)
                    {
                        canvas.clearCanvas();
                    }
                    SendPlayerChatMessage(sender, "Canvas cleared! Rejoin to see updates.");
                    break;
                }

                case "!blockdraw":
                case "!bc":
                {
                    if (!IsPlayerAdmin(sender)) return;
                    string playerIdent = message.Substring(command.Length + 1);
                    // try find a user with the username first
                    WFPlayer playerToBan = GetAllPlayers().ToList().Find(p => p.Username.Equals(playerIdent, StringComparison.OrdinalIgnoreCase));
                    // if there is no player with the username try find someone with that fisher ID
                    if (playerToBan == null)
                        playerToBan = GetAllPlayers().ToList().Find(p => p.FisherID.Equals(playerIdent, StringComparison.OrdinalIgnoreCase));
                    // if there is no player with the fisher ID try find someone with that steam ID
                    if (playerToBan == null)
                        playerToBan = GetAllPlayers().ToList().Find(p => p.SteamId.m_SteamID == ulong.Parse(playerIdent));

                    if (playerToBan == null)
                    {
                        SendPlayerChatMessage(sender, "Player not found!");
                    } else {
                        if (IsPlayerCanvasBanned(sender))
                        {
                            CanvasBanPlayer(playerToBan);
                            SendPlayerChatMessage(sender, $"Unblocked \"{sender.Username}\" from painting!");
                        } else
                        {
                            CanvasUnbanPlayer(playerToBan);
                            SendPlayerChatMessage(sender, $"Blocked \"{sender.Username}\" from painting!");
                        }
                    }
                }
                break;

            }
        }
    }
}


