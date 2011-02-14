using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;

namespace PanelsNet
{
    interface IBackdrop
    {
        /// <summary>
        /// Allocate any needed textures or other resources.
        /// </summary>
        /// <param name="content"></param>
        void Initialize(ContentManager content);

        /// <summary>
        /// Render to the device.
        /// </summary>
        /// <param name="device">Device</param>
        /// <param name="cycles">Number of frames to advance by</param>
        void Render(GraphicsDevice device, int cycles);

        /// <summary>
        /// Perform cleanup.
        /// </summary>
        void End();

        /// <summary>
        /// Triggers when a combo is made by the player.
        /// </summary>
        /// <param name="length">Number of blocks in the combo (always greater than 3)</param>
        void DoCombo(int length);

        /// <summary>
        /// Triggers when a chain is made by the player.
        /// </summary>
        /// <param name="length">Number of combos made in the chain so far (always greater than 1)</param>
        void DoChain(int length);

        void GarbageBounce(int height);
    }
}
