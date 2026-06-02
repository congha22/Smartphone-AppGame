using Microsoft.Xna.Framework.Graphics;
using SmartphoneAppGame.Data;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Minigames;

namespace SmartphoneAppGame
{
    internal sealed class ModEntry : Mod
    {
        private const string SmartphoneModId = "d5a1lamdtd.Smartphone";
        private const string GamesGroupId = "games";

        private const string ItemDartsId = "darts";
        private const string ItemPirateId = "prairie_king";
        private const string ItemMineCartId = "mine_cart";
        private const string ItemSlotsId = "slots";
        private const string ItemCalicoJackId = "calico_jack";
        private const string ItemCraneId = "crane";

        private ISmartPhoneApi? smartphoneApi;

        private Texture2D? appGameIcon;
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
            this.RegisterGamesAppGroup();
            this.RegisterChatQuickActions();
        }

        private void LoadIcons()
        {
            this.appGameIcon = this.Helper.ModContent.Load<Texture2D>("assets/app_game.png");
            this.gameDartsIcon = this.Helper.ModContent.Load<Texture2D>("assets/game_darts.png");
            this.gamePirateIcon = this.Helper.ModContent.Load<Texture2D>("assets/game_pirate.png");
            this.gameCartIcon = this.Helper.ModContent.Load<Texture2D>("assets/game_cart.png");
            this.gameSpinIcon = this.Helper.ModContent.Load<Texture2D>("assets/game_spin.png");
            this.gameJackIcon = this.Helper.ModContent.Load<Texture2D>("assets/game_jack.png");
            this.gameCraneIcon = this.Helper.ModContent.Load<Texture2D>("assets/game_crane.png");
        }

        private void RegisterGamesAppGroup()
        {
            if (this.smartphoneApi == null
                || this.appGameIcon == null
                || this.gameDartsIcon == null
                || this.gamePirateIcon == null
                || this.gameCartIcon == null
                || this.gameSpinIcon == null
                || this.gameJackIcon == null
                || this.gameCraneIcon == null)
            {
                return;
            }

            bool groupRegistered = this.smartphoneApi.RegisterPhoneAppGroup(
                ownerModId: this.ModManifest.UniqueID,
                groupId: GamesGroupId,
                displayName: "Games",
                iconTexture: this.appGameIcon,
                sortOrder: 999,
                sourceRect: null,
                isVisible: () => Context.IsWorldReady,
                getBadgeCount: null);

            if (!groupRegistered)
            {
                this.Monitor.Log("Failed to register Smartphone Games app group.", LogLevel.Warn);
                return;
            }

            this.RegisterGameItem(ItemDartsId, "Darts", this.gameDartsIcon, OpenDarts, 0);
            this.RegisterGameItem(ItemPirateId, "Prairie King", this.gamePirateIcon, OpenPrairieKing, 1);
            this.RegisterGameItem(ItemMineCartId, "Mine Cart", this.gameCartIcon, OpenMineCart, 2);
            this.RegisterGameItem(ItemCalicoJackId, "Calico Jack", this.gameJackIcon, OpenCalicoJack, 3);
            this.RegisterGameItem(ItemCraneId, "Crane", this.gameCraneIcon, OpenCrane, 4);
            this.RegisterGameItem(ItemSlotsId, "Slots", this.gameSpinIcon, OpenSlots, 5);
        }

        private void RegisterChatQuickActions()
        {
            if (this.smartphoneApi == null || this.gamePirateIcon == null)
                return;

            bool actionRegistered = this.smartphoneApi.RegisterChatQuickActionButton(
                ownerModId: this.ModManifest.UniqueID,
                actionId: "abigail_praise_king",
                iconTexture: this.gamePirateIcon,
                onClick: this.StartAbigailPraiseKing,
                closePhoneOnLaunch: true,
                sortOrder: 999,
                sourceRect: null,
                npcNames: new List<string> { "Abigail" });

            if (!actionRegistered)
            {
                this.Monitor.Log("Failed to register Abigail Praise King quick action.", LogLevel.Warn);
            }
        }

        private void RegisterGameItem(string itemId, string displayName, Texture2D icon, Action onClick, int sortOrder)
        {
            if (this.smartphoneApi == null)
                return;

            bool itemRegistered = this.smartphoneApi.RegisterPhoneAppGroupItem(
                ownerModId: this.ModManifest.UniqueID,
                groupId: GamesGroupId,
                itemId: itemId,
                displayName: displayName,
                iconTexture: icon,
                onClick: onClick,
                closePhoneOnLaunch: true,
                sortOrder: sortOrder,
                sourceRect: null,
                isVisible: () => Context.IsWorldReady,
                getBadgeCount: null);

            if (!itemRegistered)
            {
                this.Monitor.Log($"Failed to register Smartphone game item '{itemId}'.", LogLevel.Warn);
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

        private static void OpenPrairieKing()
        {
            if (!Context.IsWorldReady)
                return;

            Game1.currentMinigame = new AbigailGame();
        }

        private void StartAbigailPraiseKing(string npcName)
        {
            if (!Context.IsWorldReady)
                return;

            Game1.currentMinigame = new AbigailGame(Game1.getCharacterFromName(npcName));
            this.smartphoneApi?.SendSmartphoneMessageFromNPC(
                "Abigail",
                "Thank you for playing Prairie King with me!",
                Game1.player.UniqueMultiplayerID.ToString());
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
