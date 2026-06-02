using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace SmartphoneAppGame.Data
{
    public interface ISmartPhoneApi
    {
        bool RegisterPhoneAppGroup(
            string ownerModId,
            string groupId,
            string displayName,
            Texture2D iconTexture,
            int sortOrder = 0,
            Rectangle? sourceRect = null,
            Func<bool>? isVisible = null,
            Func<int>? getBadgeCount = null
        );
        
        bool RegisterPhoneAppGroupItem(
            string ownerModId,
            string groupId,
            string itemId,
            string displayName,
            Texture2D iconTexture,
            Action onClick,
            bool closePhoneOnLaunch = true,
            int sortOrder = 0,
            Rectangle? sourceRect = null,
            Func<bool>? isVisible = null,
            Func<int>? getBadgeCount = null
        );

        bool RegisterChatQuickActionButton(
            string ownerModId,
            string actionId,
            Texture2D iconTexture,
            Action<string> onClick,
            bool closePhoneOnLaunch = false,
            int sortOrder = 0,
            Rectangle? sourceRect = null,
            List<string>? npcNames = null
        );


        void SendSmartphoneMessageFromNPC(string npcName, string message, string playerId = "");

        void SendSmartphoneNotification(string message, string notificationName = "", string playerId = "");
    }
}
