using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using Gamefloor.Framework;

namespace PanelsNet
{
    public class Field : GameComponent
    {
        private Block[,] m_field;
        private BlockColour[,] m_below_field; // Dimmed blocks yet to be raised to the field.

        private ushort m_lift_phase; // 0-255 value indicating how far up the bottom dimmed row is.
        private Point m_cruiser_pos;
        private bool m_fast_lifting;

        private GameState m_state;
        private uint m_counter; // General purpose game counter used depending on game state
        private uint m_chain_length;

        private int m_level;
        private Difficulty m_difficulty;
        private int m_score;
        private ushort m_lift_speed;
        private uint m_flash_frames, m_face_frames, m_vanish_speed;

        private bool freeze_lift;

        private Random therand;
        const uint field_height = 12;

        public Point CruiserPos
        {
            get
            {
                return m_cruiser_pos;
            }
            set
            {
                if ((value.X > 4) || (value.Y > 11) || (value.X < 0) || (value.Y < 0)) throw new ArgumentOutOfRangeException("Moved Cruiser outside the field");
                m_cruiser_pos = value;
            }
        }

        public Block BlockAt(int X, int Y)
        {
            if ((X > 5) || (Y >= field_height)) throw new ArgumentOutOfRangeException();
            return m_field[X, Y];
        }

        public BlockColour BottomRow(int X)
        {
            if (X > 5) throw new ArgumentOutOfRangeException();
            return m_below_field[X, 1];
        }

        public ushort LiftPhase
        {
            get
            {
                return m_lift_phase;
            }
        }

        public GameState State
        {
            get
            {
                return m_state;
            }
        }

        public uint Counter
        {
            get
            {
                return m_counter;
            }
        }

        public Field(int Level, Difficulty difficulty)
        {
            therand = new Random(); // Good enough randomization for now?

            m_difficulty = difficulty;

            RandomField();

            m_below_field = new BlockColour[6, 2];

            for (int x = 0; x < 6; x++)
            {
                m_below_field[x, 0] = m_field[x, 0].Colour;
            }
            MakeBelowRow();
            MakeBelowRow();

            m_lift_phase = 0;
            m_cruiser_pos = new Point(2, 5);
            m_state = GameState.CruiserSlide;
            m_counter = 60;
            m_level = Level;
            m_score = 0;
            m_fast_lifting = false;
            m_chain_length = 1;

            CalcLiftSpeed();
            CalcFlashFrames();
        }

        public bool Switch()
        {
            Block b1, b2;
            bool result = false;
            b1 = m_field[m_cruiser_pos.X, m_cruiser_pos.Y];
            b2 = m_field[m_cruiser_pos.X + 1, m_cruiser_pos.Y];
            if (m_state == GameState.Main) // Game is in play mode
            {
                if ((b1.State == BlockState.Normal) ||
                (b1.State == BlockState.ComboFall) ||
                (b1.State == BlockState.Fall)) // Left block is movable
                {
                    if ((b2.State == BlockState.Normal) ||
                        (b2.State == BlockState.ComboFall) ||
                        (b2.State == BlockState.Fall)) // Right block is movable
                    {
                        if ((b1.Colour != BlockColour.Garbage) &&
                            (b2.Colour != BlockColour.Garbage) &&
                            (b1.Colour != BlockColour.ShockGarbage) && // Not trying to move the bottom-left garbage control block
                            (b2.Colour != BlockColour.ShockGarbage))
                        {
                            if ((b1.Colour == BlockColour.Empty) && (b2.Colour != BlockColour.Empty))
                            {
                                Block b3 = m_field[m_cruiser_pos.X, m_cruiser_pos.Y + 1];
                                if ((b3.State != BlockState.SwitchRight) && (b3.State != BlockState.SwitchLag) && (b3.State != BlockState.ComboLag)) // Disallow switching into empty space if a switch is happening above
                                {
                                    result = true;
                                }
                            }
                            else if ((b2.Colour == BlockColour.Empty) && (b1.Colour != BlockColour.Empty))
                            {
                                Block b3 = m_field[m_cruiser_pos.X + 1, m_cruiser_pos.Y + 1];
                                if ((b3.State != BlockState.SwitchLeft) && (b3.State != BlockState.SwitchLag) && (b3.State != BlockState.ComboLag)) // Disallow switching into empty space if a switch is happening above
                                {
                                    result = true;
                                }
                            }
                            else if ((b1.Colour != BlockColour.Empty) &&
                                     (b2.Colour != BlockColour.Empty))
                            {
                                result = true;
                            }
                        }
                    }
                }
            }

            if (result)
            {
                b1.State = BlockState.SwitchRight; // Begin a switch. (Lag & stuff handled in game cycle once the switch finishes)
                b1.Parameter = 3;
                b2.State = BlockState.SwitchLeft;
                b2.Parameter = 3;
                m_field[m_cruiser_pos.X, m_cruiser_pos.Y] = b1;
                m_field[m_cruiser_pos.X + 1, m_cruiser_pos.Y] = b2;
            }
            return result;
        }

        public void Cycle()
        {
            switch (m_state)
            {
                case GameState.CruiserSlide:
                    m_counter--;
                    if (m_counter == 0)
                    {
                        m_state = GameState.Countdown;
                        m_counter = 240;
                    }
                    break;
                case GameState.Countdown:
                    m_counter--;
                    if (m_counter == 0)
                    {
                        m_state = GameState.Main;
                    }
                    break;
                case GameState.Main:
                    Block theblock;
                    freeze_lift = false;

                    // === 1: Process blocks involved in combos, perform lag timing ===
                    for (int y = 0; y < field_height; y++)
                    {
                        for (int x = 0; x < 6; x++)
                        {
                            theblock = m_field[x, y];

                            switch (theblock.State)
                            {
                                case BlockState.Vanishing:
                                case BlockState.ComboVanishing:
                                    freeze_lift = true;
                                    if (theblock.Parameter > 0) theblock.Parameter--;
                                    else if (theblock.FaceTime == 1)
                                    {
                                        // TODO: Forward an event to draw flashy graphics & sound for a block disappearing.
                                        theblock.FaceTime--;
                                    }
                                    else if (theblock.FaceTime > 0) theblock.FaceTime--;
                                    else if (theblock.InvisTime > 0) theblock.InvisTime--;
                                    else
                                    {
                                        theblock.Colour = BlockColour.Empty;
                                        theblock.State = BlockState.Normal;
                                        if (y < field_height - 1)
                                        {
                                            SetLagColumn(BlockState.ComboLag, x, y + 1, 1);
                                        }
                                    }
                                    break;
                                case BlockState.SwitchLag:
                                    freeze_lift = true;
                                    if (y < 11)
                                    {
                                        Block b3 = m_field[x, y + 1];
                                        if ((b3.State != BlockState.SwitchLag) && (b3.State != BlockState.ComboLag))
                                        {
                                            // A block has been dropped on top of a laggy block. Give it a fresh lag.
                                            // If this falling block was energized from a combo, it stays that way.
                                            SetLagColumn((b3.State == BlockState.ComboFall) ? BlockState.ComboLag : BlockState.SwitchLag, x, y + 1, 0);
                                        }
                                    }
                                    if (theblock.Parameter > 0) theblock.Parameter--;
                                    else
                                    {
                                        theblock.State = BlockState.Normal; // Set to NORMAL instead of Fall to permit ninja chaining to work.
                                    }
                                    break;
                                case BlockState.ComboLag:
                                    freeze_lift = true;
                                    if (y < 11)
                                    {
                                        Block b3 = m_field[x, y + 1];
                                        if ((b3.State != BlockState.SwitchLag) && (b3.State != BlockState.ComboLag))
                                        {
                                            // A block has been dropped on top of a laggy block. Give it a fresh lag.
                                            // If this falling block was energized from a combo, it stays that way.
                                            SetLagColumn((b3.State == BlockState.ComboFall) ? BlockState.ComboLag : BlockState.SwitchLag, x, y + 1, 0);
                                        }
                                    }
                                    if (theblock.Parameter > 0) theblock.Parameter--;
                                    else
                                    {
                                        theblock.State = BlockState.ComboFall;
                                    }
                                    break;
                            }
                            m_field[x, y] = theblock;
                        }
                    }

                    // === 2: Process block falling, switching ===
                    for (int y = 0; y < field_height; y++)
                    {
                        for (int x = 0; x < 6; x++)
                        {
                            theblock = m_field[x, y];

                            switch (theblock.State)
                            {
                                case BlockState.Fall: // If a block is SEEN in this state it has landed. (Otherwise it would have slipped through the cracks)
                                    freeze_lift = true;
                                    if (y > 0) // there is a laggy or switching block further down the stack
                                    {
                                        Block lowblock = m_field[x, y - 1];
                                        if ((lowblock.State == BlockState.SwitchLeft) || (lowblock.State == BlockState.SwitchRight) ||
                                            (lowblock.State == BlockState.SwitchLag) || (lowblock.State == BlockState.ComboLag) ||
                                            (lowblock.State == BlockState.Fall) || (lowblock.State == BlockState.ComboFall))
                                        {
                                            goto case BlockState.Normal;
                                        }
                                    }
                                    theblock.State = BlockState.Normal;
                                    theblock.Parameter = 6; // Bounce
                                    goto case BlockState.Normal;
                                case BlockState.ComboFall:
                                    freeze_lift = true;
                                    if (y > 0) // there is a laggy or switching block further down the stack
                                    {
                                        Block lowblock = m_field[x, y - 1];
                                        if ((lowblock.State == BlockState.SwitchLeft) || (lowblock.State == BlockState.SwitchRight) ||
                                            (lowblock.State == BlockState.SwitchLag) || (lowblock.State == BlockState.ComboLag) ||
                                            (lowblock.State == BlockState.Fall) || (lowblock.State == BlockState.ComboFall))
                                        {
                                            goto case BlockState.Normal;
                                        }
                                    }
                                    theblock.State = BlockState.ComboLanded;
                                    theblock.Parameter = 6; // Bounce
                                    goto case BlockState.Normal;
                                case BlockState.Normal:
                                    if (theblock.Colour == BlockColour.Empty)
                                    {
                                        if (y < field_height - 1)
                                        {
                                            Block highblock = m_field[x, y + 1];
                                            if (((int)(highblock.Colour) > 0) && ((int)(highblock.Colour) < 8))
                                            {
                                                if ((highblock.State == BlockState.Normal) || (highblock.State == BlockState.Fall) || (highblock.State == BlockState.ComboFall))
                                                {
                                                    // A block with space below it: move it down by one.
                                                    freeze_lift = true;
                                                    theblock = highblock;
                                                    if (theblock.State == BlockState.Normal) theblock.State = BlockState.Fall;
                                                    else if (theblock.State == BlockState.ComboLanded) theblock.State = BlockState.ComboFall;
                                                    m_field[x, y + 1] = new Block();
                                                }
                                            }
                                            else if ((highblock.Colour == BlockColour.Garbage) || (highblock.Colour == BlockColour.ShockGarbage))
                                            {
                                                throw new NotImplementedException("Garbage blocks you crazy??");
                                            }
                                        }
                                    }
                                    else if ((int)(theblock.Colour) < 8)
                                    {
                                        if (theblock.Parameter > 0)
                                        {
                                            theblock.Parameter--;
                                        }
                                    }
                                    else
                                    {
                                        throw new NotImplementedException("Garbage blocks you crazy??");
                                    }
                                    break;
                                case BlockState.SwitchLeft:
                                case BlockState.SwitchRight:
                                    if (m_field[x, y + 1].State == BlockState.ComboFall)
                                    {
                                        Block b2 = m_field[x, y + 1];
                                        b2.State = BlockState.ComboLag;
                                        if (m_difficulty < Difficulty.Normal) b2.Parameter = 9;
                                        else if (m_difficulty > Difficulty.Normal) b2.Parameter = 5;
                                        else b2.Parameter = 7;
                                        m_field[x, y + 1] = b2;
                                    }
                                    if (theblock.Parameter > 0)
                                    {
                                        theblock.Parameter--;
                                    }
                                    else
                                    {
                                        Block rightblock = theblock; // RightBlock: Block going TO the right.
                                        theblock = m_field[x + 1, y]; // Has to be safe or stuff has gone horribly wrong.

                                        theblock.State = BlockState.Normal;
                                        rightblock.State = BlockState.Normal;
                                        if (y > 0)
                                        {
                                            // Only lag the block if it's slid to over empty space.
                                            if (m_field[x, y - 1].Colour == BlockColour.Empty)
                                            {
                                                theblock.State = BlockState.SwitchLag;
                                                theblock.Parameter = LagTime();
                                            }
                                            if (m_field[x + 1, y - 1].Colour == BlockColour.Empty)
                                            {
                                                rightblock.State = BlockState.SwitchLag;
                                                rightblock.Parameter = LagTime();
                                            }
                                        }

                                        // Set lag when switching a block out of a column
                                        if (x < 11)
                                        {
                                            if (theblock.Colour == BlockColour.Empty)
                                            {
                                                Block hiblock = m_field[x, y + 1];
                                                hiblock.State = BlockState.SwitchLag;
                                                hiblock.Parameter = LagTime();
                                                m_field[x, y + 1] = hiblock;
                                            }
                                            else if (rightblock.Colour == BlockColour.Empty)
                                            {
                                                Block hiblock = m_field[x + 1, y + 1];
                                                hiblock.State = BlockState.SwitchLag;
                                                hiblock.Parameter = LagTime();
                                                m_field[x + 1, y + 1] = hiblock;
                                            }
                                        }

                                        m_field[x + 1, y] = rightblock;
                                    }
                                    break;
                            }

                            m_field[x, y] = theblock;
                        }
                    }

                    // === 3: Find combos ===

                    bool chain_active = false; // Set true if any blocks energized by combos are on the screen.
                    bool is_chain_combo = false; // Set true if a row of blocks found on this frame are part of a chain.

                    for (int y = 0; y < field_height; y++)
                    {
                        for (int x = 0; x < 6; x++)
                        {
                            theblock = m_field[x, y];
                            Block block1, block2;
                            bool chain_this_try = false;

                            if (((int)(theblock.Colour) > 0) && ((int)(theblock.Colour) < 8))
                            {
                                switch (theblock.State)
                                {
                                    case BlockState.ComboLanded:
                                        chain_this_try = true;
                                        goto case BlockState.Normal;
                                    case BlockState.VanishMarked:
                                    case BlockState.Normal:
                                        // 1. Horizontal check.
                                        if (x < 4)
                                        {
                                            block1 = m_field[x + 1, y];
                                            block2 = m_field[x + 2, y];

                                            if ((block1.Colour == theblock.Colour) &&
                                                (block2.Colour == theblock.Colour) &&
                                                ((block1.State == BlockState.Normal) ||
                                                 (block1.State == BlockState.ComboLanded) ||
                                                 (block1.State == BlockState.VanishMarked)) &&
                                                ((block2.State == BlockState.Normal) ||
                                                 (block2.State == BlockState.ComboLanded) ||
                                                 (block2.State == BlockState.VanishMarked)))
                                            {
                                                if ((block1.State == BlockState.ComboLanded) || (block2.State == BlockState.ComboLanded) || chain_this_try)
                                                {
                                                    is_chain_combo = true;
                                                    chain_active = true;
                                                }

                                                theblock.State = BlockState.VanishMarked;
                                                block1.State = BlockState.VanishMarked;
                                                block2.State = BlockState.VanishMarked;

                                                m_field[x, y] = theblock;
                                                m_field[x + 1, y] = block1;
                                                m_field[x + 2, y] = block2;
                                            }
                                        }
                                        if (y < field_height - 3)
                                        {
                                            block1 = m_field[x, y + 1];
                                            block2 = m_field[x, y + 2];

                                            if ((block1.Colour == theblock.Colour) &&
                                                (block2.Colour == theblock.Colour) &&
                                                ((block1.State == BlockState.Normal) ||
                                                 (block1.State == BlockState.ComboLanded) ||
                                                 (block1.State == BlockState.VanishMarked)) &&
                                                ((block2.State == BlockState.Normal) ||
                                                 (block2.State == BlockState.ComboLanded) ||
                                                 (block2.State == BlockState.VanishMarked)))
                                            {
                                                if ((block1.State == BlockState.ComboLanded) || (block2.State == BlockState.ComboLanded) || chain_this_try)
                                                {
                                                    is_chain_combo = true;
                                                    chain_active = true;
                                                }

                                                theblock.State = BlockState.VanishMarked;
                                                block1.State = BlockState.VanishMarked;
                                                block2.State = BlockState.VanishMarked;

                                                m_field[x, y] = theblock;
                                                m_field[x, y + 1] = block1;
                                                m_field[x, y + 2] = block2;
                                            }
                                        }
                                        break;
                                }
                            }

                            // === 4: Chain keep-alive check ===
                            switch (theblock.State)
                            {
                                case BlockState.ComboFall:
                                case BlockState.ComboLag:
                                case BlockState.ComboVanishing:
                                    chain_active = true;
                                    break;
                                case BlockState.ComboLanded:
                                    theblock.State = BlockState.Normal;
                                    m_field[x, y] = theblock;
                                    break;
                            }
                        }
                    }

                    // === 4: Count blocks involved in the combo on this frame ===
                    uint BlockCount = 0;
                    Point ch_pos = new Point(0, 0);
                    for (int y = 0; y < field_height; y++)
                    {
                        for (int x = 0; x < 6; x++)
                        {
                            if (m_field[x, y].State == BlockState.VanishMarked)
                            {
                                BlockCount++; // Maybe I can do this in the above loop but the logic is stupid

                                // Kind of odd. Picks the rightmost block in the topmost row and declares it the important block for this combo/chain event
                                // Replace with a more authentic approach.
                                ch_pos.X = x;
                                ch_pos.Y = y;
                            }
                        }
                    }

                    uint Delayer = 1;

                    // === 5: Set this frame's combo's blocks to the vanishing state ===
                    for (int y = ((int)field_height) - 1; y >= 0; y--)
                    {
                        for (int x = 0; x < 6; x++)
                        {
                            theblock = m_field[x, y];
                            if (theblock.State == BlockState.VanishMarked)
                            {
                                theblock.State = is_chain_combo ? BlockState.ComboVanishing : BlockState.Vanishing;
                                theblock.Parameter = m_flash_frames;
                                theblock.FaceTime = m_face_frames + Delayer * m_vanish_speed;
                                theblock.InvisTime = (BlockCount - Delayer) * m_vanish_speed;
                                theblock.ComboOrdinal = Delayer;
                                Delayer++;
                            }
                            m_field[x, y] = theblock;
                        }
                    }

                    if (!freeze_lift)
                    {
                        ushort liftold = m_lift_phase;
                        m_lift_phase += m_fast_lifting ? (ushort)4096 : m_lift_speed;
                        if (m_cruiser_pos.Y >= 11) m_cruiser_pos.Y = 10;

                        if (m_lift_phase < liftold)
                        {
                            m_lift_phase = 0;
                            RaiseField();
                        }
                    }

                    if (BlockCount > 3) // Combo event
                    {
                        if (OnCombo != null) OnCombo(this, new PaneponEventArgs(ch_pos, BlockCount));
                    }

                    if (is_chain_combo) // Chain event
                    {
                        m_chain_length++;
                        if (OnChain != null) OnChain(this, new PaneponEventArgs(ch_pos, m_chain_length));
                    }

                    // TODO: Scoring based on BlockCount, m_chain_length, and m_level

                    if (!chain_active) m_chain_length = 1;

                    break;
            }
        }

        public void Lift()
        {
            if (!freeze_lift) m_fast_lifting = true;
        }

        public delegate void ComboEvent(object sender, PaneponEventArgs e);
        public delegate void ChainEvent(object sender, PaneponEventArgs e);

        public event ComboEvent OnCombo;
        public event ChainEvent OnChain;

        public class PaneponEventArgs : EventArgs
        {
            private Point m_location;
            private uint m_size;

            public PaneponEventArgs(Point location, uint size)
            {
                m_location = location;
                m_size = size;
            }

            public Point Location
            {
                get
                {
                    return m_location;
                }
            }

            public uint Size
            {
                get
                {
                    return m_size;
                }
            }
        }

        private void SetLagColumn(BlockState state, int x, int height_start, int lag_bias)
        {
            for (int z = height_start; z < field_height; z++) // Set all higher up blocks in the stack to ComboFall mode.
            {
                Block highblock2 = m_field[x, z];
                if (highblock2.Colour == BlockColour.Empty) break;
                if (highblock2.State == BlockState.Normal)
                {
                    highblock2.State = state;
                    highblock2.Parameter = (uint)(LagTime() + lag_bias);

                    m_field[x, z] = highblock2;
                }
                else break; // Stop if another vanishing block or garbage
            }
        }

        private uint LagTime()
        {
            if (m_difficulty < Difficulty.Normal) return 8;
            else if (m_difficulty > Difficulty.Normal) return 4;
            else return 6;
        }

        private void CalcFlashFrames()
        {
            switch (m_difficulty)
            {
                case Difficulty.S_Easy:
                case Difficulty.Easy:
                    m_flash_frames = 49;
                    m_face_frames = 17;
                    m_vanish_speed = 8;
                    break;
                case Difficulty.Normal:
                    m_flash_frames = 41;
                    m_face_frames = 11;
                    m_vanish_speed = 6;
                    break;
                default:
                    m_flash_frames = 33;
                    m_face_frames = 5;
                    m_vanish_speed = 4;
                    break;
            }
        }

        private void RaiseField()
        {
            for (int y = ((int)field_height) - 1; y > 0; y--)
            {
                for (int x = 0; x < 6; x++)
                {
                    m_field[x, y] = m_field[x, y - 1];
                }
            }
            for (int x = 0; x < 6; x++)
            {
                m_field[x, 0] = new Block { Colour = m_below_field[x, 1] };
            }
            MakeBelowRow();
            m_cruiser_pos.Y++;
            m_fast_lifting = false;
        }

        private void MakeBelowRow()
        {
            for (int x = 0; x < 6; x++)
            {
                m_below_field[x, 1] = m_below_field[x, 0];
            }

            m_below_field[0, 0] = RandomColour(m_difficulty, m_below_field[0, 1]);
            for (int x = 1; x < 6; x++)
            {
                m_below_field[x, 0] = RandomColour(m_difficulty, m_below_field[x, 1], m_below_field[x - 1, 0]);
            }
        }

        private void RandomField()
        {
            m_field = new Block[6, field_height];

            int[] columns = new int[6];

            int y;

            // Random heights but always 30 blocks
            for (int x = 0; x < 30; x++) // TODO: Optimize
            {
                do
                {
                    y = therand.Next(0, 6);
                } while (columns[y] >= 8);
                columns[y]++;
            }

            // Random blocks within
            if (columns[0] > 0)
            {
                m_field[0, 0].Colour = RandomColour(m_difficulty);
                for (int z = 1; z < columns[0]; z++)
                {
                    m_field[0, z].Colour = RandomColour(m_difficulty, m_field[0, z - 1].Colour);
                }
            }
            for (int x = 1; x < 6; x++)
            {
                if (columns[x] > 0)
                {
                    m_field[x, 0].Colour = RandomColour(m_difficulty, m_field[x - 1, 0].Colour);
                    for (int z = 1; z < columns[x]; z++)
                    {
                        m_field[x, z].Colour = RandomColour(m_difficulty, m_field[x, z - 1].Colour, m_field[x - 1, z].Colour);
                    }
                }
            }
        }

        /// <summary>
        /// Picks a random block colour which is not the same as any of the passed colours.
        /// </summary>
        private BlockColour RandomColour(Difficulty diff, BlockColour n1, BlockColour n2)
        {
            int count;
            int bump1 = 10, bump2 = 10; // Initially higher than any possible value so they don't interfere

            if ((n1 == BlockColour.Empty) && (n2 == BlockColour.Empty))
            {
                count = (diff > Difficulty.Normal) ? 7 : 6;
            }
            else if ((n1 == n2) || (n2 == BlockColour.Empty) || (n1 == BlockColour.Empty))
            {
                count = (diff > Difficulty.Normal) ? 6 : 5;
                bump1 = (int)n1;
            }
            else
            {
                count = (diff > Difficulty.Normal) ? 5 : 4;
                bump1 = (int)n1;
                bump2 = (int)n2;
                if (bump1 > bump2) // sort
                {
                    int w = bump2;
                    bump2 = bump1;
                    bump1 = w;
                }
            }

            int result = therand.Next(1, count);
            if (result >= bump1) result++;
            if (result >= bump2) result++;
            return (BlockColour)(result);
        }

        private BlockColour RandomColour(Difficulty diff, BlockColour n1)
        {
            return RandomColour(diff, n1, BlockColour.Empty);
        }

        private BlockColour RandomColour(Difficulty diff)
        {
            return RandomColour(diff, BlockColour.Empty, BlockColour.Empty);
        }

        private void CalcLiftSpeed()
        {
            m_lift_speed = (ushort)(20 + 20*(ushort)m_level); // Temporary until I find a good formula.
        }
    }

    public struct Block
    {
        public BlockColour Colour;
        public BlockState State;
        public byte Width, Height; // For garbage blocks
        public uint Parameter; // Timing for switches, lags, flashing, and bounce anims. Decreases towards 0.
        public uint FaceTime; // Time a vanishing block spends at the Face phase
        public uint InvisTime; // Time a vanishing block spends at the Invisible time
        public uint ComboOrdinal;
    }

    public enum BlockColour
    {
        Empty = 0, Red = 1, Green = 2, Teal = 3, Yellow = 4, Purple = 5, Blue = 6, Shock, Garbage, ShockGarbage
    }

    public enum BlockState
    {
        Normal = 0,
        SwitchLeft, SwitchRight,
        SwitchLag, // SwitchLag is only set if the block is switched to over a void.
        ComboLag, // Set for blocks above a combo about to fall. Here, "combos" include rows of 3.
        Fall, // This state's ONLY purpose is to make the block bounce when it hits the ground. =3
        ComboFall, // Special flag for blocks falling because of a move made
        Vanishing,
        ComboVanishing, // True if ComboFalling blocks were present in this combo. This value will keep a combo alive.
        ComboLanded, // Set only inside frame processing. Indicates a block is on the ground so can participate in combos but hasn't lost its combo magic yet.
        VanishMarked, // Set only inside frame processing. Indicates that this block is part of a new combo.
        GarbagePart
    }

    public enum GameState
    {
        CruiserSlide, Countdown, Main, GameOver
    }

    public enum Difficulty
    {
        S_Easy, Easy, Normal, Hard, V_Hard, S_Hard, Intense
    }
}
