using System;
using System.Collections.Generic;
using System.Text;
using Gamefloor.Framework;
using Gamefloor.Graphics;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace Panelix
{
    class TestMode : GameMode
    {
        public TestMode(Game game) : base(game)
        {
        }

        private TextureAtlas tex;

        protected override void Begin()
        {
            AtlasBuilder builder = new AtlasBuilder(Game);
            builder.Textures.Add(new LoadingTexture("block00.png"));
            builder.Textures.Add(new LoadingTexture("block10.png"));
            builder.Textures.Add(new LoadingTexture("block20.png"));
            builder.Textures.Add(new LoadingTexture("block30.png"));
            builder.Textures.Add(new LoadingTexture("block40.png"));
            builder.Textures.Add(new LoadingTexture("block50.png"));

            using (tex = builder.Build())
            {
                //Game.Wait(300);
                while (true) Game.NextFrame();
            }
        }

        public override void Update(bool RenderableFrame)
        {
            //flicker = !flicker;
        }

        public override void Render(IGraphicsContext context)
        {
            GL.ClearColor(0.25f, 0.5f, 1.0f, 1.0f);
            GL.Clear(ClearBufferMask.ColorBufferBit);

            GL.Viewport(0, 0, 640, 480);
            GL.LoadIdentity();
            GL.Ortho(0.0d, 640.0d, 480.0d, 0.0d, -1.0d, 1.0d);
            GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);
            GL.Enable(EnableCap.Texture2D);
            GL.Enable(EnableCap.Blend);
            tex.Bind();

            GL.Begin(BeginMode.Quads);
            {
                GL.Color4(1.0f, 1.0f, 1.0f, 1.0f);
                drawbrick(0, 0, tex["block00.png"]);
                drawbrick(160, 0, tex["block10.png"]);
                drawbrick(0, 160, tex["block20.png"]);
                drawbrick(160, 160, tex["block30.png"]);
                drawbrick(0, 320, tex["block40.png"]);
                drawbrick(160, 320, tex["block50.png"]);
            }
            GL.End();
        }

        void drawbrick(int x, int y, TextureInfo brick)
        {
            GL.TexCoord2(brick.Left, brick.Top);
            GL.Vertex3((float)x, (float)y, 0.0f);

            GL.TexCoord2(brick.Right, brick.Top);
            GL.Vertex3((float)(x + brick.Width), (float)y, 0.0f);

            GL.TexCoord2(1.0f, 1.0f);
            GL.TexCoord2(brick.Right, brick.Bottom);
            GL.Vertex3((float)(x + brick.Width), (float)(y + brick.Height), 0.0f);

            GL.TexCoord2(0.0f, 1.0f);
            GL.TexCoord2(brick.Left, brick.Bottom);
            GL.Vertex3((float)x, (float)(y + brick.Height), 0.0f);
        }
    }
}
