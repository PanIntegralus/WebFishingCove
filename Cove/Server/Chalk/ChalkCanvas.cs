using Cove.GodotFormat;
using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Cove.Server.Chalk
{
    public class ChalkCanvas
    {
        public long canvasID;
        Dictionary<(float, float), int> chalkImage = new();

        public ChalkCanvas(long canvasID)
        {
            this.canvasID = canvasID;
        }

        public void drawChalk(Vector2 position, int color)
        {
            var key = (position.x, position.y);
            chalkImage[key] = color;
        }

        public Dictionary<int, object> getChalkPacket()
        {

            Dictionary<int, object> packet = new Dictionary<int, object>();
            ulong i = 0;
            foreach (var entry in chalkImage.ToList())
            {
                var position = new Vector2(entry.Key.Item1, entry.Key.Item2);
                Dictionary<int, object> arr = new()
                {
                    [0] = position,
                    [1] = entry.Value
                };
                packet[(int)i] = arr;
                i++;
            }

            return packet;
        }

        public Dictionary<(float, float), int> getChalkImage()
        {
            return chalkImage;
        }

        public void chalkUpdate(Dictionary<int, object> packet)
        {
            foreach (var entry in packet)
            {
                var arr = (Dictionary<int, object>)entry.Value;
                var position = (Vector2)arr[0];
                var key = (position.x, position.y);
                long color = (long)arr[1];

                chalkImage[key] = (int)color;
            }
        }

        public void resetCanvas() {
            foreach (var entry in chalkImage)
            {
                chalkImage[entry.Key] = -1;   
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
                string line = $"{pair.Key.Item1}:{pair.Key.Item2}:{pair.Value}";
                lines.Add(line);
            }
            File.WriteAllLines($"chalk_{canvasID}.txt", lines);
        }

        public void loadCanvas()
        {
            var lines = File.ReadAllLines($"chalk_{canvasID}.txt");
            clearCanvas();

            foreach (var line in lines)
            {
                var parts = line.Split(':');

                if (parts.Length == 3)
                {
                    if (float.TryParse(parts[0], out float x) &&
                    float.TryParse(parts[1], out float y) &&
                    int.TryParse(parts[2], out int color))
                    {
                        var key  = (x, y);
                        chalkImage[key] = color;
                    }
                }
            }
        }
    }
}
