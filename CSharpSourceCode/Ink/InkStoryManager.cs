using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TOR_Core.Utilities;

namespace TOR_Core.Ink
{
    public class InkStoryManager
    {
        private static InkStoryManager _instance;
        private Dictionary<string, InkStory> _stories = new Dictionary<string, InkStory>();

        private InkStoryManager() { }

        public static void Initialize()
        {
            _instance = new InkStoryManager();
            _instance.LoadStories();
        }

        private void LoadStories()
        {
            _stories.Clear();
            var path = TORPaths.TORCoreModuleRootPath + "InkStories/";
            if (Directory.Exists(path))
            {
                var files = Directory.GetFiles(path, "*.ink");
                foreach(var file in files)
                {
                    _stories.Add(Path.GetFileNameWithoutExtension(file), new InkStory(file));
                }
            }
        }

        public static void ReloadStories()
        {
            _instance.LoadStories();
        }

        public static InkStory GetStory(string name)
        {
            InkStory story = null;
            _instance._stories.TryGetValue(name, out story);
            return story;
        }

        public static void OpenStory(string name)
        {
            var story = GetStory(name);
            if(story != null && Game.Current.GameType is Campaign)
            {
                var behavior = Campaign.Current.GetCampaignBehavior<InkStoryCampaignBehavior>();
                if(behavior != null) behavior.OpenStory(story);
            }
        }

        public static void CloseCurrentStory()
        {
            if(Game.Current.GameType is Campaign)
            {
                var behavior = Campaign.Current.GetCampaignBehavior<InkStoryCampaignBehavior>();
                if (behavior != null) behavior.CloseStory();
            }
        }
    }
}
