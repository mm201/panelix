﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Panelix
{
    class Program
    {
        static void Main(string[] args)
        {
            PanelixGame theGame = new PanelixGame(320, 480, "Panelix test");
            theGame.Run();
        }
    }
}
