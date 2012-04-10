using System;
using System.Collections.Generic;
using System.Text;
using Gamefloor.Framework;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace Panelix
{
    class TestMode : GameMode
    {
        public TestMode(Game game) : base(game)
        {
        }

        protected override void Begin()
        {
            Game.Wait(300);
        }

        public override void Update(bool RenderableFrame)
        {
            flicker = !flicker;
        }

        bool flicker = false;

        public override void Render(IGraphicsContext context)
        {
            GL.ClearColor(1.0f, 0.25f, 0.0f, 1.0f);
            GL.Clear(ClearBufferMask.ColorBufferBit);

            GL.Viewport(0, 0, 640, 480);
            GL.LoadIdentity();
            GL.Ortho(0.0d, 1.0d, 1.0d, 0.0d, -1.0d, 1.0d);

            GL.Begin(BeginMode.TriangleStrip);
            {
                if (flicker)
                {
                    GL.Color4(0.0f, 0.0f, 1.0f, 1.0f);
                    GL.Vertex3(0.0f, 0.0f, 0.0f);

                    GL.Color4(1.0f, 1.0f, 1.0f, 1.0f);
                    GL.Vertex3(1.0f, 0.0f, 0.0f);

                    GL.Color4(1.0f, 0.0f, 0.0f, 1.0f);
                    GL.Vertex3(0.0f, 1.0f, 0.0f);

                    GL.Color4(0.0f, 1.0f, 0.0f, 1.0f);
                    GL.Vertex3(1.0f, 1.0f, 0.0f);
                }
                else
                {
                    GL.Color4(1.0f, 0.0f, 0.0f, 1.0f);
                    GL.Vertex3(0.0f, 0.0f, 0.0f);

                    GL.Color4(0.0f, 1.0f, 0.0f, 1.0f);
                    GL.Vertex3(1.0f, 0.0f, 0.0f);

                    GL.Color4(0.0f, 0.0f, 1.0f, 1.0f);
                    GL.Vertex3(0.0f, 1.0f, 0.0f);

                    GL.Color4(1.0f, 1.0f, 1.0f, 1.0f);
                    GL.Vertex3(1.0f, 1.0f, 0.0f);
                }
            }
            GL.End();
        }
    }
}
