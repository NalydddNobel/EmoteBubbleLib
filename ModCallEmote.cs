using System;
using Terraria.Localization;
using Terraria.ModLoader;

namespace EmoteBubbleLib
{
    public sealed class ModCallEmote : ModEmote
    {
        public readonly string TexturePath;
        public readonly string Key;
        public readonly string InternalName;

        public override string Name => InternalName;
        public override string Texture => TexturePath;

        public override LocalizedText Command => Language.GetText(Key);
        public override LocalizedText SlashCommand => Language.GetText(Key);
        public Func<bool> CanNPCChatFunc;

        public override bool CanNPCChat()
        {
            return CanNPCChatFunc();
        }

        public ModCallEmote(string internalName, string key, string texture, Func<bool> canNPCChat = null)
        {
            TexturePath = texture;
            InternalName = internalName;
            Key = key;
            CanNPCChatFunc = canNPCChat ?? (() => true);
        }

        public override bool IsLoadingEnabled(Mod mod)
        {
            return false;
        }
    }
}