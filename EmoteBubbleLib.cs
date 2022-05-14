using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Terraria;
using Terraria.Chat;
using Terraria.Chat.Commands;
using Terraria.GameContent;
using Terraria.GameContent.UI;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.Initializers;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI;
using Terraria.UI.Chat;

namespace EmoteBubbleLib
{
	public sealed class EmoteBubbleLib : Mod
	{
		public static int EmoteCount { get; private set; }
        public static int ModEmoteCount => registeredEmotes.Length;
        private static ModEmote[] registeredEmotes;
        internal static List<GlobalEmote> globalEmotes;
        internal static bool EmotesSetup { get; private set; }

        public override object Call(params object[] args)
        {
            switch ((string)args[0])
            {
                // Parameter 1 - Mod - Mod: The mod for this emote
                // Parameter 2 - string - Internal Name: The internal name of this emote
                // Parameter 3 - string - Name: The name (aka command) of this emote. For consistency, use all lowercase
                // Parameter 4 - string - Text: The texture path for this emote, should be 64x32, with 2 horizontal frames, for consistency 
                // Parameter 5 (OPTIONAL) - byte - Emote Listing, check EmoteListing.cs for a list, defaults to None
                // Parameter 6 (OPTIONAL) - Func<bool> - Whether or not town NPCs can naturally use this emote. Defaults to always true
                // Parameter 7 (OPTIONAL) - float - gfxOffY: Adjusts the y position of the emote. You can probably just do this in the texture though. Defaults to 8
                case "AddEmote":
                    {
                        var call = new ModCallEmote(((Mod)args[1]).Name + "/" + (string)args[2], (string)args[3], (string)args[4], args.Length > 6 ? (Func<bool>)args[6] : null);
                        if (args.Length > 5)
                        {
                            try
                            {
                                call.Listing = (EmoteListing)(byte)args[5];
                            }
                            catch
                            {
                                throw new Exception("Parameter 5 was not of type 'byte'. Please cast the input number as (byte){Number}");
                            }
                        }
                        if (args.Length > 7)
                        {
                            call.gfxOffY = (float)args[7];
                        }
                        return AddEmote(call);
                    }
            }

            return null;
        }

        public override void Load()
        {
            EmotesSetup = false;
            EmoteCount = EmoteID.Count;
            registeredEmotes = Array.Empty<ModEmote>();
            globalEmotes = new List<GlobalEmote>();
            On.Terraria.GameContent.UI.Elements.EmotesGroupListItem.ctor += EmotesGroupListItem_ctor;
            On.Terraria.GameContent.UI.Elements.EmoteButton.GetFrame += EmoteButton_GetFrame;
            On.Terraria.GameContent.UI.States.UIEmotesMenu.GetEmotesBosses += UIEmotesMenu_GetEmotesBosses;
            On.Terraria.GameContent.UI.States.UIEmotesMenu.GetEmotesCritters += UIEmotesMenu_GetEmotesCritters;
            On.Terraria.GameContent.UI.States.UIEmotesMenu.GetEmotesTownNPCs += UIEmotesMenu_GetEmotesTownNPCs;
            On.Terraria.GameContent.UI.States.UIEmotesMenu.GetEmotesBiomesAndEvents += UIEmotesMenu_GetEmotesBiomesAndEvents;
            On.Terraria.GameContent.UI.States.UIEmotesMenu.GetEmotesItems += UIEmotesMenu_GetEmotesItems;
            On.Terraria.GameContent.UI.States.UIEmotesMenu.GetEmotesRPS += UIEmotesMenu_GetEmotesRPS;
            On.Terraria.GameContent.UI.States.UIEmotesMenu.GetEmotesGeneral += UIEmotesMenu_GetEmotesGeneral;
            IL.Terraria.GameContent.UI.EmoteBubble.PickNPCEmote += EmoteBubble_PickNPCEmote;
            On.Terraria.GameContent.UI.EmoteBubble.Draw += EmoteBubble_Draw;
            On.Terraria.Initializers.ChatInitializer.PrepareAliases += ChatInitializer_PrepareAliases;

            //Call("AddEmote", this, "OmegaStarite", "omegastaritecommand", "EmoteBubbleLib/ModEmoteTest");
            //Call("AddEmote", this, "OmegaStarite2", "omegastaritecommand2", "EmoteBubbleLib/ModEmoteTest", (byte)1);
            //Call("AddEmote", this, "OmegaStarite3", "omegastaritecommand3", "EmoteBubbleLib/ModEmoteTest", (byte)2);
            //Call("AddEmote", this, "OmegaStarite4", "omegastaritecommand4", "EmoteBubbleLib/ModEmoteTest", (byte)3, () => { Main.NewText("This is some random text"); return true; });
            //Call("AddEmote", this, "OmegaStarite5", "omegastaritecommand5", "EmoteBubbleLib/ModEmoteTest", (byte)3, () => { Main.NewText("This is some random text 2"); return true; }, 28f);
        }

        #region Hooks
        private void EmotesGroupListItem_ctor(On.Terraria.GameContent.UI.Elements.EmotesGroupListItem.orig_ctor orig, EmotesGroupListItem self, LocalizedText groupTitle, int groupIndex, int maxEmotesPerRow, int[] emotes)
        {
            orig(self, groupTitle, groupIndex, maxEmotesPerRow, emotes);
            var elements = (List<UIElement>)typeof(UIElement).GetField("Elements", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(self);

            for (int i = 0; i < elements.Count; i++)
            {
                if (elements[i] is EmoteButton emoteButton)
                {
                    int type = (int)typeof(EmoteButton).GetField("_emoteIndex", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(emoteButton);
                    if (type >= EmoteID.Count)
                    {
                        elements[i] = registeredEmotes[type - EmoteID.Count].ProvideEmoteButton(emoteButton);
                        elements[i].Remove();
                        typeof(UIElement).GetProperty("Parent", BindingFlags.Public | BindingFlags.Instance).GetSetMethod(nonPublic: true)
                            .Invoke(elements[i], new object[] { self });
                        elements[i].Recalculate();
                    }
                }
            }
        }
        private Rectangle EmoteButton_GetFrame(On.Terraria.GameContent.UI.Elements.EmoteButton.orig_GetFrame orig, EmoteButton self)
        {
            if (self is ModdedEmoteButton modded)
            {
                return modded.BubbleFrame();
            }
            return orig(self);
        }

        private static List<int> UIEmotesMenu_GetEmotesBosses(On.Terraria.GameContent.UI.States.UIEmotesMenu.orig_GetEmotesBosses orig, Terraria.GameContent.UI.States.UIEmotesMenu self)
        {
            return GetOfListing(orig(self), EmoteListing.Dangers);
        }
        private static List<int> UIEmotesMenu_GetEmotesCritters(On.Terraria.GameContent.UI.States.UIEmotesMenu.orig_GetEmotesCritters orig, Terraria.GameContent.UI.States.UIEmotesMenu self)
        {
            return GetOfListing(orig(self), EmoteListing.CrittersAndMonsters);
        }
        private static List<int> UIEmotesMenu_GetEmotesTownNPCs(On.Terraria.GameContent.UI.States.UIEmotesMenu.orig_GetEmotesTownNPCs orig, Terraria.GameContent.UI.States.UIEmotesMenu self)
        {
            return GetOfListing(orig(self), EmoteListing.Town);
        }
        private static List<int> UIEmotesMenu_GetEmotesBiomesAndEvents(On.Terraria.GameContent.UI.States.UIEmotesMenu.orig_GetEmotesBiomesAndEvents orig, Terraria.GameContent.UI.States.UIEmotesMenu self)
        {
            return GetOfListing(orig(self), EmoteListing.NatureAndWeather);
        }
        private static List<int> UIEmotesMenu_GetEmotesItems(On.Terraria.GameContent.UI.States.UIEmotesMenu.orig_GetEmotesItems orig, Terraria.GameContent.UI.States.UIEmotesMenu self)
        {
            return GetOfListing(orig(self), EmoteListing.Items);
        }
        private static List<int> UIEmotesMenu_GetEmotesRPS(On.Terraria.GameContent.UI.States.UIEmotesMenu.orig_GetEmotesRPS orig, Terraria.GameContent.UI.States.UIEmotesMenu self)
        {
            return GetOfListing(orig(self), EmoteListing.RockPaperScissors);
        }
        private static List<int> UIEmotesMenu_GetEmotesGeneral(On.Terraria.GameContent.UI.States.UIEmotesMenu.orig_GetEmotesGeneral orig, Terraria.GameContent.UI.States.UIEmotesMenu self)
        {
            return GetOfListing(orig(self), EmoteListing.General);
        }
        private static List<int> GetOfListing(List<int> originalList, EmoteListing listing)
        {
            foreach (var r in registeredEmotes)
            {
                if (r.Listing == listing)
                {
                    originalList.Add(r.Type);
                }
            }
            return originalList;
        }

        private static void ChatInitializer_PrepareAliases(On.Terraria.Initializers.ChatInitializer.orig_PrepareAliases orig)
        {
            orig();

            if (!EmotesSetup)
            {
                return;
            }

            for (int i = 0; i < ModEmoteCount; i++)
            {
                var key = registeredEmotes[i].SlashCommand;
                ChatManager.Commands.AddAlias(key, NetworkText.FromFormattable("{0} {1}", Language.GetText("ChatCommand.Emoji_1"), registeredEmotes[i].Command));
            }
        }
        private static void EmoteBubble_Draw(On.Terraria.GameContent.UI.EmoteBubble.orig_Draw orig, EmoteBubble self, SpriteBatch sb)
        {
            bool regularDraw = true;
            foreach (var g in globalEmotes)
            {
                regularDraw |= g.PreDraw(self, sb);
            }
            if (self.emote >= EmoteID.Count)
            {
                if (regularDraw)
                {
                    registeredEmotes[self.emote - EmoteID.Count].Draw(self, sb);
                }
                registeredEmotes[self.emote - EmoteID.Count].SpecialDraw(self, sb);
                goto SpecialDraw;
            }

            if (regularDraw)
                orig(self, sb);

        SpecialDraw:
            foreach (var g in globalEmotes)
            {
                g.SpecialDraw(self, sb);
            }
        }
        private void EmoteBubble_PickNPCEmote(ILContext il)
        {
            var c = new ILCursor(il);

            if (!c.TryGotoNext(MoveType.After, (i) => i.MatchLdloc(2)))
            {
                Logger.Error("Failed to find ldloc.2 location for " + nameof(DetermineBossActive));
                return;
            }

            //c.Emit(OpCodes.Ldarg_0); // WorldUIAnchor
            //c.Emit(OpCodes.Ldloc_0); // Player
            //c.Emit(OpCodes.Ldloc_1); // List<int>, emote list
            c.Emit(OpCodes.Ldloca, 2); // ref bool, boss active
            c.EmitDelegate(DetermineBossActive);

            if (!c.TryGotoNext(MoveType.After, (i) => i.MatchCall<EmoteBubble>("ProbeCombat")))
            {
                Logger.Error("Failed to find call location EmoteBubble::ProbeCombat location for " + nameof(GetModEmoteBubbles));
                return;
            }

            c.Index++;
            c.Emit(OpCodes.Ldarg_0); // EmoteBubble
            c.Emit(OpCodes.Ldarg_0); // WorldUIAnchor
            c.Emit(OpCodes.Ldloc_0); // Player
            c.Emit(OpCodes.Ldloc_1); // List<int>, emote list
            c.Emit(OpCodes.Ldloc_2); // bool, boss active
            c.EmitDelegate(GetModEmoteBubbles);
        }
        private static void DetermineBossActive(ref bool bossActive)
        {
            bool? modBossActive = null;
            foreach (var g in globalEmotes)
            {
                bool? globalValue = g.IsBossActive();
                if (modBossActive == true)
                {
                    continue;
                }
                if (globalValue.HasValue)
                {
                    modBossActive = globalValue.Value;
                }
            }
            if (modBossActive.HasValue)
            {
                bossActive = modBossActive.Value;
            }
        }
        private static void GetModEmoteBubbles(EmoteBubble emote, WorldUIAnchor anchor, Player player, List<int> emotes, bool bossActive)
        {
            foreach (var e in registeredEmotes)
            {
                if (e.CanNPCChat())
                {
                    emotes.Add(e.Type);
                }
            }
            foreach (var global in globalEmotes)
            {
                global.ModifyEmoteChoices(emotes, emote, anchor, player, bossActive);
            }
        }
        #endregion

        public override void AddRecipes()
        {
            foreach (var e in registeredEmotes)
            {
                EmoteID.Search.Add(e.FullName, e.Type);
            }
            EmotesSetup = true;
            ChatInitializer.PrepareAliases();
            var emoteCommands = GetCommandList();
            if (emoteCommands == null)
            {
                Logger.Error("Emote Commands is null.");
            }
            if (registeredEmotes == null)
            {
                Logger.Error("Registered emotes list is null.");
            }
            foreach (var e in registeredEmotes)
            {
                if (e == null)
                {
                    Logger.Error("Mod Emote is null.");
                }
                if (e?.Command == null)
                {
                    Logger.Error("Mod Emote command is null.");
                }
                emoteCommands.Add(e.Command, e.Type);
            }
        }

        public override void Unload()
        {
            if (registeredEmotes != null)
            {
                foreach (var c in registeredEmotes)
                {
                    try
                    {
                        EmoteID.Search.Remove(c.Type);
                    }
                    catch
                    {
                    }
                }
            }

            try
            {
                var emoteCommands = GetCommandList();
                List<LocalizedText> remove = new List<LocalizedText>();
                foreach (var c in emoteCommands)
                {
                    if (c.Value >= EmoteID.Count)
                    {
                        remove.Add(c.Key);
                    }
                }
                foreach (var r in remove)
                {
                    emoteCommands.Remove(r);
                }
            }
            catch
            {
            }
        }

        private static Dictionary<LocalizedText, int> GetCommandList()
        {
            var chatCommandsList = (Dictionary<ChatCommandId, IChatCommand>)typeof(ChatCommandProcessor).GetField("_commands", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(ChatManager.Commands);
            var emojiCommandInstance = chatCommandsList.Where((v) => v.Value is EmojiCommand).First();
            var emoteCommands = (Dictionary<LocalizedText, int>)typeof(EmojiCommand).GetField("_byName", BindingFlags.NonPublic | BindingFlags.Instance)
                .GetValue(emojiCommandInstance.Value);
            return emoteCommands;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="type"></param>
        /// <returns>A modded emote using the type provided. Returns null if the type doesn't exist</returns>
        public static ModEmote GetEmote(int type)
        {
            if (type < EmoteID.Count || type > EmoteCount)
            {
                return registeredEmotes[type];
            }
            return null;
        }
        public static int EmoteType<T>() where T : ModEmote
        {
            return ModContent.GetInstance<T>().Type;
        }

        /// <summary>
        /// Adds a modded emote
        /// </summary>
        /// <param name="emote"></param>
        /// <returns>The ID of the emote</returns>
        public static int AddEmote(ModEmote emote)
        {
            if (EmotesSetup)
            {
                throw new Exception("Emotes are already setup. Please add emotes before Mod.AddRecipes.");
            }
            ModContent.GetInstance<EmoteBubbleLib>().Logger.Info("Adding " + emote.Name + ", command: " + emote.Command + ", type: " + emote.Type);
            if (!Main.dedServ)
            {
                emote.Icon = ModContent.Request<Texture2D>(emote.Texture);
            }
            Array.Resize(ref registeredEmotes, ModEmoteCount + 1);
            registeredEmotes[ModEmoteCount - 1] = emote;
            registeredEmotes[ModEmoteCount - 1].Type = EmoteCount + ModEmoteCount - 1;
            return ModEmoteCount - 1;
        }

        //public class GlobalEmoteTest : GlobalEmote 
        //{
        //    public override bool? IsBossActive()
        //    {
        //        return Main.rand.NextBool() ? null : true;
        //    }

        //    public override void ModifyEmoteChoices(List<int> emotes, EmoteBubble bubble, WorldUIAnchor anchor, Player player, bool bossActive)
        //    {
        //        emotes.Clear();
        //        emotes.Add(EmoteType<ModEmoteTest>());
        //    }
        //}

        //public class ModEmoteTest : ModEmote 
        //{
        //    public override LocalizedText Command => CreateName("omegastarite");
        //    public override LocalizedText SlashCommand => CreateName("/omegastarite");

        //    public override void SetStaticDefaults()
        //    {
        //        Listing = EmoteListing.Dangers;
        //    }
        //}

        //public class TheCommand : ModCommand
        //{
        //    public override string Command => "emojis";

        //    public override CommandType Type => CommandType.Chat;

        //    public override void Action(CommandCaller caller, string input, string[] args)
        //    {
        //        foreach (var c in GetCommandList())
        //        {
        //            if (c.Value >= EmoteID.Count)
        //            {
        //                caller.Reply(c.Key.ToString());
        //            }
        //        }
        //    }
        //}

        public static void GetDrawParams(EmoteBubble emote, out Vector2 drawPosition, out Vector2 origin, out SpriteEffects effect, out bool dontDrawIcon)
        {
            switch (emote.anchor.type)
            {
                case WorldUIAnchor.AnchorType.Entity:
                    effect = (SpriteEffects)((emote.anchor.entity.direction != -1) ? 1 : 0);
                    drawPosition = new Vector2(emote.anchor.entity.Top.X, emote.anchor.entity.VisualPosition.Y) + new Vector2((float)(-emote.anchor.entity.direction * emote.anchor.entity.width) * 0.75f, 2f) - Main.screenPosition;
                    break;

                case WorldUIAnchor.AnchorType.Pos:
                    effect = SpriteEffects.None;
                    drawPosition = emote.anchor.pos - Main.screenPosition;
                    break;

                case WorldUIAnchor.AnchorType.Tile:
                    effect = SpriteEffects.None;
                    drawPosition = emote.anchor.pos - Main.screenPosition + new Vector2(0f, (0f - emote.anchor.size.Y) / 2f);
                    break;

                default:
                    effect = SpriteEffects.None;
                    drawPosition = new Vector2((float)Main.screenWidth, (float)Main.screenHeight) / 2f;
                    break;
            }

            drawPosition.Floor();

            dontDrawIcon = emote.lifeTime < 6 || emote.lifeTimeStart - emote.lifeTime < 6;
            var bubbleFrame = TextureAssets.Extra[ExtrasID.EmoteBubble].Frame(EmoteBubble.EMOTE_SHEET_HORIZONTAL_FRAMES, EmoteBubble.EMOTE_SHEET_VERTICAL_FRAMES, (!dontDrawIcon) ? 1 : 0);
            origin = new Vector2(bubbleFrame.Width / 2f, bubbleFrame.Height);
            if (Main.player[Main.myPlayer].gravDir == -1)
            {
                origin.Y = 0f;
                effect |= SpriteEffects.FlipVertically;
                drawPosition = Main.ReverseGravitySupport(drawPosition);
            }
        }
    }
}