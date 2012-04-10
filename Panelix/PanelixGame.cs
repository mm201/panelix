using System;
using System.Collections.Generic;
using System.Text;
using Gamefloor.Framework;

namespace Panelix
{
    class PanelixGame : Game
    {
        protected override void Begin()
        {
            TestMode testmode = new TestMode(this);
            testmode.Run();
        }

        public PanelixGame(int width, int height, String title)
            : base(width, height, title)
        {
        }
    }
}
