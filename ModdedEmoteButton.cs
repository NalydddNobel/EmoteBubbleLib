using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.Localization;
using Terraria.UI;

namespace EmoteBubbleLib
{
    public class ModdedEmoteButton : EmoteButton
    {
        public Asset<Texture2D> IconTexture;
        public int EmoteType;
        public LocalizedText Text;
        public float gfxOffY;

        public int frameCounter;
        public bool hovered;

        public ModdedEmoteButton(int emoteIndex, Asset<Texture2D> icon, LocalizedText text) : base(emoteIndex)
        {
            EmoteType = emoteIndex;
            IconTexture = icon;
            Text = text;
        }

        public virtual Rectangle BubbleFrame()
        {
            return new Rectangle(-100, -100, 32, 32);
        }
        public virtual Rectangle GetIconFrame()
        {
            return new Rectangle(frameCounter > 10 ? (IconTexture.Value.Width / 2) : 0, 0, IconTexture.Value.Width / 2, IconTexture.Value.Height);
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            if (++frameCounter >= 20)
            {
                frameCounter = 0;
            }
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            base.DrawSelf(spriteBatch);
            var dimensions = GetDimensions();
            var position = dimensions.Position() + new Vector2(dimensions.Width / 2f, dimensions.Height / 2f + gfxOffY - 8f);
            var frame = GetIconFrame();

            spriteBatch.Draw(IconTexture.Value, position, frame, Color.White, 0f, frame.Size() / 2f, 1f, SpriteEffects.None, 0f);

            if (hovered)
            {
                Main.instance.MouseText(Text.Value, 0, 0);
            }
        }

        public override void MouseOver(UIMouseEvent evt)
        {
            base.MouseOver(evt);
            hovered = true;
        }

        public override void MouseOut(UIMouseEvent evt)
        {
            base.MouseOut(evt);
            hovered = false;
        }
    }
}