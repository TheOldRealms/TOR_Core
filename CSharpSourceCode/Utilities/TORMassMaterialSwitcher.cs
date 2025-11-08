using SandBox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace TOR_Core.Utilities
{
    public class TORMassMaterialSwitcher : ScriptComponentBehavior
    {
        public Material SourceMaterial;
        public Material TargetMaterial;
        public bool UseMaterialNamePatternMatching;
        public int TextureScale;
        public SimpleButton Reverse;
        public SimpleButton SwitchAll;

        protected override void OnEditorVariableChanged(string variableName)
        {
            base.OnEditorVariableChanged(variableName);
            if (variableName == "SwitchAll") DoSwap();
            if (variableName == "Reverse") DoReverse();
        }

        private void DoReverse()
        {
            if (SourceMaterial == null || TargetMaterial == null) return;
            var source = SourceMaterial;
            var target = TargetMaterial;
            SourceMaterial = target;
            TargetMaterial = source;
        }

        private void DoSwap()
        {
            if (SourceMaterial == null || TargetMaterial == null) return;
            List<GameEntity> entities = new List<GameEntity>();
            Scene.GetEntities(ref entities);
            if(entities.Count > 0)
            {
                foreach(var entity in entities)
                {
                    for(int i = 0; i < entity.MultiMeshComponentCount; i++)
                    {
                        var multiMesh = entity.GetMetaMesh(i);
                        if(multiMesh != null)
                        {
                            for(int y = 0; y < multiMesh.MeshCount; y++)
                            {
                                var mesh = multiMesh.GetMeshAtIndex(y);
                                var mat = mesh.GetMaterial();
                                if(mat == SourceMaterial || (UseMaterialNamePatternMatching && mat.Name.Contains(SourceMaterial.Name)))
                                {
                                    mesh.SetMaterial(TargetMaterial);
                                    mesh.SetVectorArgument2(TextureScale, TextureScale, 0, 0);
                                }
                            }
                        }
                    }
                }
            }
        }

        protected override bool IsOnlyVisual() => true;
    }
}
