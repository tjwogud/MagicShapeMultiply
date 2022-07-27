using System.IO;
using System.Xml.Serialization;
using UnityEngine;
using UnityModManagerNet;

namespace MagicShapeMultiply
{
    public class Settings : UnityModManager.ModSettings
    {
        public override void Save(UnityModManager.ModEntry modEntry)
        {
            var filepath = Path.Combine(modEntry.Path, "Settings.xml");
            using StreamWriter writer = new StreamWriter(filepath);
            new XmlSerializer(GetType()).Serialize(writer, this);
        }

        public KeyCode key = KeyCode.M;
        public bool ctrl = true;
        public bool alt = false;
        public bool shift = false;
    }
}