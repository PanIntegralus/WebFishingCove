﻿using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cove.GodotFormat;

namespace Cove.Server.Actor
{
    public class WFPlayer : WFActor
    {
        public CSteamID SteamId { get; set; }
        public string FisherID { get; set; }
        public string Username { get; set; }

        public WFPlayer(CSteamID id, string fisherName) : base(0, "player", Vector3.zero)
        {
            SteamId = id;
            string randomID = new string(Enumerable.Range(0, 3).Select(_ => "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789"[new Random().Next(36)]).ToArray());
            FisherID = randomID;
            Username = fisherName;

            pos = new Vector3(0, 0, 0);
            despawn = false; // players down despawn!
        }

                    // Proximity detection using Godot's DistanceTo method
        public bool IsNear(WFActor other, float proximityDistance)
        {
            return pos.DistanceTo(other.pos) <= proximityDistance;
        }

        // Touch detection with optional threshold for floating-point precision
        public bool IsTouching(WFActor other, float touchDistance = 0.1f)
        {
            return pos.DistanceTo(other.pos) <= touchDistance;
        }
        
    };
}
