using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SmartphoneAppGame.Data;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Minigames;

namespace SmartphoneAppGame
{
    public class ContactActionCardButton : IContactActionCardButton
    {
        public string Text { get; set; } = string.Empty;
        public Color BackgroundColor { get; set; } = Color.White;
        public Color TextColor { get; set; } = Color.Black;
        public Action<string>? OnClick { get; set; }
    }

    internal sealed class ModEntry : Mod
    {
        private const string SmartphoneModId = "d5a1lamdtd.Smartphone";

        private const string ItemDartsId = "darts";
        private const string ItemPirateId = "prairie_king";
        private const string ItemMineCartId = "mine_cart";
        private const string ItemSlotsId = "slots";
        private const string ItemCalicoJackId = "calico_jack";
        private const string ItemCraneId = "crane";

        private ISmartPhoneApi? smartphoneApi;

        private Texture2D? gameDartsIcon;
        private Texture2D? gamePirateIcon;
        private Texture2D? gameCartIcon;
        private Texture2D? gameSpinIcon;
        private Texture2D? gameJackIcon;
        private Texture2D? gameCraneIcon;

        public override void Entry(IModHelper helper)
        {
            helper.Events.GameLoop.GameLaunched += this.OnGameLaunched;
        }

        private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
        {
            this.smartphoneApi = this.Helper.ModRegistry.GetApi<ISmartPhoneApi>(SmartphoneModId);
            if (this.smartphoneApi == null)
            {
                this.Monitor.Log("Smartphone API is unavailable; Games app group was not registered.", LogLevel.Warn);
                return;
            }

            this.LoadIcons();
            this.RegisterGameApps();
            this.RegisterChatQuickActions();
        }

        private void LoadIcons()
        {
            this.gameDartsIcon = this.Helper.ModContent.Load<Texture2D>("assets/game_darts.png");
            this.gamePirateIcon = this.Helper.ModContent.Load<Texture2D>("assets/game_pirate.png");
            this.gameCartIcon = this.Helper.ModContent.Load<Texture2D>("assets/game_cart.png");
            this.gameSpinIcon = this.Helper.ModContent.Load<Texture2D>("assets/game_spin.png");
            this.gameJackIcon = this.Helper.ModContent.Load<Texture2D>("assets/game_jack.png");
            this.gameCraneIcon = this.Helper.ModContent.Load<Texture2D>("assets/game_crane.png");
        }

        private void RegisterGameApps()
        {
            if (this.smartphoneApi == null
                || this.gameDartsIcon == null
                || this.gamePirateIcon == null
                || this.gameCartIcon == null
                || this.gameSpinIcon == null
                || this.gameJackIcon == null
                || this.gameCraneIcon == null)
            {
                return;
            }

            this.RegisterGameApp(ItemDartsId, "Darts", this.gameDartsIcon, OpenDarts);
            this.RegisterGameApp(ItemPirateId, "Prairie King", this.gamePirateIcon, OpenPrairieKing);
            this.RegisterGameApp(ItemMineCartId, "Mine Cart", this.gameCartIcon, OpenMineCart);
            this.RegisterGameApp(ItemCalicoJackId, "Calico Jack", this.gameJackIcon, OpenCalicoJack);
            this.RegisterGameApp(ItemCraneId, "Crane", this.gameCraneIcon, OpenCrane);
            this.RegisterGameApp(ItemSlotsId, "Slots", this.gameSpinIcon, OpenSlots);
        }

        private void RegisterChatQuickActions()
        {
            if (this.smartphoneApi == null)
                return;

            var playButton = new ContactActionCardButton
            {
                Text = "Play",
                BackgroundColor = Color.CadetBlue,
                TextColor = Color.White,
                OnClick = this.StartAbigailPraiseKing
            };

            bool actionRegistered = this.smartphoneApi.RegisterContactActionCard(
                modId: this.ModManifest.UniqueID,
                cardTitle: "Prairie King",
                buttons: new List<IContactActionCardButton> { playButton },
                npcNames: new List<string> { "Abigail" });

            if (!actionRegistered)
            {
                this.Monitor.Log("Failed to register Abigail Praise King contact card.", LogLevel.Warn);
            }
        }

        private void RegisterGameApp(string appId, string displayName, Texture2D icon, Action onClick)
        {
            if (this.smartphoneApi == null)
                return;

            bool appRegistered = this.smartphoneApi.RegisterPhoneApp(
                ownerModId: this.ModManifest.UniqueID,
                appId: appId,
                displayName: displayName,
                onClick: onClick,
                closePhoneOnLaunch: true,
                sourceRect: null,
                getBadgeCount: null,
                supportedSizes: Array.Empty<AppSize>(),
                onDrawWidget: null,
                themedIconTextures: new Dictionary<string, Texture2D>(StringComparer.OrdinalIgnoreCase)
                {
                    { "default", icon }
                });

            if (!appRegistered)
            {
                this.Monitor.Log($"Failed to register Smartphone game app '{appId}'.", LogLevel.Warn);
            }
        }

        private static void OpenMineCart()
        {
            if (!Context.IsWorldReady)
                return;

            int type = Game1.dayOfMonth % 2 == 0 ? 3 : 2;
            Game1.currentMinigame = new MineCart(0, type);
        }

        private void OpenCalicoJack()
        {
            if (!Context.IsWorldReady)
                return;

            int bet = Game1.dayOfMonth % 2 == 0 ? 1000 : 100;
            int costToStart = 100;

            if (Game1.player.Money < costToStart)
            {
                this.smartphoneApi?.SendSmartphoneNotification(
                    "You don't have enough 100 Gold!",
                    "Games",
                    Game1.player.UniqueMultiplayerID.ToString());
                return;
            }

            if (Game1.player.clubCoins < bet)
            {
                this.smartphoneApi?.SendSmartphoneNotification(
                    $"You don't have enough Casino Coins for today bet: {bet}!",
                    "Games",
                    Game1.player.UniqueMultiplayerID.ToString());
                return;
            }

            string question = $"Cost to start: {costToStart}G. Today's bet: {bet} Casino Coins";

            Game1.activeClickableMenu = new ConfirmationDialog(
                question,
                onConfirm: (Farmer who) =>
                {
                    Game1.player.Money -= costToStart;

                    bool highStakes = bet == 1000;
                    Game1.activeClickableMenu = null;
                    Game1.currentMinigame = new CalicoJack(highStakes: highStakes);
                },
                onCancel: (Farmer who) =>
                {
                    Game1.activeClickableMenu = null;
                });
        }

        private static void OpenDarts()
        {
            if (!Context.IsWorldReady)
                return;

            Game1.currentMinigame = new Darts();
        }

        private void OpenPrairieKing()
        {
            if (!Context.IsWorldReady || this.smartphoneApi == null)
                return;

            Game1.activeClickableMenu = new PraiseKingStartScreen(
                this.smartphoneApi,
                () => this.smartphoneApi.OpenPhoneHomeScreen());
        }

        private void StartAbigailPraiseKing(string npcName)
        {
            if (!Context.IsWorldReady)
                return;

            Game1.currentMinigame = new AbigailGame(Game1.getCharacterFromName(npcName));
        }

        private void OpenSlots()
        {
            if (!Context.IsWorldReady)
                return;

            if (Game1.player.clubCoins < 10)
            {
                this.smartphoneApi?.SendSmartphoneNotification(
                    "You don't have enough Casino Coins!",
                    "Games",
                    Game1.player.UniqueMultiplayerID.ToString());
                return;
            }

            Game1.currentMinigame = new Slots();
        }

        private void OpenCrane()
        {
            if (!Context.IsWorldReady)
                return;

            if (Game1.player.Money < 750)
            {
                this.smartphoneApi?.SendSmartphoneNotification(
                    "You don't have enough Gold!",
                    "Games",
                    Game1.player.UniqueMultiplayerID.ToString());
                return;
            }

            string question = "Play Crane Game? Cost to start: 750G";

            Game1.activeClickableMenu = new ConfirmationDialog(
                question,
                onConfirm: (Farmer who) =>
                {
                    Game1.player.Money -= 750;

                    Game1.activeClickableMenu = null;
                    Game1.currentMinigame = new CraneGame();
                },
                onCancel: (Farmer who) =>
                {
                    Game1.activeClickableMenu = null;
                });
        }
    }
}
