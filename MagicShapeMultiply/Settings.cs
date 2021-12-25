using System.IO;
using System.Xml.Serialization;
using UnityModManagerNet;

namespace MagicShapeMultiply
{
    public class Settings : UnityModManager.ModSettings, IDrawable
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

        [Draw("Enable 'Internal Angle or External Angle' Option")]
        public bool EnableInOrOut = true;
        [Draw("Internal Angle or External Angle")]
        public bool InOrOut = true;
        [Draw("Keep Selecteds When Show Floor Nums")]
        public bool KeepSelecteds = true;
        [Draw("Multiply or BPM")]
        public bool MultiplyOrBPM = false;
        [Draw("Show 'SetSpeed' Event Icon Instead of 'Twirl' Event")]
        public bool ShowEvent = true;
    }
}
