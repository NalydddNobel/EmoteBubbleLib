using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace EmoteBubbleLib
{
    public abstract class ModEmote : ModTexturedType
    {
        public Asset<Texture2D> Icon { get; internal set; }
        public int Type { get; internal set; }

        public abstract LocalizedText Command { get; }
        /// <summary>
        /// Please include a "/" at the beginning of the text. Also for consistency, it should be the same as <see cref="Command"/>
        /// </summary>
        public abstract LocalizedText SlashCommand { get; }

        public float gfxOffY;
        public EmoteListing Listing;

        protected LocalizedText CreateName(string key)
        {
            return Language.GetText(key);
        }

        protected sealed override void Register()
        {
        }

        public override void SetupContent()
        {
            EmoteBubbleLib.AddEmote(this);
            SetStaticDefaults();
        }

        public virtual bool CanNPCChat()
        {
            return true;
        }

        public virtual void Draw(EmoteBubble emote, SpriteBatch spriteBatch)
        {
            EmoteBubbleLib.GetDrawParams(emote, out var drawPosition, out var origin, out var effects, out bool dontDrawIcon);
            DrawBubble(emote, spriteBatch, drawPosition, origin, effects, !dontDrawIcon);
            if (!dontDrawIcon)
            {
                InnerDrawIcon(emote, spriteBatch, drawPosition + new Vector2(0f, gfxOffY - 8f), origin, effects);
            }
        }
        protected virtual void InnerDrawIcon(EmoteBubble emote, SpriteBatch spriteBatch, Vector2 drawPosition, Vector2 origin, SpriteEffects effects)
        {
            spriteBatch.Draw(Icon.Value, drawPosition, Icon.Frame(2, 1, emote.frame, 0), Color.White, 0f, origin, 1f, effects, 0f);
        }
        /// <summary>
        /// Helper method which draws the emote bubble
        /// </summary>
        /// <param name="emote"></param>
        /// <param name="spriteBatch"></param>
        protected void DrawBubble(EmoteBubble emote, SpriteBatch spriteBatch, Vector2 drawPosition, Vector2 origin, SpriteEffects effects, bool drawIcon)
        {
            spriteBatch.Draw(TextureAssets.Extra[ExtrasID.EmoteBubble].Value, drawPosition, 
                TextureAssets.Extra[ExtrasID.EmoteBubble].Frame(8, 39, drawIcon ? 1 : 0, 0), Color.White, 0f, 
                origin, 1f, effects, 0f);
        }

        /// <summary>
        /// Always draws even if <see cref="GlobalEmote.PreDraw(EmoteBubble, SpriteBatch)"/> returns false. Runs after <see cref="Draw(EmoteBubble, SpriteBatch)"/>
        /// </summary>
        public virtual void SpecialDraw(EmoteBubble emote, SpriteBatch spriteBatch)
        {
        }

        public virtual EmoteButton ProvideEmoteButton(EmoteButton original)
        {
            return new ModdedEmoteButton(Type, Icon, SlashCommand)
            {
                HAlign = original.HAlign,
                VAlign = original.VAlign,
                Top = original.Top,
                Left = original.Left,
                gfxOffY = gfxOffY,
            };
        }
    }
}