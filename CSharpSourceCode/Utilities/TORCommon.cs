using Helpers;
using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Map;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.ViewModelCollection.Inventory;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace TOR_Core.Utilities
{
    public static class TORCommon
    {
        private static Random _random = new Random();
        
        public static void Say(TextObject text)
        {
            Say(text.ToString());
        }

        /// <summary>
        /// Print a message to the MB2 message window.
        /// </summary>
        /// <param name="text">The text that you want to print to the console.</param>
        public static void Say(string text)
        {
            InformationManager.DisplayMessage(new InformationMessage(text, new TaleWorlds.Library.Color(134, 114, 250)));
        }


        public static string GetCompleteStringValue(TextObject textObject)
        {
            if (textObject == null) return null;
            List < TextObject > a= new List<TextObject> { textObject };
            List<string> b = TextObject.ConvertToStringList(a);

            return b[0];
        }

        public static void Log(string message, LogLevel severity)
        {
            var logger = LogManager.GetCurrentClassLogger();
            logger.Log(severity, message);
        }

        /// <summary>
        /// Copies the currently equipped equipment items to the Windows Clipboard to enable pasting.
        /// </summary>
        /// <param name="vm">The Inventory ViewModel instance</param>
        public static void CopyEquipmentToClipBoard(SPInventoryVM vm)
        {
            string text = "";
            text += GetText(vm.CharacterWeapon1Slot) + ",";
            text += GetText(vm.CharacterWeapon2Slot) + ",";
            text += GetText(vm.CharacterWeapon3Slot) + ",";
            text += GetText(vm.CharacterWeapon4Slot) + ",";
            text += GetText(vm.CharacterHelmSlot) + ",";
            text += GetText(vm.CharacterTorsoSlot) + ",";
            text += GetText(vm.CharacterCloakSlot) + ",";
            text += GetText(vm.CharacterGloveSlot) + ",";
            text += GetText(vm.CharacterBootSlot) + ",";
            text += GetText(vm.CharacterMountSlot) + ",";
            text += GetText(vm.CharacterMountArmorSlot);
            Clipboard.SetText(text);
            InformationManager.DisplayMessage(new InformationMessage("Equipment items copied!", TaleWorlds.Library.Colors.Green));
        }

        private static string GetText(SPItemVM slot)
        {
            if (slot.StringId != "" && slot.StringId != null) return "Item." + slot.StringId;
            else return "none";
        }
        
        public static Vec3 GetRandomDirection(float deviation, bool fixZ=true)
        {
            float x = MBRandom.RandomFloatRanged(-deviation, deviation);
            var y = MBRandom.RandomFloatRanged(-deviation, deviation);
            var z = fixZ? 1: MBRandom.RandomFloatRanged(-deviation, deviation);
            return new Vec3(x, y, z);
        }
        public static Mat3 GetRandomOrientation(Mat3 orientation, float deviation)
        {
            float rand1 = MBRandom.RandomFloatRanged(-deviation, deviation);
            orientation.f.RotateAboutX(rand1);
            float rand2 = MBRandom.RandomFloatRanged(-deviation, deviation);
            orientation.f.RotateAboutY(rand2);
            float rand3 = MBRandom.RandomFloatRanged(-deviation, deviation);
            orientation.f.RotateAboutZ(rand3);
            return orientation;
        }

        /// <summary>
        /// Picks a random scene for the main menu based on the naming convenction of towmm_menuscene_XX where XX are sequential numbers.
        /// </summary>
        /// <returns>The scene's name as string</returns>
        public static string GetRandomSceneForMainMenu()
        {
            var filterednames = new List<string>();
            string pickedname = "towmm_menuscene_01";
            var path = TORPaths.TOREnvironmentModuleRootPath + "SceneObj/";
            if (Directory.Exists(path))
            {
                var dirnames = Directory.GetDirectories(path);
                filterednames = dirnames.Where(x =>
                {
                    string[] s = x.Split('/');
                    var name = s[s.Length - 1];
                    if (name.StartsWith("towmm_")) return true;
                    else return false;
                }).ToList();
            }

            if (filterednames.Count > 0)
            {
                var index = _random.Next(0, filterednames.Count);
                pickedname = filterednames[index];
                string[] s = pickedname.Split('/');
                pickedname = s[s.Length - 1];
            }

            return pickedname;
        }

        /// <summary>
        /// Finds the Settlement closest to the specified part and within the radius given. Lower values for radius will lead to better performance.
        /// </summary>
        /// <param name="party"></param>
        /// <param name="radius"></param>
        /// <returns></returns>
        public static Settlement FindNearestSettlement(MobileParty party, float radius, Func<Settlement, bool> condition = null)
        {
            LocatableSearchData<Settlement> locatableSearchData = Settlement.StartFindingLocatablesAroundPosition(party.Position2D, radius);
            List<Settlement> nearbySettlements = new List<Settlement>();
            for (Settlement settlement = Settlement.FindNextLocatable(ref locatableSearchData); settlement != null; settlement = Settlement.FindNextLocatable(ref locatableSearchData))
            {
                nearbySettlements.Add(settlement);
            }
            if (condition != null)
            {
                nearbySettlements = nearbySettlements.FindAll(x => condition(x));
            }
            if (nearbySettlements.Count == 0) return null;
            // The list of nearbySettlements is unordered, thus we need to find the
            // settlement with minimum distance.
            return nearbySettlements.MinBy(
                settlement => Campaign.Current.Models.MapDistanceModel.GetDistance(party, settlement));
        }

        public static MBList<Settlement> FindSettlementsAroundPosition(Vec2 position, float radius, Func<Settlement, bool> condition = null)
        {
            MBList<Settlement> settlements = new MBList<Settlement>();
            LocatableSearchData<Settlement> locatableSearchData = Settlement.StartFindingLocatablesAroundPosition(position, radius);

            for (Settlement settlement = Settlement.FindNextLocatable(ref locatableSearchData); settlement != null; settlement = Settlement.FindNextLocatable(ref locatableSearchData))
            {
                if(condition == null || condition(settlement))
                {
                    settlements.Add(settlement);
                }
            }
            return settlements;
        }

        public static MBReadOnlyList<MobileParty> FindPartiesAroundPosition(Vec2 position, float radius, Func<MobileParty, bool> condition = null)
        {
            MBList<MobileParty> parties = new MBList<MobileParty>();
            LocatableSearchData<MobileParty> locatableSearchData = MobileParty.StartFindingLocatablesAroundPosition(position, radius);

            for (MobileParty party = MobileParty.FindNextLocatable(ref locatableSearchData); party != null; party = MobileParty.FindNextLocatable(ref locatableSearchData))
            {
                if (condition == null || condition(party))
                {
                    parties.Add(party);
                }
            }
            return new MBReadOnlyList<MobileParty>(parties);
        }

        public static void WriteHeightMapDataForCurrentScene()
        {
            var scene = Mission.Current?.Scene;
            if(scene != null && scene.HasTerrainHeightmap)
            {
                // heightmap generation
                /// Here's how to extraxt heightmap data from a scene that doesn't have terrain edit 
                /// data. The idea is to get number of nodes in the x and y directions, call it xNodes 
                /// and yNodes respectively, the number of meters in the x and y directions, call it 
                /// xMeters and yMeters, set the resolution of the output image in the x and y 
                /// directions, call it xRes and yRes, and get the 'height' for each pixel in the 
                /// output image using
                /// (x_i, y_j) = (xMeters * i / xRes, yMeters * j / yRes)
                Vec2i nodeDimension = default(Vec2i); // the number of nodes in the x and y directions
                float nodeSize = 0f; // the length of the side of a node in meters
                int layerCount = 0;
                int layerVersion = 0;
                scene.GetTerrainData(out nodeDimension, out nodeSize, out layerCount, out layerVersion);
                int xNodes = nodeDimension.Item1; // nodes in the x direction
                int yNodes = nodeDimension.Item2; // nodes in the y direction
                float xMeters = nodeSize * xNodes; // meters in the x direction
                float yMeters = nodeSize * yNodes; // meters in the y direction
                int xRes = 4096; // x resolution of final image, 1 pixel for every half meters for now
                int yRes = 4096; // y resolution of final image
                float[,] terrainData = new float[xRes, yRes];
                float max = float.MinValue; // max height of the terrain
                float min = float.MaxValue; // min height of the terrain
                for (int i = 0; i < xRes; i++)
                {
                    float x = xMeters * ((float)i / (float)xRes);
                    for (int j = 0; j < yRes; j++)
                    {
                        float y = yMeters * ((float)j / (float)xRes);
                        Vec2 pos = new Vec2(x, y);
                        float height = scene.GetTerrainHeight(pos);
                        terrainData[i, j] = height;
                        if (height > max)
                            max = height;

                        if (height < min)
                            min = height;
                    }
                }
                PixelFormat formatOutput = PixelFormats.Gray16;
                ushort[] imagedata = new ushort[yRes * xRes];
                for(int i = 0; i < yRes; i++)
                {
                    for(int j = 0; j < xRes; j++)
                    {
                        imagedata[i * yRes + j] = MapToRange(terrainData[i, j], min, max);
                    }
                }
                BitmapSource bitmap = BitmapSource.Create(yRes, xRes, 96, 96, formatOutput, null, imagedata, yRes * 2);
                TransformedBitmap tb = new TransformedBitmap(bitmap, new RotateTransform(270));
                FileStream stream = new FileStream("heightmap_for_" + scene.GetName() + ".png", FileMode.Create);
                PngBitmapEncoder encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(tb));
                encoder.Save(stream);
                Say("Heightmap saved.");
            }
        }
        private static ushort MapToRange(float value, float minSource, float maxSource, ushort minTarget = ushort.MinValue, ushort maxTarget = ushort.MaxValue)
        {
            var result = (value - minSource) / (maxSource - minSource) * (maxTarget - minTarget) + minTarget;
            return (ushort)result;
        }

        public static uint ConvertColorToUint(TaleWorlds.Library.Color color)
        {
            var r = (int)color.Red & 0xFF;
            var g = (int)color.Green & 0xFF;
            var b = (int)color.Blue & 0xFF;
            var a = (int)color.Alpha & 0xFF;

            return (uint)((r << 24) + (g << 16) + (b << 8) + (a));
        }
    }
}