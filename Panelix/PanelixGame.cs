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
            TestEndlessMode testmode = new TestEndlessMode(this);
            testmode.Run();
        }

        public PanelixGame(int width, int height, String title)
            : base(width, height, title)
        {
            
        }

        protected override void SetupWindow()
        {
            this.TargetFps = 60;
            this.Vsync = true;
        }
    }
}
