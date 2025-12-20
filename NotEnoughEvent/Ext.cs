using System;
using System.Collections.Generic;
using System.Text;
using Modding.Blocks;
using Modding.Common;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace NEE
{
    public static class Ext
    {
        public static bool IsFuelable(this Block block)
        {
            return block.ToBlockCost()?.fuelConsumption > 0;
        }

        public static bool IsFuelConsumptionNow(this Block block)
        {
            if (block == null || block.InternalObject == null)
            {
                return false;
            }

            SteeringWheel steeringWheel = block.InternalObject as SteeringWheel;
            if (steeringWheel != null)
            {
                return steeringWheel.AutomaticToggle?.IsActive ?? false;
            }

            CogMotorControllerHinge cog = block.InternalObject as CogMotorControllerHinge;
            if (cog)
            {
                return cog.AutomaticToggle.IsActive || cog.Input != 0;
            }

            FlyingController fly = block.InternalObject as FlyingController;
            if (fly)
            {
                return fly.AutomaticToggle.IsActive || fly.flying;
            }

            return false;
        }

        public static void TryStop(this Block block)
        {
            List<MKey> keyList = block.InternalObject.KeyList;
            foreach (MKey mKey in keyList)
            {
                mKey.Ignored = true;
            }

            SteeringWheel steer = block.InternalObject as SteeringWheel;
            if (steer != null)
            {
                steer.targetAngleMode = false;
                if (steer.AutomaticToggle != null)
                {
                    steer.AutomaticToggle.IsActive = false;
                }
                steer.UpdateBlock();
                return;
            }

            CogMotorControllerHinge cog = block.InternalObject as CogMotorControllerHinge;
            if (cog != null)
            {
                cog.motor.freeSpin = false;
                cog.Input = 0;
                cog.AutomaticToggle.IsActive = false;
                cog.UpdateBlock();
                return;
            }

            FlyingController fly = block.InternalObject as FlyingController;
            if (fly != null)
            {
                fly.canFly = false;
                fly.UpdateBlock();
                return;
            }

            SqrBalloonController bal = block.InternalObject as SqrBalloonController;
            if (bal != null)
            {
                bal.keyInputSpeed = 0f;
                bal.UpdateBlock();
                return;
            }
        }

        public static BlockCost ToBlockCost(this Block block)
        {
            if (block == null)
            {
                return null;
            }
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
            ChatView chatView = Object.FindObjectOfType<ChatView>();
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

        public static void LogCurrentSceneHierarchy(string nameFilter)
        {
            Scene scene = SceneManager.GetActiveScene();
            GameObject[] roots = scene.GetRootGameObjects();

            foreach (GameObject root in roots)
            {
                LogTransformRecursive(root.transform, 0, nameFilter);
            }
        }

        public static void LogTransformRecursive(
            Transform transform,
            int depth,
            string nameFilter = "")
        {
            if (transform.name.IndexOf(nameFilter, StringComparison.OrdinalIgnoreCase) >= 0 || nameFilter == "")
            {
                string indent = new string(' ', depth * 2);
                string components = GetComponentsString(transform.gameObject);

                Debug.Log($"{indent}- {transform.name} ({components})");
            }

            for (int i = 0; i < transform.childCount; i++)
            {
                LogTransformRecursive(transform.GetChild(i), depth + 1, nameFilter);
            }
        }

        private static string GetComponentsString(GameObject gameObject)
        {
            Component[] components = gameObject.GetComponents<Component>();
            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < components.Length; i++)
            {
                if (i > 0)
                {
                    sb.Append(", ");
                }

                Component component = components[i];
                sb.Append(component.GetType());
            }

            return sb.ToString();
        }

        public static List<T> FindAllWithComponent<T>() where T : Component
        {
            Scene scene = SceneManager.GetActiveScene();
            GameObject[] roots = scene.GetRootGameObjects();

            List<T> result = new List<T>();

            foreach (GameObject root in roots)
            {
                FindRecursive(root.transform, result);
            }

            return result;
        }

        private static void FindRecursive<T>(
            Transform transform,
            List<T> result) where T : Component
        {
            T component = transform.GetComponent<T>();
            if (component != null)
            {
                result.Add(component);
            }

            for (int i = 0; i < transform.childCount; i++)
            {
                FindRecursive(transform.GetChild(i), result);
            }
        }
    }
}