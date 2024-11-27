﻿using System.Net.Security;
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
        Server.messageGlobal($"Server: Se acaba de unir unna personita muy especial saludad a " + player.Username);
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
                        SendPlayerChatMessage(sender, "!users - Shows all players in the server");
                        SendPlayerChatMessage(sender, "!spawn <actor> - Spawns an actor");
                        SendPlayerChatMessage(sender, "!kick <player> - Kicks a player");
                        SendPlayerChatMessage(sender, "!ban <player> - Bans a player");
                        SendPlayerChatMessage(sender, "!setjoinable <true/false> - Opens or closes the lobby");
                        SendPlayerChatMessage(sender, "!refreshadmins - Refreshes the admins list");
                        SendPlayerChatMessage(sender, "!uptime - Shows the server uptime");
                    }
                    break;

                case "!users":
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
                    {
                        if (!IsPlayerAdmin(sender)) return;
                        Server.readAdmins();
                    }
                    break;

                case "!uptime":
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
                        Server.messageGlobal("Iniciando pilla pilla");
                        PillaPLugin.initPila(GetAllPlayers().ToList());
                    }
                    break;
                
                case "!savecanvas":
                {
                    if (!IsPlayerAdmin(sender)) return;
                    SendPlayerChatMessage(sender, "Saving all canvas...");
                    foreach (ChalkCanvas canvas in Server.chalkCanvas)
                    {
                        canvas.saveCanvas();
                    }
                    break;
                }

                case "!loadcanvas":
                {
                    if (!IsPlayerAdmin(sender)) return;
                    SendPlayerChatMessage(sender, "Loading all canvas...");
                    foreach (ChalkCanvas canvas in Server.chalkCanvas)
                    {
                        canvas.loadCanvas();
                        Dictionary<string, object> chalkPacket = new Dictionary<string, object>();
                        foreach (KeyValuePair<int, object> entry in canvas.getChalkPacket())
                        {
                            chalkPacket[entry.Key.ToString()] = entry.Value;
                        }
                        SendPacketToAll(chalkPacket);
                    }
                    break;
                }
            }
        }
    }
}


