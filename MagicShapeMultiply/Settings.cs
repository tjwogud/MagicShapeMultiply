using System.IO;
using System.Xml.Serialization;
using UnityModManagerNet;

namespace MagicShapeMultiply
{
    public class Settings : UnityModManager.ModSettings
    {
        public override void Save(UnityModManager.ModEntry modEntry)
        {
            var filepath = Path.Combine(modEntry.Path, "Settings.xml");
            using (var writer = new StreamWriter(filepath))
                new XmlSerializer(GetType()).Serialize(writer, this);
        }

        public void OnChange()
        {
        }

        public bool EnableInOrOut = true;
        public bool InOrOut = true;
        public bool MultiplyOrBPM = false;
        public bool ShowEvent = true;
        public bool RealOrTileBPM = true;
    }
}
