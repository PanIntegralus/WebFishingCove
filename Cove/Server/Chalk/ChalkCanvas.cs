using Cove.GodotFormat;
using System;
using System.IO;
using System.Text.Json;

namespace Cove.Server.Chalk
{
    public class SerializableVector2
    {
        public float x { get; set; }
        public float y { get; set; }

        public SerializableVector2(Vector2 vector2)
        {
            this.x = vector2.x;
            this.y = vector2.y;
        }

        public Vector2 ToVector2()
        {
            return new Vector2(x, y);
        }
    }
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
            var serializableDictionary = new Dictionary<SerializableVector2, int>();
            foreach (var pair in chalkImage)
            {
                serializableDictionary[new SerializableVector2(pair.Key)] = pair.Value;
            }

            string json = JsonSerializer.Serialize(serializableDictionary, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            File.WriteAllText($"canvas_{canvasID}.json", json);
        }

        public void loadCanvas()
        {
            string json = File.ReadAllText($"canvas_{canvasID}.json");

            var serializableDictionary = JsonSerializer.Deserialize<Dictionary<SerializableVector2, int>>(json);

            chalkImage.Clear();
            foreach (var pair in serializableDictionary)
            {
                chalkImage[pair.Key.ToVector2()] = pair.Value;
            }
        }
    }
}
