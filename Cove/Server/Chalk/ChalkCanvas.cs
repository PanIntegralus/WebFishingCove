using Cove.GodotFormat;
using System;
using System.IO;

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
            foreach (KeyValuePair<Vector2, int> entry in chalkImage.ToDictionary(pair => pair.Key, pair => pair.Value))
            {
                Dictionary<int, object> arr = new();
                arr[0] = entry.Key;
                arr[1] = entry.Value;
                packet[(int)i] = arr;
                i++;
            }

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
            List<string> lines = new List<string>();

            foreach(var pair in chalkImage)
            {
                string line = $"{pair.Key.x}-{pair.Key.y}-{pair.Value}";
                lines.Add(line);
            }
            File.WriteAllText($"chalk_{canvasID}.txt", string.Empty);
            File.WriteAllLines($"chalk_{canvasID}.txt", lines);
        }

        public void loadCanvas()
        {
            var lines = File.ReadAllLines($"chalk_{canvasID}.txt");

            chalkImage.Clear();

            foreach (var line in lines)
            {
                var parts = line.Split('-');

                if (parts.Length == 3)
                {
                    if (float.TryParse(parts[0], out float x) &&
                    float.TryParse(parts[1], out float y) &&
                    int.TryParse(parts[2], out int color))
                    {
                        var vector = new Vector2(x, y);
                        chalkImage[vector] = color;
                    }
                }
            }
        }
    }
}
