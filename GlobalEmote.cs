using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent.UI;
using Terraria.ModLoader;

namespace EmoteBubbleLib
{
    public abstract class GlobalEmote : ModType
    {
        protected sealed override void Register()
        {
        }

        public override void SetupContent()
        {
            EmoteBubbleLib.globalEmotes.Add(this);
            SetStaticDefaults();
        }

        /// <summary>
        /// Determines whether or not a boss is active. This makes npcs spawn combat related chat bubbles instead of regular ones
        /// </summary>
        /// <returns>Return null for vanilla behavior</returns>
        public virtual bool? IsBossActive()
        {
            return null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="emotes">A list of emote IDs which will randomly be chosen</param>
        /// <param name="bubble">The emote bubble instance</param>
        /// <param name="anchor">The entity UI anchor</param>
        /// <param name="player">Closest player to the emote bubble</param>
        /// <param name="bossActive">Whether or not a boss is active</param>
        public virtual void ModifyEmoteChoices(List<int> emotes, EmoteBubble bubble, WorldUIAnchor anchor, Player player, bool bossActive)
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="emote">The emote bubble</param>
        /// <param name="spriteBatch"></param>
        /// <returns>Whether or not to draw the vanilla emote bubble or <see cref="ModEmote.Draw"/></returns>
        public virtual bool PreDraw(EmoteBubble emote, SpriteBatch spriteBatch)
        {
            return true;
        }

        /// <summary>
        /// Runs even if <see cref="PreDraw(EmoteBubble, SpriteBatch)"/> returns false. Runs after <see cref="ModEmote.Draw(EmoteBubble, SpriteBatch)"/> or the vanilla <see cref="EmoteBubble.Draw(SpriteBatch)"/>
        /// </summary>
        /// <param name="emote">The emote bubble</param>
        /// <param name="spriteBatch"></param>
        public virtual void SpecialDraw(EmoteBubble emote, SpriteBatch spriteBatch)
        {
        }
    }
}