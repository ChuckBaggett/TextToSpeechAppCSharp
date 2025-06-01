using PiperSharp.Models; // For VoiceModel
using System; // For StringIsNullOrWhiteSpace, ToLowerInvariant, Split, ArgumentNullException

namespace TextToSpeechApp
{
    public class VoiceViewModel
    {
        public VoiceModel Model { get; }
        public string DisplayName { get; }

        public VoiceViewModel(VoiceModel model)
        {
            Model = model ?? throw new ArgumentNullException(nameof(model));

            string lang = model.Language?.Name ?? model.Language?.Code ?? "unk"; // Uses .Name
            string keyDisplayName = model.Key ?? "unknown_key";
            string nameDisplayName = model.Name ?? "";

            DisplayName = $"{keyDisplayName} ({lang})";

            if (!string.IsNullOrWhiteSpace(nameDisplayName))
            {
                string keyPrefix = "";
                if (keyDisplayName.Contains("-"))
                {
                    keyPrefix = keyDisplayName.Split('-')[0];
                }
                else
                {
                    keyPrefix = keyDisplayName;
                }

                bool nameIsKeyPrefix = nameDisplayName.ToLowerInvariant() == keyPrefix.ToLowerInvariant();
                bool nameIsFullKey = nameDisplayName.ToLowerInvariant() == keyDisplayName.ToLowerInvariant();

                if (!nameIsKeyPrefix && !nameIsFullKey)
                {
                    DisplayName = $"{nameDisplayName} - {keyDisplayName} ({lang})";
                }
            }
        }
    }
}