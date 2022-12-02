using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;

namespace TOR_Core.CharacterDevelopment.CareerSystem
{
    public class CareerNode : IEquatable<CareerNode>
    {
        private List<CareerNode> _nextNodes = new List<CareerNode>();
        private Action<Hero> _onSelected;
        public string StringId { get; private set; }
        public CareerNode(string stringId, Action<Hero> onSelected)
        {
            StringId = stringId;
            _onSelected = onSelected;
        }

        public void AddNextNode(CareerNode node)
        {
            if (!_nextNodes.Contains(node)) _nextNodes.Add(node);
        }

        public bool Equals(CareerNode other)
        {
            return other.StringId == this.StringId;
        }
    }
}
