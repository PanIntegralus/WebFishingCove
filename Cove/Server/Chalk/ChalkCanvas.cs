using Cove.GodotFormat;
using System;
using System.IO;
using System.Text.Json;

namespace Cove.Server.Chalk
{
    public class ChalkCanvas
    {
        public long canvasID;
        Dictionary<Vector2, int> chalkImage = new Dictionary<Vector2, int>();

        public ChalkCanvas(long canvasID)
        {
            this.canvasID = canvasID;
        }

        public void drawChalk(Vector2 position, int color)
        {
            chalkImage[position] = color;
        }

        public Dictionary<int, object> getChalkPacket()
        {
            Dictionary<int, object> packet = new Dictionary<int, object>();
            ulong i = 0;
            foreach (KeyValuePair<Vector2, int> entry in chalkImage)
            {
                Dictionary<int, object> arr = new();
                arr[0] = entry.Key;
                arr[1] = entry.Value;
                packet[(int)i] = arr;
                i++;
            }

            //CoveServer.printArray(packet);

            return packet;
        }

        public void chalkUpdate(Dictionary<int, object> packet)
        {
            foreach (KeyValuePair<int, object> entry in packet)
            {
                Dictionary<int, object> arr = (Dictionary<int, object>)entry.Value;
                Vector2 vector2 = (Vector2)arr[0];
                Int64 color = (Int64)arr[1];

                chalkImage[vector2] = (int)color;
            }
        }

        public void clearCanvas()
        {
            chalkImage.Clear();
        }

        public void saveCanvas()
        {
            var serializableChalkImage = chalkImage.ToDictionary(
                entry => $"{entry.Key.x},{entry.Key.y}",
                entry => entry.Value
            );
            string json = JsonSerializer.Serialize(serializableChalkImage);
            File.WriteAllText($"chalk_{canvasID}.json", json);
        }

        public void loadCanvas()
        {
            string json = File.ReadAllText($"chalk_{canvasID}.json");
            if (!string.IsNullOrEmpty(json))
            {
                var serializableChalkImage = JsonSerializer.Deserialize<Dictionary<string, int>>(json);

                chalkImage = serializableChalkImage.ToDictionary(
                    entry =>
                    {
                        var parts = entry.Key.Split(',');
                        return new Vector2(long.Parse(parts[0]), long.Parse(parts[1]));
                    },
                    entry => entry.Value
                );
            }
        }
    }
}
