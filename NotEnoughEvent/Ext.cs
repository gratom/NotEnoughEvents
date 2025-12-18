using System.Collections.Generic;
using Modding.Blocks;
using Modding.Common;
using UnityEngine;

namespace NEE
{
    public static class Ext
    {
        public static BlockCost ToBlockCost(this Block block)
        {
            BlockCost b;
            if (Consts.BlocksDict.TryGetValue(block.Prefab.Type, out b))
            {
                return b;
            }
            string nm = block.Prefab.Name;
            if (nm.Contains(Consts.ModID))
            {
                string str = nm.Substring(nm.Length - 4);
                if (int.TryParse(str, out int type))
                {
                    if (Consts.BlocksDict.TryGetValue(type, out b))
                    {
                        return b;
                    }
                }
            }
            return null;
        }

        public static BlockCost ToBlockCost(this BlockBehaviour block)
        {
            BlockCost b;
            if (Consts.BlocksDict.TryGetValue((int)block.Prefab.Type, out b))
            {
                return b;
            }
            string nm = block.Prefab.name;
            if (nm.Contains(Consts.ModID))
            {
                string str = nm.Substring(nm.Length - 4);
                if (int.TryParse(str, out int type))
                {
                    if (Consts.BlocksDict.TryGetValue(type, out b))
                    {
                        return b;
                    }
                }
            }
            return null;
        }

        public static void ToChat(this string text, int saymode = 1, MPTeam team = MPTeam.Red)
        {
            IChatController chatController = ReferenceMaster.ChatController;
            ChatView chatView = UnityEngine.Object.FindObjectOfType<ChatView>();
            Color color = chatView.ChatTeamColors[(int)team];

            Player player = Player.GetLocalPlayer();
            List<Player> players = Player.GetAllPlayers();
            foreach (Player p in players)
            {
                if (p.Team == team)
                {
                    player = p;
                    break;
                }
            }

            switch (saymode)
            {
                case 0: //Host
                    text = string.Format("<color=#{0}>{1}:  </color><color=#D3D3D3FF>{2}</color>", ColorUtility.ToHtmlStringRGBA(color), player.Name, text);
                    chatView.AddTextEntry(text);
                    break;
                case 1: //ALL
                    text = string.Format("<color=#{0}>{1}:  </color>{2}", ColorUtility.ToHtmlStringRGBA(color), player.Name, text);
                    chatController.HandleSayCommand(player.InternalObject, ChatMode.Global, text);
                    break;
                case 2: //Team
                    text = string.Format("<color=#{0}>{1}:  {2}</color>", ColorUtility.ToHtmlStringRGBA(color), player.Name, text);
                    chatController.HandleSayCommand(player.InternalObject, ChatMode.Team, text);
                    break;
            }
        }
    }
}