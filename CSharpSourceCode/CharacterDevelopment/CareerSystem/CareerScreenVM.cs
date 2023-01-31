using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.Library;

namespace TOR_Core.CharacterDevelopment.CareerSystem
{
    public class CareerScreenVM : ViewModel
    {
        private Action _closeAction;

        public CareerScreenVM(Action closeAction)
        {
            _closeAction = closeAction;
        }

        private void ExecuteClose()
        {
            _closeAction();
        }
    }
}
