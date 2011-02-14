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
        // parent
        private BaseGame m_base_game;
        private GraphicsDevice m_graphics;

        // field object
        private Field m_field;

        // graphics resources
        private SpriteBatch m_sprite_batch;

        public Texture2D[,] blocks;
        public Texture2D c1;
        public Texture2D c2;

        // state logic
        private String note_text;
        private int note_show_time;

        private int cruiser_anim_frame;


        public SinglePlay(BaseGame game)
            : base(game)
        {
            m_base_game = game;
            m_graphics = game.graphics.GraphicsDevice;
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

            m_field = new Field(1, Difficulty.Easy);
            m_field.OnCombo += DoCombo;
            m_field.OnChain += DoChain;

            input_lag = 0;

            m_sprite_batch = new SpriteBatch(m_graphics);
            blocks = new Texture2D[6, 4];

            for (int x = 0; x < 4; x++)
            {
                blocks[0, x] = m_base_game.Content.Load<Texture2D>("red" + (x + 1).ToString());
                blocks[1, x] = m_base_game.Content.Load<Texture2D>("green" + (x + 1).ToString());
                blocks[2, x] = m_base_game.Content.Load<Texture2D>("teal" + (x + 1).ToString());
                blocks[3, x] = m_base_game.Content.Load<Texture2D>("yellow" + (x + 1).ToString());
                blocks[4, x] = m_base_game.Content.Load<Texture2D>("purple" + (x + 1).ToString());
                blocks[5, x] = m_base_game.Content.Load<Texture2D>("blue" + (x + 1).ToString());
            }

            c1 = m_base_game.Content.Load<Texture2D>("cruiser1");
            c2 = m_base_game.Content.Load<Texture2D>("cruiser2");

            base.Initialize();
        }

        protected void DoCombo(object sender, Field.PaneponEventArgs e)
        {
            note_text = e.Size.ToString() + " combo!";
            note_show_time = 60;
        }

        protected void DoChain(object sender, Field.PaneponEventArgs e)
        {
            note_text = e.Size.ToString() + " chain!";
            note_show_time = 60;
        }

        public void Render(GameTime gameTime)
        {
            m_base_game.graphics.GraphicsDevice.Clear(Color.CornflowerBlue);
            int w = m_graphics.Viewport.Width;
            int h = m_graphics.Viewport.Height;

            int BlockSize = h / 15;
            Point start = new Point(w / 2 - BlockSize * 3, h / 2 + BlockSize * 5);
            Color white = new Color(255, 255, 255);
            Color red = new Color(255, 0, 0);

            m_sprite_batch.Begin(SpriteBlendMode.AlphaBlend);

            Block theblock;

            int bx, by;

            // Draw the dark bottom row.
            Color grey = new Color(128, 128, 128);
            BlockColour thecolour;
            by = start.Y + BlockSize - ((m_field.LiftPhase * BlockSize) >> 16);
            for (int x = 0; x < 6; x++)
            {
                thecolour = m_field.BottomRow(x);

                bx = start.X + x * BlockSize;

                m_sprite_batch.Draw(blocks[(int)(thecolour) - 1, 0], new Rectangle(bx, by, BlockSize, BlockSize), grey);
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
                    theblock = m_field.BlockAt(x, y);
                    by = start.Y - (y * BlockSize) - ((m_field.LiftPhase * BlockSize) >> 16);

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
                        else if (!invis) m_sprite_batch.Draw(block_tex, new Rectangle(bx, by, BlockSize, BlockSize), block_col);
                    }
                }
            }

            m_sprite_batch.End();
            if (flashcount > 0)
            {
                BasicEffect effect = new BasicEffect(m_graphics, null);
                effect.TextureEnabled = true;
                effect.LightingEnabled = false;
                effect.DiffuseColor = new Vector3(1f, 1f, 1f);
                effect.AmbientLightColor = new Vector3(1f, 1f, 1f);
                effect.Alpha = 1f;
                effect.VertexColorEnabled = true;
                effect.Projection = Matrix.CreateOrthographicOffCenter(0, w, h, 0, 0, 1);

                m_graphics.RenderState.PointSize = BlockSize;

                effect.Begin();
                foreach (EffectPass pass in effect.CurrentTechnique.Passes)
                {
                    pass.Begin();
                    m_graphics.DrawUserPrimitives<VertexPositionColor>(PrimitiveType.PointList, pts.ToArray(), 0, flashcount);
                    pass.End();
                }
                effect.End();
            }
            m_sprite_batch.Begin();

            if (m_field.State == GameState.CruiserSlide)
            {
                if (m_field.Counter > 20)
                {
                    m_sprite_batch.Draw(c1, new Rectangle(start.X - BlockSize / 4 + (4 * BlockSize), (int)(start.Y - BlockSize / 8 - ((12 * 40 - (60 - m_field.Counter) * 7) * BlockSize) / 40 - ((m_field.LiftPhase * BlockSize) >> 16)), (5 * BlockSize) / 2, (5 * BlockSize) / 4), white);
                }
                else
                {
                    m_sprite_batch.Draw(c1, new Rectangle((int)(start.X - BlockSize / 4 + ((4 * 20 - (20 - m_field.Counter) * 2) * BlockSize) / 20), start.Y - BlockSize / 8 - (5 * BlockSize) - ((m_field.LiftPhase * BlockSize) >> 16), (5 * BlockSize) / 2, (5 * BlockSize) / 4), white);
                }
            }
            else if (m_field.State == GameState.Countdown)
            {
                if (cruiser_anim_frame % 2 == 0)
                {
                    m_sprite_batch.Draw((cruiser_anim_frame >= 60) ? c2 : c1, new Rectangle(start.X - BlockSize / 4 + (m_field.CruiserPos.X * BlockSize), start.Y - BlockSize / 8 - (m_field.CruiserPos.Y * BlockSize) - ((m_field.LiftPhase * BlockSize) >> 16), (5 * BlockSize) / 2, (5 * BlockSize) / 4), white);
                }
            }
            else if (m_field.State == GameState.Main)
            {
                m_sprite_batch.Draw((cruiser_anim_frame >= 60) ? c2 : c1, new Rectangle(start.X - BlockSize / 4 + (m_field.CruiserPos.X * BlockSize), start.Y - BlockSize / 8 - (m_field.CruiserPos.Y * BlockSize) - ((m_field.LiftPhase * BlockSize) >> 16), (5 * BlockSize) / 2, (5 * BlockSize) / 4), white);
            }
            else if (m_field.State == GameState.GameOver)
            {
            }

            if (note_show_time > 0)
            {
                m_sprite_batch.DrawString(m_base_game.sprite_font, note_text, new Vector2(40, 40), white);
            }

            m_sprite_batch.End();
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
                m_field.Lift();
            }
            if (ks.IsKeyDown(Keys.Space))
            {
                if (btn != InputButton.Switch)
                {
                    btn = InputButton.Switch;
                    m_field.Switch();
                }
            }
            else if (ks.IsKeyDown(Keys.Left))
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
            else if (ks.IsKeyDown(Keys.Right))
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
            else if (ks.IsKeyDown(Keys.Up))
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
            else if (ks.IsKeyDown(Keys.Down))
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

            cruiser_anim_frame--;
            if (cruiser_anim_frame < 0)
            {
                cruiser_anim_frame += 120;
            }

            m_field.Cycle();

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