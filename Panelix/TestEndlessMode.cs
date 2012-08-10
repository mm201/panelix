using System;
using System.Collections.Generic;
using System.Text;
using Gamefloor.Framework;
using Gamefloor.Graphics;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using System.Drawing;
using Panelix.Components;
using OpenTK.Input;
using OpenTK;

namespace Panelix
{
    class TestEndlessMode : GameMode
    {
        public TestEndlessMode(Game game) : base(game)
        {
            zoom = game.Window.Height / 960.0f;
        }

        private TextureAtlas tex;
        private float zoom = 1.0f;
        private Field m_field;
        private TextureInfo[,] blocks;
        private TextureInfo cruiser;
        private TextureInfo whitepixel;

        private enum InputButton
        {
            None, Left, Right, Up, Down, Switch
        }

        private int input_lag;
        private InputButton btn;

        protected override void Begin()
        {
            try
            {
                Game.Window.KeyPress += InputEvent;

                m_field = new Field(Game, 1, Difficulty.Easy);
                m_field.OnCombo += DoCombo;
                m_field.OnChain += DoChain;

                AtlasBuilder builder = new AtlasBuilder(Game);
                builder.Textures.Add(LoadResized("block00.png"));
                builder.Textures.Add(LoadResized("block10.png"));
                builder.Textures.Add(LoadResized("block20.png"));
                builder.Textures.Add(LoadResized("block30.png"));
                builder.Textures.Add(LoadResized("block40.png"));
                builder.Textures.Add(LoadResized("block50.png"));
                builder.Textures.Add(LoadResized("cruiser0.png"));
                builder.Textures.Add(new LoadingTexture("white.png"));

                using (tex = builder.Build())
                {
                    //builder.Dispose();
                    builder = null;

                    blocks = new TextureInfo[6, 1];
                    blocks[0, 0] = tex["block00.png"];
                    blocks[1, 0] = tex["block10.png"];
                    blocks[2, 0] = tex["block20.png"];
                    blocks[3, 0] = tex["block30.png"];
                    blocks[4, 0] = tex["block40.png"];
                    blocks[5, 0] = tex["block50.png"];
                    cruiser = tex["cruiser0.png"];
                    whitepixel = tex["white.png"];

                    while (true)
                    {
                        HandleInput();
                        m_field.Cycle();
                        Game.NextFrame();
                    }
                }
            }
            finally
            {
                Game.Window.KeyPress -= InputEvent;
            }
        }

        protected void DoCombo(object sender, Field.PaneponEventArgs e)
        {

        }

        protected void DoChain(object sender, Field.PaneponEventArgs e)
        {

        }

        protected void InputEvent(object sender, KeyPressEventArgs e)
        {

        }

        protected void HandleInput()
        {
            if (input_lag > 0) input_lag--;

            Game.WaitForInput();
            KeyboardState ks = Keyboard.GetState();

            if (ks.IsKeyDown(Key.LShift) || ks.IsKeyDown(Key.RShift))
            {
                m_field.Lift();
            }

            if (ks.IsKeyDown(Key.Space))
            {
                if (btn != InputButton.Switch)
                {
                    btn = InputButton.Switch;
                    m_field.Switch();
                }
            }
            else if (ks.IsKeyDown(Key.Left))
            {
                if (btn != InputButton.Left)
                {
                    btn = InputButton.Left;
                    input_lag = 10;
                    if (m_field.CruiserPos.X > 0) m_field.CruiserPos = new Point(m_field.CruiserPos.X - 1, m_field.CruiserPos.Y);
                }
                else if (input_lag == 0)
                {
                    if (m_field.CruiserPos.X > 0) m_field.CruiserPos = new Point(m_field.CruiserPos.X - 1, m_field.CruiserPos.Y);
                }
            }
            else if (ks.IsKeyDown(Key.Right))
            {
                if (btn != InputButton.Right)
                {
                    btn = InputButton.Right;
                    input_lag = 10;
                    if (m_field.CruiserPos.X < 4) m_field.CruiserPos = new Point(m_field.CruiserPos.X + 1, m_field.CruiserPos.Y);
                }
                else if (input_lag == 0)
                {
                    if (m_field.CruiserPos.X < 4) m_field.CruiserPos = new Point(m_field.CruiserPos.X + 1, m_field.CruiserPos.Y);
                }
            }
            else if (ks.IsKeyDown(Key.Up))
            {
                if (btn != InputButton.Up)
                {
                    btn = InputButton.Up;
                    input_lag = 10;
                    if (m_field.CruiserPos.Y < 11) m_field.CruiserPos = new Point(m_field.CruiserPos.X, m_field.CruiserPos.Y + 1);
                }
                else if (input_lag == 0)
                {
                    if (m_field.CruiserPos.Y < 11) m_field.CruiserPos = new Point(m_field.CruiserPos.X, m_field.CruiserPos.Y + 1);
                }
            }
            else if (ks.IsKeyDown(Key.Down))
            {
                if (btn != InputButton.Down)
                {
                    btn = InputButton.Down;
                    input_lag = 10;
                    if (m_field.CruiserPos.Y > 0) m_field.CruiserPos = new Point(m_field.CruiserPos.X, m_field.CruiserPos.Y - 1);
                }
                else if (input_lag == 0)
                {
                    if (m_field.CruiserPos.Y > 0) m_field.CruiserPos = new Point(m_field.CruiserPos.X, m_field.CruiserPos.Y - 1);
                }
            }
            else
            {
                btn = InputButton.None;
            }


        }

        public override void Update(bool RenderableFrame)
        {

        }

        public override void Render(IGraphicsContext context)
        {
            GL.ClearColor(0.25f, 0.5f, 1.0f, 1.0f);
            GL.Clear(ClearBufferMask.ColorBufferBit);

            Game.SetViewport(ViewportSizing.Pixels, zoom);

            DrawField();
        }

        void DrawField()
        {
            // todo: move me to field class
            int BlockSize = 80;
            int h = 960;
            
            Point start = new Point(0, 880);
            Color4 white = new Color4(1.0f, 1.0f, 1.0f, 1.0f);
            Color4 transwhite = new Color4(1.0f, 1.0f, 1.0f, 0.75f);
            Color4 red = new Color4(1.0f, 0.0f, 0.0f, 1.0f);
            Color4 bottom_tint = new Color4(0.5f, 0.5f, 0.5f, 1.0f);

            GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);
            GL.Enable(EnableCap.Texture2D);
            GL.Enable(EnableCap.Blend);
            tex.Bind();

            GL.Begin(BeginMode.Quads);
            {
                // Dark bottom row
                Block theblock;
                BlockColour thecolour;
                int bx, by;

                by = start.Y + BlockSize - ((m_field.LiftPhase * BlockSize) >> 16);
                for (int x = 0; x < 6; x++)
                {
                    thecolour = m_field.BottomRow(x);
                    bx = start.X + x * BlockSize;

                    drawbrick(bx, by, blocks[(int)(thecolour) - 1, 0], BlockSize, bottom_tint, h - by);
                }

                // main field
                bool invis;
                Color4 block_col;
                TextureInfo block_tex;

                for (int y = 0; y < 12; y++)
                {
                    for (int x = 0; x < 6; x++)
                    {
                        theblock = m_field.BlockAt(x, y);
                        by = start.Y - (y * BlockSize) - ((m_field.LiftPhase * BlockSize) >> 16);

                        if (theblock.Colour > 0)
                        {
                            bx = start.X + x * BlockSize;
                            invis = false;

                            switch (theblock.State)
                            {
                                case BlockState.SwitchLeft:
                                    bx -= (BlockSize * (4 - (int)(theblock.Parameter))) / 4;
                                    goto default;
                                case BlockState.SwitchRight:
                                    bx += (BlockSize * (4 - (int)(theblock.Parameter))) / 4;
                                    goto default;
                                case BlockState.Vanishing:
                                case BlockState.ComboVanishing:
                                    if (theblock.Parameter > 0) // flash
                                    {
                                        if (theblock.Parameter % 2 == 0)
                                        {
                                            block_col = white;
                                            block_tex = whitepixel;
                                        }
                                        else
                                        {
                                            block_col = bottom_tint;
                                            block_tex = blocks[(int)(theblock.Colour) - 1, 0];
                                        }
                                    }
                                    else if (theblock.FaceTime > 0) // face
                                    {
                                        block_tex = blocks[(int)(theblock.Colour) - 1, 0];
                                        block_col = white;
                                    }
                                    else // invisible
                                    {
                                        invis = true;
                                        block_tex = blocks[(int)(theblock.Colour) - 1, 0];
                                        block_col = white;
                                    }
                                    break;
                                case BlockState.Normal:
                                    if (theblock.Parameter == 0) goto default;
                                    else
                                    {
                                        block_col = white;
                                        block_tex = blocks[(int)(theblock.Colour) - 1, 0]; // todo: bounce
                                        break;
                                    }
                                default:
                                    block_col = white;
                                    block_tex = blocks[(int)(theblock.Colour) - 1, 0];
                                    break;
                            }

                            if (!invis) drawbrick(bx, by, block_tex, BlockSize, block_col, BlockSize);
                        }
                    }
                }

                GL.Color4(white);

                // cruiser
                if (m_field.State == GameState.CruiserSlide)
                {
                    if (m_field.Counter > 20)
                    {
                        drawcruiser(start.X,
                            (int)(start.Y - BlockSize / 8 - ((12 * 40 - (60 - m_field.Counter) * 7) * BlockSize) / 40 - ((m_field.LiftPhase * BlockSize) >> 16)),
                            BlockSize);
                    }
                    else
                    {
                        drawcruiser((int)(start.X - BlockSize / 4 + ((4 * 20 - (20 - m_field.Counter) * 2) * BlockSize) / 20), start.Y - BlockSize / 8 - (5 * BlockSize) - ((m_field.LiftPhase * BlockSize) >> 16),
                            BlockSize);
                    }
                }
                else if (m_field.State == GameState.Countdown)
                {
                    if (true)
                    {
                        drawcruiser(start.X + m_field.CruiserPos.X * BlockSize,
                                    start.Y - m_field.CruiserPos.Y * BlockSize - ((m_field.LiftPhase * BlockSize) >> 16),
                                    BlockSize);
                    }
                }
                else if (m_field.State == GameState.Main)
                {
                    drawcruiser(start.X + m_field.CruiserPos.X * BlockSize,
                                start.Y - m_field.CruiserPos.Y * BlockSize - ((m_field.LiftPhase * BlockSize) >> 16),
                                BlockSize);
                }
                else if (m_field.State == GameState.GameOver)
                {
                }
            }
            GL.End();

        }

        void drawbrick(int x, int y, TextureInfo brick, int size, Color4 tint, int clip)
        {
            if (clip > size) clip = size;
            if (clip <= 0) return;

            GL.TexCoord2(brick.Left, brick.Top);
            GL.Color4(tint);
            GL.Vertex3((float)x, (float)y, 0.0f);

            GL.TexCoord2(brick.Right, brick.Top);
            GL.Vertex3((float)(x + size), (float)y, 0.0f);

            GL.TexCoord2(brick.Right, brick.Top + (brick.Bottom - brick.Top) * clip / size);
            GL.Vertex3((float)(x + size), (float)(y + clip), 0.0f);

            GL.TexCoord2(brick.Left, brick.Top + (brick.Bottom - brick.Top) * clip / size);
            GL.Vertex3((float)x, (float)(y + clip), 0.0f);
        }

        void drawcruiser(int x, int y, int size)
        {
            int offset = size / 8;
            int offset_right = size * 2 + offset;
            int offset_bottom = size + offset;

            GL.TexCoord2(cruiser.Left, cruiser.Top);
            GL.Vertex3(x - offset, y - offset, 0.0f);

            GL.TexCoord2(cruiser.Right, cruiser.Top);
            GL.Vertex3(x + offset_right, y - offset, 0.0f);

            GL.TexCoord2(cruiser.Right, cruiser.Bottom);
            GL.Vertex3(x + offset_right, y + offset_bottom, 0.0f);

            GL.TexCoord2(cruiser.Left, cruiser.Bottom);
            GL.Vertex3(x - offset, y + offset_bottom, 0.0f);
        }

        private LoadingTexture LoadResized(String filename)
        {
            Bitmap b = new Bitmap(filename);
            Bitmap d = new Bitmap(b, (int)(b.Width * zoom), (int)(b.Height * zoom));
            return new LoadingTexture(filename, d);
        }
    }
}
