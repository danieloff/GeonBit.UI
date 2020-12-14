using GeonBit.UI.Entities;
using GoodOrBad.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace GeonBit.UI
{
    public class SummonKeyboardInterface
    {
        public Func<string> RetrieveText;
        public Action<string> UpdateText;
    }

    public interface IGameUI
    {
        //Entity AddEntity(Entity entity);

        void AddChild(Entity child);

        IMouseInput GetMouseInputProvider();

        IKeyboardInput GetKeyboardInputProvider();

        GoodOrBadGame Game
        {
            get;
        }

        void SummonKeyboard(SummonKeyboardInterface keyboardinterface);
    }
}
