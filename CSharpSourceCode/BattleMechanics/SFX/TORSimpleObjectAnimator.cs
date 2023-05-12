using System;
using System.Linq;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade;
using TOR_Core.Utilities;

namespace TOR_Core.BattleMechanics.SFX
{
  
    
    public class TORSimpleObjectAnimator : ScriptComponentBehavior
    {
        public String AnimationName = "";
        private GameEntity _entityBody;
        private Skeleton _skeleton;
        private bool _init;



        protected  override void OnInit()
        {
            base.OnInit();
            Init();
        }
        
        protected  override void OnEditorInit()
        {
            base.OnInit();
            if(AnimationName==null)
                AnimationName = "";
            Init();
        }

        private void Init()
        {
            if (AnimationName != "")
            {
            
                this._entityBody = this.GameEntity;
                if (_entityBody == null)
                {
                    TORCommon.Say("no body");
                    return;
                }
                this._skeleton = this._entityBody.Skeleton;
                if (_skeleton == null)
                {
                    TORCommon.Say("no skeleton");
                    return;
                }
                
                this._skeleton.SetAnimationAtChannel(AnimationName, 0, blendInPeriod: 0.0f);
                _skeleton.SetAgentActionChannel(0,ActionIndexCache.act_none, 0,0,false);



                TORCommon.Say("Init");
                _init = true;
            }
        }

        protected override void OnEditorTick(float dt)
        {
            if (!_init)
            {
                Init();
            }

            if(_skeleton==null) return;
            
            if (AnimationName != _skeleton.GetAnimationAtChannel(0))
            {
                Init();
            }
        }
        



    }
}