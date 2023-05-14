using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade.View;

namespace TOR_Core.HarmonyPatches
{
    [HarmonyPatch]
    public static class MountCreationPatches
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(MountVisualCreator), "SetHorseColors")]
        public static bool Prefix(MetaMesh horseMesh, MountCreationKey mountCreationKey)
        {
            bool result = true;
			for (int i = 0; i < horseMesh.MeshCount; i++)
			{
				Mesh meshAtIndex = horseMesh.GetMeshAtIndex(i);
				if (meshAtIndex.HasTag("DoNotOverrideVectorArguments"))
				{
                    result = false;
                    break;
				}
			}
			return result;
        }
    }
}
