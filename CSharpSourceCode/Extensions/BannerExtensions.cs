using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.View;
using TOR_Core.Extensions.ExtendedInfoSystem;

namespace TOR_Core.Extensions
{
    public static class BannerExtensions
    {
        public static string GetBannerImageResource(this Banner banner)
        {
            return ExtendedInfoManager.GetBannerImageResource(banner);
        }

        public static void AddBannerImageResource(this BannerVisual visual, ref MetaMesh metaMesh, string resourceName)
        {
            var texture = Texture.GetFromResource(resourceName);
            if (texture == null || !texture.IsValid) return;

            var material = Material.GetFromResource("faction_banner_expo_mat").CreateCopy();
            material.SetTexture(Material.MBTextureType.DiffuseMap, texture);
            material.SetTexture(Material.MBTextureType.DiffuseMap2, texture);
            if (material == null || !material.IsValid) return;

            metaMesh.GetMeshAtIndex(metaMesh.MeshCount - 1).ClearMesh();
            var matrixData = visual.Banner.BannerDataList.Last();

            var mesh = Mesh.CreateMeshWithMaterial(material);

            Vec3 position = new Vec3(-0.5f, -0.5f, 0f, -1f);
            Vec3 position2 = new Vec3(0.5f, -0.5f, 0f, -1f);
            Vec3 position3 = new Vec3(0.5f, 0.5f, 0f, -1f);
            Vec3 position4 = new Vec3(-0.5f, 0.5f, 0f, -1f);
            Vec3 normal = new Vec3(0f, 0f, 1f, -1f);
            Vec2 uvCoord = new Vec2(0f, 0f);
            Vec2 uvCoord2 = new Vec2(1f, 0f);
            Vec2 uvCoord3 = new Vec2(1f, 1f);
            Vec2 uvCoord4 = new Vec2(0f, 1f);
            UIntPtr uintPtr = mesh.LockEditDataWrite();
            int num = mesh.AddFaceCorner(position, normal, uvCoord, uint.MaxValue, uintPtr);
            int patchNode = mesh.AddFaceCorner(position2, normal, uvCoord2, uint.MaxValue, uintPtr);
            int num2 = mesh.AddFaceCorner(position3, normal, uvCoord3, uint.MaxValue, uintPtr);
            int patchNode2 = mesh.AddFaceCorner(position4, normal, uvCoord4, uint.MaxValue, uintPtr);
            mesh.AddFace(num, patchNode, num2, uintPtr);
            mesh.AddFace(num2, patchNode2, num, uintPtr);
            mesh.UnlockEditDataWrite(uintPtr);
            var meshMatrix = BannerVisual.GetMeshMatrix(ref mesh, matrixData.Position.x, matrixData.Position.y, matrixData.Size.x, matrixData.Size.y, matrixData.Mirror, matrixData.RotationValue * 2f * 3.1415927f, metaMesh.MeshCount);
            mesh.SetLocalFrame(meshMatrix);
            metaMesh.AddMesh(mesh);
        }
    }
}
