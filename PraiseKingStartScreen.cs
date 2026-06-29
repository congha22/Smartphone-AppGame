using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using SmartphoneAppGame.Data;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Minigames;

namespace SmartphoneAppGame
{
    public class PraiseKingStartScreen : IClickableMenu
    {
        private readonly ISmartPhoneApi smartphoneApi;
        private readonly Action onBack;

        // Layout bounds
        private int phoneFrameWidth;
        private int phoneFrameHeight;
        private int phoneContentOffsetX;
        private int phoneContentOffsetY;
        private float phoneUiScale;

        private Texture2D? phoneFrameTexture;
        private Texture2D? phoneBackgroundTexture;

        private int contentWidth;
        private int contentHeight;

        // Drag State
        private bool isDragging;
        private int dragOffsetX;
        private int dragOffsetY;

        // Scroll State
        private int scrollOffset;
        private int maxScroll;
        private bool isScrolling;
        private int lastScrollMouseY;
        private int touchScrollStartY;
        private bool hasTouchScrolled;
        
        // Buttons
        private Rectangle soloButtonBounds;
        private Rectangle duoButtonBounds;
        private bool isSoloHovered;
        private bool isDuoHovered;

        // Colors
        private static readonly Color ButtonColor = new Color(50, 150, 255);
        private static readonly Color ButtonHoverColor = new Color(100, 180, 255);
        private static readonly Color ButtonTextColor = Color.White;

        // Animation
        private float bounceTimer;

        public PraiseKingStartScreen(ISmartPhoneApi api, Action onBack)
            : base()
        {
            this.smartphoneApi = api;
            this.onBack = onBack;

            // Get phone position
            var (px, py) = api.GetPhonePosition();
            this.xPositionOnScreen = px;
            this.yPositionOnScreen = py;

            this.phoneFrameWidth = api.GetPhoneFrameWidth();
            this.phoneFrameHeight = api.GetPhoneFrameHeight();
            var (offX, offY) = api.GetPhoneContentOffset();
            this.phoneContentOffsetX = offX;
            this.phoneContentOffsetY = offY;
            this.phoneUiScale = api.GetPhoneUiScale();
            this.phoneFrameTexture = api.GetPhoneFrameTexture();
            this.phoneBackgroundTexture = api.GetPhoneBackgroundTexture();

            this.width = this.phoneFrameWidth;
            this.height = this.phoneFrameHeight;

            if (this.phoneBackgroundTexture != null && !this.phoneBackgroundTexture.IsDisposed)
            {
                this.contentWidth = (int)Math.Round(this.phoneBackgroundTexture.Width * this.phoneUiScale);
                this.contentHeight = (int)Math.Round(this.phoneBackgroundTexture.Height * this.phoneUiScale);
            }
            else
            {
                this.contentWidth = Math.Max(1, this.phoneFrameWidth - (this.phoneContentOffsetX * 2));
                this.contentHeight = Math.Max(1, this.phoneFrameHeight - this.phoneContentOffsetY - ScaleValue(80));
            }

            CalculateLayout();
        }

        private int ScaleValue(int baseValue)
        {
            return (int)Math.Round(baseValue * this.phoneUiScale);
        }

        private Rectangle GetFrameBounds()
        {
            return new Rectangle(this.xPositionOnScreen, this.yPositionOnScreen, this.phoneFrameWidth, this.phoneFrameHeight);
        }

        private Rectangle GetContentBounds()
        {
            return new Rectangle(
                this.xPositionOnScreen + this.phoneContentOffsetX,
                this.yPositionOnScreen + this.phoneContentOffsetY,
                this.contentWidth,
                this.contentHeight);
        }

        private void CalculateLayout()
        {
            Rectangle content = GetContentBounds();

            // Calculate total scrollable height
            int totalHeight = this.contentHeight + ScaleValue(180); // Need enough space to show instructions and buttons below fold
            this.maxScroll = Math.Max(0, totalHeight - this.contentHeight);
            
            int btnWidth = ScaleValue(220);
            int btnHeight = ScaleValue(60);
            int centerX = content.Center.X;

            // These are local Y coordinates relative to the top of the scrollable content.
            // Put them below the fold (i.e. below this.contentHeight)
            int soloLocalY = this.contentHeight + ScaleValue(20);
            int duoLocalY = soloLocalY + btnHeight + ScaleValue(20);

            this.soloButtonBounds = new Rectangle(centerX - btnWidth / 2, soloLocalY, btnWidth, btnHeight);
            this.duoButtonBounds = new Rectangle(centerX - btnWidth / 2, duoLocalY, btnWidth, btnHeight);
        }

        public override void draw(SpriteBatch b)
        {
            // Dim background
            b.Draw(Game1.staminaRect, new Rectangle(0, 0, Game1.uiViewport.Width, Game1.uiViewport.Height), Color.Black * 0.6f);

            Rectangle contentRect = GetContentBounds();
            Rectangle frameRect = GetFrameBounds();

            // Draw phone background
            if (this.phoneBackgroundTexture != null && !this.phoneBackgroundTexture.IsDisposed)
            {
                b.Draw(this.phoneBackgroundTexture, contentRect, Color.White);
            }
            else
            {
                b.Draw(Game1.staminaRect, contentRect, new Color(30, 30, 30));
            }

            // Scissor rect for scrollable content
            b.End();
            b.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, new RasterizerState() { ScissorTestEnable = true });
            Rectangle previousScissor = Game1.graphics.GraphicsDevice.ScissorRectangle;
            Game1.graphics.GraphicsDevice.ScissorRectangle = contentRect;

            DrawScrollableContent(b, contentRect);

            b.End();
            Game1.graphics.GraphicsDevice.ScissorRectangle = previousScissor;
            b.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);

            // Draw phone border on top
            if (this.phoneFrameTexture != null && !this.phoneFrameTexture.IsDisposed)
            {
                b.Draw(this.phoneFrameTexture, frameRect, Color.White);
            }

            // Draw phone size adjustment buttons
            this.smartphoneApi.DrawPhoneSizeButtons(b, this.xPositionOnScreen, this.yPositionOnScreen);

            drawMouse(b);
        }

        private void DrawScrollableContent(SpriteBatch b, Rectangle contentRect)
        {
            SpriteFont font = Game1.dialogueFont;
            
            // Title centered vertically on the first page
            float titleScale = Math.Max(0.7f, this.phoneUiScale * 1.0f);
            string titleLine1 = "Journey of the";
            string titleLine2 = "Praise King";
            Vector2 title1Size = font.MeasureString(titleLine1) * titleScale;
            Vector2 title2Size = font.MeasureString(titleLine2) * titleScale;

            int titleY = contentRect.Y - this.scrollOffset + (this.contentHeight / 2) - (int)((title1Size.Y + title2Size.Y) / 2) - ScaleValue(60);

            Vector2 title1Pos = new Vector2(contentRect.Center.X - (title1Size.X / 2f), titleY);
            b.DrawString(font, titleLine1, title1Pos + new Vector2(2f, 2f), Color.Black * 0.4f, 0f, Vector2.Zero, titleScale, SpriteEffects.None, 1f);
            b.DrawString(font, titleLine1, title1Pos, Color.Gold, 0f, Vector2.Zero, titleScale, SpriteEffects.None, 1f);
            
            Vector2 title2Pos = new Vector2(contentRect.Center.X - (title2Size.X / 2f), titleY + title1Size.Y);
            b.DrawString(font, titleLine2, title2Pos + new Vector2(2f, 2f), Color.Black * 0.4f, 0f, Vector2.Zero, titleScale, SpriteEffects.None, 1f);
            b.DrawString(font, titleLine2, title2Pos, Color.Gold, 0f, Vector2.Zero, titleScale, SpriteEffects.None, 1f);

            // Instructions near the bottom of the first page
            string inst1 = "Use W A S D to move";
            string inst2 = "Use arrow button to shoot.";
            float instScale = 0.65f * this.phoneUiScale;
            Vector2 inst1Size = font.MeasureString(inst1) * instScale;
            Vector2 inst2Size = font.MeasureString(inst2) * instScale;

            int instY = contentRect.Y - this.scrollOffset + this.contentHeight - ScaleValue(110);

            Vector2 inst1Pos = new Vector2(contentRect.Center.X - (inst1Size.X / 2f), instY);
            Vector2 inst2Pos = new Vector2(contentRect.Center.X - (inst2Size.X / 2f), instY + inst1Size.Y);

            b.DrawString(font, inst1, inst1Pos, Color.Black, 0f, Vector2.Zero, instScale, SpriteEffects.None, 1f);
            b.DrawString(font, inst2, inst2Pos, Color.Black, 0f, Vector2.Zero, instScale, SpriteEffects.None, 1f);

            // Down Arrow
            if (this.scrollOffset < this.maxScroll)
            {
                float bounce = (float)Math.Sin(this.bounceTimer * 5f) * ScaleValue(5);
                string arrow = "↓";
                Vector2 arrowSize = font.MeasureString(arrow) * titleScale;
                Vector2 arrowPos = new Vector2(contentRect.Center.X - (arrowSize.X / 2f), contentRect.Bottom - ScaleValue(40) + bounce);
                b.DrawString(font, arrow, arrowPos, Color.Black, 0f, Vector2.Zero, titleScale, SpriteEffects.None, 1f);
            }

            // Draw Buttons
            // Adjust bounds by content rect and scroll offset
            Rectangle actualSoloBounds = new Rectangle(
                this.soloButtonBounds.X,
                contentRect.Y - this.scrollOffset + this.soloButtonBounds.Y,
                this.soloButtonBounds.Width,
                this.soloButtonBounds.Height);

            Rectangle actualDuoBounds = new Rectangle(
                this.duoButtonBounds.X,
                contentRect.Y - this.scrollOffset + this.duoButtonBounds.Y,
                this.duoButtonBounds.Width,
                this.duoButtonBounds.Height);

            DrawButton(b, actualSoloBounds, this.isSoloHovered ? ButtonHoverColor : ButtonColor, "Play solo", font, 0.5f * this.phoneUiScale, ButtonTextColor);
            DrawButton(b, actualDuoBounds, this.isDuoHovered ? ButtonHoverColor : ButtonColor, "Duo with Abigail", font, 0.5f * this.phoneUiScale, ButtonTextColor);
        }

        private static void DrawButton(SpriteBatch b, Rectangle bounds, Color fillColor, string text, SpriteFont font, float scale, Color textColor)
        {
            Color highlight = new Color(Math.Min(255, (int)(fillColor.R * 1.3f)), Math.Min(255, (int)(fillColor.G * 1.3f)), Math.Min(255, (int)(fillColor.B * 1.3f)), fillColor.A);
            Color shadow = new Color((int)(fillColor.R * 0.6f), (int)(fillColor.G * 0.6f), (int)(fillColor.B * 0.6f), fillColor.A);

            b.Draw(Game1.staminaRect, bounds, fillColor);
            b.Draw(Game1.staminaRect, new Rectangle(bounds.X, bounds.Y, bounds.Width, 2), highlight);
            b.Draw(Game1.staminaRect, new Rectangle(bounds.X, bounds.Y, 2, bounds.Height), highlight);
            b.Draw(Game1.staminaRect, new Rectangle(bounds.X, bounds.Bottom - 2, bounds.Width, 2), shadow);
            b.Draw(Game1.staminaRect, new Rectangle(bounds.Right - 2, bounds.Y, 2, bounds.Height), shadow);

            Vector2 txtSize = font.MeasureString(text) * scale;
            Vector2 txtPos = new Vector2(bounds.Center.X - (txtSize.X / 2f), bounds.Center.Y - (txtSize.Y / 2f));
            b.DrawString(font, text, txtPos + new Vector2(1f, 1f), Color.Black * 0.3f, 0f, Vector2.Zero, scale, SpriteEffects.None, 1f);
            b.DrawString(font, text, txtPos, textColor, 0f, Vector2.Zero, scale, SpriteEffects.None, 1f);
        }

        public override void performHoverAction(int x, int y)
        {
            base.performHoverAction(x, y);

            Rectangle contentRect = GetContentBounds();

            Rectangle actualSoloBounds = new Rectangle(
                this.soloButtonBounds.X,
                contentRect.Y - this.scrollOffset + this.soloButtonBounds.Y,
                this.soloButtonBounds.Width,
                this.soloButtonBounds.Height);

            Rectangle actualDuoBounds = new Rectangle(
                this.duoButtonBounds.X,
                contentRect.Y - this.scrollOffset + this.duoButtonBounds.Y,
                this.duoButtonBounds.Width,
                this.duoButtonBounds.Height);

            // Only hover if within the content rect (scissor region)
            if (contentRect.Contains(x, y))
            {
                this.isSoloHovered = actualSoloBounds.Contains(x, y);
                this.isDuoHovered = actualDuoBounds.Contains(x, y);
            }
            else
            {
                this.isSoloHovered = false;
                this.isDuoHovered = false;
            }
        }

        public override void receiveLeftClick(int x, int y, bool playSound = true)
        {
            if (this.smartphoneApi.HandlePhoneAppBottomNavClick(x, y, this.xPositionOnScreen, this.yPositionOnScreen, onBack: this.onBack))
            {
                return;
            }

            if (this.smartphoneApi.HandlePhoneSizeButtonsClick(x, y, this.xPositionOnScreen, this.yPositionOnScreen))
            {
                return;
            }

            this.lastScrollMouseY = y;
            this.touchScrollStartY = y;
            this.hasTouchScrolled = false;
            this.isScrolling = false;
        }

        public override void receiveScrollWheelAction(int direction)
        {
            base.receiveScrollWheelAction(direction);
            int scrollAmount = ScaleValue(40);
            if (direction > 0)
            {
                this.scrollOffset -= scrollAmount;
            }
            else if (direction < 0)
            {
                this.scrollOffset += scrollAmount;
            }
            this.scrollOffset = Math.Clamp(this.scrollOffset, 0, this.maxScroll);
        }

        public override void releaseLeftClick(int x, int y)
        {
            base.releaseLeftClick(x, y);

            if (!this.hasTouchScrolled)
            {
                Rectangle contentRect = GetContentBounds();
                if (contentRect.Contains(x, y))
                {
                    Rectangle actualSoloBounds = new Rectangle(
                        this.soloButtonBounds.X,
                        contentRect.Y - this.scrollOffset + this.soloButtonBounds.Y,
                        this.soloButtonBounds.Width,
                        this.soloButtonBounds.Height);

                    Rectangle actualDuoBounds = new Rectangle(
                        this.duoButtonBounds.X,
                        contentRect.Y - this.scrollOffset + this.duoButtonBounds.Y,
                        this.duoButtonBounds.Width,
                        this.duoButtonBounds.Height);

                    if (actualSoloBounds.Contains(x, y))
                    {
                        Game1.playSound("bigSelect");
                        Game1.activeClickableMenu = null;
                        Game1.currentMinigame = new AbigailGame();
                    }
                    else if (actualDuoBounds.Contains(x, y))
                    {
                        Game1.playSound("bigSelect");
                        Game1.activeClickableMenu = null;
                        Game1.currentMinigame = new AbigailGame(Game1.getCharacterFromName("Abigail"));
                    }
                }
            }

            this.isDragging = false;
            this.isScrolling = false;
        }

        public override void leftClickHeld(int x, int y)
        {
            base.leftClickHeld(x, y);

            if (!this.isDragging && !this.isScrolling)
            {
                Rectangle frameBounds = GetFrameBounds();
                Rectangle contentBounds = GetContentBounds();

                if (contentBounds.Contains(x, y))
                {
                    this.isScrolling = true;
                    this.lastScrollMouseY = y;
                }
                else if (frameBounds.Contains(x, y))
                {
                    this.isDragging = true;
                    this.dragOffsetX = x - this.xPositionOnScreen;
                    this.dragOffsetY = y - this.yPositionOnScreen;
                }
            }

            if (this.isScrolling)
            {
                if (Math.Abs(y - this.touchScrollStartY) > 5)
                    this.hasTouchScrolled = true;

                int deltaY = y - this.lastScrollMouseY;
                this.lastScrollMouseY = y;
                if (deltaY != 0)
                {
                    this.scrollOffset -= deltaY;
                    this.scrollOffset = Math.Clamp(this.scrollOffset, 0, this.maxScroll);
                }
            }
        }

        public override void update(GameTime time)
        {
            UpdateScaleAndDimensions();

            base.update(time);

            this.bounceTimer += (float)time.ElapsedGameTime.TotalSeconds;

            if (this.isDragging)
            {
                this.xPositionOnScreen = Game1.getMouseX() - this.dragOffsetX;
                this.yPositionOnScreen = Game1.getMouseY() - this.dragOffsetY;
                ClampToViewport();
                CalculateLayout();
                this.smartphoneApi.SetPhonePosition(this.xPositionOnScreen, this.yPositionOnScreen);
            }
        }

        private void UpdateScaleAndDimensions()
        {
            float currentScale = this.smartphoneApi.GetPhoneUiScale();
            if (currentScale != this.phoneUiScale)
            {
                this.phoneUiScale = currentScale;
                this.phoneFrameWidth = this.smartphoneApi.GetPhoneFrameWidth();
                this.phoneFrameHeight = this.smartphoneApi.GetPhoneFrameHeight();
                var (offX, offY) = this.smartphoneApi.GetPhoneContentOffset();
                this.phoneContentOffsetX = offX;
                this.phoneContentOffsetY = offY;
                this.phoneFrameTexture = this.smartphoneApi.GetPhoneFrameTexture();
                this.phoneBackgroundTexture = this.smartphoneApi.GetPhoneBackgroundTexture();

                this.width = this.phoneFrameWidth;
                this.height = this.phoneFrameHeight;

                if (this.phoneBackgroundTexture != null && !this.phoneBackgroundTexture.IsDisposed)
                {
                    this.contentWidth = (int)Math.Round(this.phoneBackgroundTexture.Width * this.phoneUiScale);
                    this.contentHeight = (int)Math.Round(this.phoneBackgroundTexture.Height * this.phoneUiScale);
                }
                else
                {
                    this.contentWidth = Math.Max(1, this.phoneFrameWidth - (this.phoneContentOffsetX * 2));
                    this.contentHeight = Math.Max(1, this.phoneFrameHeight - this.phoneContentOffsetY - ScaleValue(80));
                }

                CalculateLayout();
            }
        }

        public override void receiveKeyPress(Microsoft.Xna.Framework.Input.Keys key)
        {
            if (key == Microsoft.Xna.Framework.Input.Keys.Escape)
            {
                this.onBack?.Invoke();
                return;
            }

            string keyStr = key.ToString();
            if (keyStr == this.smartphoneApi.GetDecreaseSizeKey())
            {
                this.smartphoneApi.AdjustPhoneSize(-0.1f);
                return;
            }
            if (keyStr == this.smartphoneApi.GetIncreaseSizeKey())
            {
                this.smartphoneApi.AdjustPhoneSize(0.1f);
                return;
            }

            base.receiveKeyPress(key);
        }

        private void ClampToViewport()
        {
            this.xPositionOnScreen = Math.Max(0, Math.Min(this.xPositionOnScreen, Game1.uiViewport.Width - this.width));
            this.yPositionOnScreen = Math.Max(0, Math.Min(this.yPositionOnScreen, Game1.uiViewport.Height - this.height));
        }
    }
}
