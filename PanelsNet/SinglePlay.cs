using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using Microsoft.Xna.Framework.Net;
using Microsoft.Xna.Framework.Storage;


namespace PanelsNet
{
    /// <summary>
    /// This is a game component that implements IUpdateable.
    /// </summary>
    public class SinglePlay : Microsoft.Xna.Framework.GameComponent
    {
        private BaseGame base_game;

        private SpriteBatch spriteBatch;

        private String note_text;
        private int note_show_time;

        private PaneponGame the_game;
        private int cruiser_anim_frame;
        private GraphicsDeviceManager graphics;

        public Texture2D[,] blocks;
        public Texture2D c1;
        public Texture2D c2;

        public SinglePlay(BaseGame game)
            : base(game)
        {
            base_game = game;
            graphics = game.graphics;
        }

        /// <summary>
        /// Allows the game component to perform any initialization it needs to before starting
        /// to run.  This is where it can query for any required services and load content.
        /// </summary>
        public override void Initialize()
        {
            // TODO: Add your initialization code here
            cruiser_anim_frame = 0;
            note_show_time = 0;

            the_game = new PaneponGame(1, Difficulty.Easy);
            the_game.OnCombo += DoCombo;
            the_game.OnChain += DoChain;

            input_lag = 0;

            spriteBatch = new SpriteBatch(graphics.GraphicsDevice);
            blocks = new Texture2D[6, 4];

            for (int x = 0; x < 4; x++)
            {
                blocks[0, x] = base_game.Content.Load<Texture2D>("red" + (x + 1).ToString());
                blocks[1, x] = base_game.Content.Load<Texture2D>("green" + (x + 1).ToString());
                blocks[2, x] = base_game.Content.Load<Texture2D>("teal" + (x + 1).ToString());
                blocks[3, x] = base_game.Content.Load<Texture2D>("yellow" + (x + 1).ToString());
                blocks[4, x] = base_game.Content.Load<Texture2D>("purple" + (x + 1).ToString());
                blocks[5, x] = base_game.Content.Load<Texture2D>("blue" + (x + 1).ToString());
            }

            c1 = base_game.Content.Load<Texture2D>("cruiser1");
            c2 = base_game.Content.Load<Texture2D>("cruiser2");

            base.Initialize();
        }

        protected void DoCombo(object sender, PaneponGame.PaneponEventArgs e)
        {
            note_text = e.Size.ToString() + " combo!";
            note_show_time = 60;
        }

        protected void DoChain(object sender, PaneponGame.PaneponEventArgs e)
        {
            note_text = e.Size.ToString() + " chain!";
            note_show_time = 60;
        }

        public void Render(GameTime gameTime)
        {
            base_game.graphics.GraphicsDevice.Clear(Color.CornflowerBlue);
            int w = graphics.GraphicsDevice.Viewport.Width;
            int h = graphics.GraphicsDevice.Viewport.Height;

            int BlockSize = h / 15;
            Point start = new Point(w / 2 - BlockSize * 3, h / 2 + BlockSize * 5);
            Color white = new Color(255, 255, 255);
            Color red = new Color(255, 0, 0);

            spriteBatch.Begin(SpriteBlendMode.AlphaBlend);

            Block theblock;

            int bx, by;

            // Draw the dark bottom row.
            Color grey = new Color(128, 128, 128);
            BlockColour thecolour;
            by = start.Y + BlockSize - ((the_game.LiftPhase * BlockSize) >> 16);
            for (int x = 0; x < 6; x++)
            {
                thecolour = the_game.BottomRow(x);

                bx = start.X + x * BlockSize;

                spriteBatch.Draw(blocks[(int)(thecolour) - 1, 0], new Rectangle(bx, by, BlockSize, BlockSize), grey);
            }

            bool invis, flasher;
            int flashcount = 0;
            Texture2D block_tex;
            Color block_col;
            List<VertexPositionColor> pts = new List<VertexPositionColor>();

            // Draw the main field.
            for (int y = 0; y < 12; y++)
            {
                for (int x = 0; x < 6; x++)
                {
                    theblock = the_game.BlockAt(x, y);
                    by = start.Y - (y * BlockSize) - ((the_game.LiftPhase * BlockSize) >> 16);

                    if (theblock.Colour > 0)
                    {
                        bx = start.X + x * BlockSize;
                        invis = false;
                        flasher = false;

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
                                    block_tex = blocks[(int)(theblock.Colour) - 1, 0];
                                    if (theblock.Parameter % 2 == 0)
                                    {
                                        block_col = white;
                                        flashcount++;
                                        pts.Add(new VertexPositionColor(new Vector3(bx + (float)BlockSize / 2f, by + (float)BlockSize / 2f, 0), white));
                                        flasher = true;
                                    }
                                    else
                                    {
                                        block_col = grey;
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
                                    block_tex = blocks[(int)(theblock.Colour) - 1, theblock.Parameter / 2];
                                    break;
                                }
                            default:
                                block_col = white;
                                block_tex = blocks[(int)(theblock.Colour) - 1, 0];
                                break;
                        }

                        if (flasher) { }
                        else if (!invis) spriteBatch.Draw(block_tex, new Rectangle(bx, by, BlockSize, BlockSize), block_col);
                    }
                }
            }

            spriteBatch.End();
            if (flashcount > 0)
            {
                BasicEffect effect = new BasicEffect(graphics.GraphicsDevice, null);
                effect.TextureEnabled = true;
                effect.LightingEnabled = false;
                effect.DiffuseColor = new Vector3(1f, 1f, 1f);
                effect.AmbientLightColor = new Vector3(1f, 1f, 1f);
                effect.Alpha = 1f;
                effect.VertexColorEnabled = true;
                effect.Projection = Matrix.CreateOrthographicOffCenter(0, w, h, 0, 0, 1);

                graphics.GraphicsDevice.RenderState.PointSize = BlockSize;

                effect.Begin();
                foreach (EffectPass pass in effect.CurrentTechnique.Passes)
                {
                    pass.Begin();
                    graphics.GraphicsDevice.DrawUserPrimitives<VertexPositionColor>(PrimitiveType.PointList, pts.ToArray(), 0, flashcount);
                    pass.End();
                }
                effect.End();
            }
            spriteBatch.Begin();

            if (the_game.State == GameState.CruiserSlide)
            {
                if (the_game.Counter > 20)
                {
                    spriteBatch.Draw(c1, new Rectangle(start.X - BlockSize / 4 + (4 * BlockSize), (int)(start.Y - BlockSize / 8 - ((12 * 40 - (60 - the_game.Counter) * 7) * BlockSize) / 40 - ((the_game.LiftPhase * BlockSize) >> 16)), (5 * BlockSize) / 2, (5 * BlockSize) / 4), white);
                }
                else
                {
                    spriteBatch.Draw(c1, new Rectangle((int)(start.X - BlockSize / 4 + ((4 * 20 - (20 - the_game.Counter) * 2) * BlockSize) / 20), start.Y - BlockSize / 8 - (5 * BlockSize) - ((the_game.LiftPhase * BlockSize) >> 16), (5 * BlockSize) / 2, (5 * BlockSize) / 4), white);
                }
            }
            else if (the_game.State == GameState.Countdown)
            {
                if (cruiser_anim_frame % 2 == 0)
                {
                    spriteBatch.Draw((cruiser_anim_frame >= 60) ? c2 : c1, new Rectangle(start.X - BlockSize / 4 + (the_game.CruiserPos.X * BlockSize), start.Y - BlockSize / 8 - (the_game.CruiserPos.Y * BlockSize) - ((the_game.LiftPhase * BlockSize) >> 16), (5 * BlockSize) / 2, (5 * BlockSize) / 4), white);
                }
            }
            else if (the_game.State == GameState.Main)
            {
                spriteBatch.Draw((cruiser_anim_frame >= 60) ? c2 : c1, new Rectangle(start.X - BlockSize / 4 + (the_game.CruiserPos.X * BlockSize), start.Y - BlockSize / 8 - (the_game.CruiserPos.Y * BlockSize) - ((the_game.LiftPhase * BlockSize) >> 16), (5 * BlockSize) / 2, (5 * BlockSize) / 4), white);
            }
            else if (the_game.State == GameState.GameOver)
            {
            }

            if (note_show_time > 0)
            {
                spriteBatch.DrawString(base_game.sprite_font, note_text, new Vector2(40, 40), white);
            }

            spriteBatch.End();
        }

        /// <summary>
        /// Allows the game component to update itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        public override void Update(GameTime gameTime)
        {
            // TODO: Add your update code here

            if (input_lag > 0) input_lag--;
            if (note_show_time > 0) note_show_time--;

            KeyboardState ks = Keyboard.GetState();

            if (ks.IsKeyDown(Keys.LeftShift) || ks.IsKeyDown(Keys.RightShift))
            {
                the_game.Lift();
            }
            if (ks.IsKeyDown(Keys.Space))
            {
                if (btn != InputButton.Switch)
                {
                    btn = InputButton.Switch;
                    the_game.Switch();
                }
            }
            else if (ks.IsKeyDown(Keys.Left))
            {
                if (btn != InputButton.Left)
                {
                    btn = InputButton.Left;
                    input_lag = 10;
                    if (the_game.CruiserPos.X > 0) the_game.CruiserPos = new Point(the_game.CruiserPos.X - 1, the_game.CruiserPos.Y);
                }
                else if (input_lag == 0)
                {
                    if (the_game.CruiserPos.X > 0) the_game.CruiserPos = new Point(the_game.CruiserPos.X - 1, the_game.CruiserPos.Y);
                }
            }
            else if (ks.IsKeyDown(Keys.Right))
            {
                if (btn != InputButton.Right)
                {
                    btn = InputButton.Right;
                    input_lag = 10;
                    if (the_game.CruiserPos.X < 4) the_game.CruiserPos = new Point(the_game.CruiserPos.X + 1, the_game.CruiserPos.Y);
                }
                else if (input_lag == 0)
                {
                    if (the_game.CruiserPos.X < 4) the_game.CruiserPos = new Point(the_game.CruiserPos.X + 1, the_game.CruiserPos.Y);
                }
            }
            else if (ks.IsKeyDown(Keys.Up))
            {
                if (btn != InputButton.Up)
                {
                    btn = InputButton.Up;
                    input_lag = 10;
                    if (the_game.CruiserPos.Y < 11) the_game.CruiserPos = new Point(the_game.CruiserPos.X, the_game.CruiserPos.Y + 1);
                }
                else if (input_lag == 0)
                {
                    if (the_game.CruiserPos.Y < 11) the_game.CruiserPos = new Point(the_game.CruiserPos.X, the_game.CruiserPos.Y + 1);
                }
            }
            else if (ks.IsKeyDown(Keys.Down))
            {
                if (btn != InputButton.Down)
                {
                    btn = InputButton.Down;
                    input_lag = 10;
                    if (the_game.CruiserPos.Y > 0) the_game.CruiserPos = new Point(the_game.CruiserPos.X, the_game.CruiserPos.Y - 1);
                }
                else if (input_lag == 0)
                {
                    if (the_game.CruiserPos.Y > 0) the_game.CruiserPos = new Point(the_game.CruiserPos.X, the_game.CruiserPos.Y - 1);
                }
            }
            else
            {
                btn = InputButton.None;
            }

            cruiser_anim_frame--;
            if (cruiser_anim_frame < 0)
            {
                cruiser_anim_frame += 120;
            }

            the_game.Cycle();

            base.Update(gameTime);
        }

        private enum InputButton
        {
            None, Left, Right, Up, Down, Switch
        }

        private int input_lag;
        private InputButton btn;

    }
}