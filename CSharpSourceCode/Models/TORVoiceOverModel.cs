using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.Localization;
using TOR_Core.Extensions;

namespace TOR_Core.Models
{
    public class TORVoiceOverModel : DefaultVoiceOverModel
    {
        public override string GetSoundPathForCharacter(CharacterObject character, VoiceObject voiceObject)
        {
            var result = base.GetSoundPathForCharacter(character, voiceObject);
            if(character.IsFemale && character.IsVampire()) return "";
            if (voiceObject != null && voiceObject.VoicePaths.Count > 0)
            {
                var path = voiceObject.VoicePaths[0];
                path = path.Replace("$PLATFORM", "PC");
                var file = path + ".ogg";
                if (File.Exists(file))
                {
                    result = file;
                }
            }
            return result;
        }
    }
}
