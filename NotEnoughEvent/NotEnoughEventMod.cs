using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Modding;
using Modding.Blocks;
using Modding.Common;
using Modding.Modules;
using NEE.Blocks;
using UnityEngine;

namespace NEE
{
    public class Mod : ModEntryPoint
    {
        private GameObject loader;

        public override void OnLoad()
        {
            loader = GameObject.Find("NotEnoughEvent");
            if (loader != null)
            {
                return;
            }

            InitModules();

            loader = new GameObject("NotEnoughEvent");
            UnityEngine.Object.DontDestroyOnLoad(loader);
            MainEventer eventer = loader.AddComponent<MainEventer>();
            StringConsoleGui gui = loader.AddComponent<StringConsoleGui>();
            eventer.gui = gui;
        }

        private void InitModules()
        {
            CustomModules.AddBlockModule<MyKeyModule, MyKeyModuleBehaviour>(
                "MyKeyModule",
                false
            );
        }
    }

    public class MainEventer : SingleInstance<MainEventer>
    {
        public override string Name => "MainEventer";
        public StringConsoleGui gui;

        protected void Start()
        {
            UpdatePlayers(null);

            Events.OnSimulationToggle += EventsOnSimulationToggle;
            Events.OnPlayerJoin += UpdatePlayers;
            Events.OnPlayerLeave += UpdatePlayers;
            Events.OnBlockPlaced += block => UpdateBlocksCost(block, false);
            Events.OnBlockRemoved += block => UpdateBlocksCost(block, true);
        }

        private static List<Player> players;

        private void UpdatePlayers(Player _)
        {
            players = Player.GetAllPlayers();
        }

        private void UpdateBlocksCost(Block blck, bool isRemoving)
        {
            float sumGold = 0;
            float sumSteel = 0;
            float sumWood = 0;
            float sumFuel = 0;
            float sumFabric = 0;
            float sumFuelCount = 0;

            //update Cost and UI
            if (!StatMaster.isMP)
            {
                Solo();
            }
            else
            {
                Multiplayer();
            }

            //smth else


            void Solo()
            {
                List<BlockBehaviour> blocks = Machine.Active().BuildingBlocks;
                for (int i = 0; i < blocks.Count; i++)
                {
                    BlockBehaviour block = blocks[i];
                    BlockCost b = block.ToBlockCost();
                    if (b != null)
                    {
                        AddValues(b);
                    }
                    else
                    {
                        Debug.Log($"Unknown block : {block.Prefab.Type} : {block.Prefab.name}");
                    }
                }
                string cost = CostCombine();
                ToDebug(cost);
            }

            void Multiplayer()
            {
                Player player = Player.GetLocalPlayer();
                ReadOnlyCollection<Block> blocks = player.Machine.BuildingBlocks;
                for (int i = 0; i < blocks.Count; i++)
                {
                    Block block = blocks[i];
                    BlockCost b = block.ToBlockCost();
                    if (b != null)
                    {
                        AddValues(b);
                    }
                    else
                    {
                        Debug.Log($"Unknown block : {block.Prefab.Type} : {block.Prefab.Name}");
                    }
                }
                string cost = CostCombine();
                ToDebug(cost);
            }

            void AddValues(BlockCost b)
            {
                if (isRemoving && b.type == blck.ToBlockCost().type)
                {
                    isRemoving = false;
                    return;
                }
                sumGold += b.costGold;
                sumSteel += b.costSteel;
                sumWood += b.costWood;
                sumFuel += b.costFuel;
                sumFabric += b.costFabric;
                sumFuelCount += b.fuelCount;
            }

            string CostCombine()
            {

                float sec = 0;
                if (sumFuel != 0)
                {
                    sec = sumFuelCount / sumFuel;
                }
                TimeSpan t = TimeSpan.FromSeconds(Math.Truncate(sec));
                return $"<b>{Time.time:0.00} Machine cost:</b>\n" +
                       $"<color=#FFD700>Gold:</color> {sumGold}\n" +
                       $"<color=#B0B0B0>Steel:</color> {sumSteel}\n" +
                       $"<color=#3b2715>Wood:</color> {sumWood}\n" +
                       $"<color=#FF6A00>Fuel consumption:</color> {sumFuel:0.00} [{Math.Truncate(t.TotalMinutes):00}:{t.Seconds:00}] \n" +
                       $"<color=#C0A0FF>Fabric:</color> {sumFabric}\n" +
                       $"<color=#ff984f>Fuel count:</color> {sumFuelCount}\n";
            }
        }

        private void ToDebug(string str)
        {
            gui.SetString(str);
        }

        private void EventsOnSimulationToggle(bool simulationIsOn)
        {

        }
    }

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

    public class StringConsoleGui : SingleInstance<StringConsoleGui>
    {
        public override string Name => "StringConsoleGui";

        private Rect windowRect = new Rect(0, 80, 250, 170);
        private int windowID;

        private string currentText = string.Empty;

        private GUIStyle textStyle = new GUIStyle(GUIStyle.none);

        public void Awake()
        {
            windowID = ModUtility.GetWindowId();

            textStyle.fontSize = 14;
            textStyle.normal.textColor = Color.white;
            textStyle.alignment = TextAnchor.UpperLeft;
            textStyle.wordWrap = true;
            textStyle.richText = true;
            textStyle.padding = new RectOffset(6, 6, 6, 6);
        }

        public void SetString(string str)
        {
            currentText = str ?? string.Empty;
        }

        public void OnGUI()
        {
            if (StatMaster.isMainMenu)
            {
                return;
            }

            windowRect = GUILayout.Window(
                windowID,
                windowRect,
                DrawWindow,
                "Machine state"
            );
        }

        private void DrawWindow(int id)
        {
            GUILayout.Label(currentText, textStyle);
            GUI.DragWindow();
        }
    }

    public class BlockCost
    {
        public int type;
        public int costGold;
        public int costSteel;
        public int costWood;
        public int costFabric;
        public float costFuel;
        public float fuelCount;
    }

    public static class Consts
    {
        public const string ModID = "1bf3a164-b945-4dcf-8238-364dea7d30e4";

        public static Dictionary<int, BlockCost> BlocksDict = new Dictionary<int, BlockCost>()
        {
            { 0, new BlockCost() { type = 0, costGold = 0, costSteel = 0, costWood = 0, costFabric = 0, costFuel = 0 } }, // Starting Block 
            { 15, new BlockCost() { type = 15, costGold = 1, costSteel = 0, costWood = 1, costFabric = 0, costFuel = 0 } }, // Small Wooden Block 
            { 1, new BlockCost() { type = 1, costGold = 2, costSteel = 0, costWood = 2, costFabric = 0, costFuel = 0 } }, // Wooden Block 
            { 41, new BlockCost() { type = 41, costGold = 1, costSteel = 0, costWood = 1, costFabric = 0, costFuel = 0 } }, // Wooden Pole 
            { 63, new BlockCost() { type = 63, costGold = 3, costSteel = 0, costWood = 3, costFabric = 0, costFuel = 0 } }, // Log 
            { 7, new BlockCost() { type = 7, costGold = 5, costSteel = 1, costWood = 0, costFabric = 0, costFuel = 0 } }, // Brace 
            { 12, new BlockCost() { type = 12, costGold = 5, costSteel = 1, costWood = 0, costFabric = 0, costFuel = 0 } }, // Smooth Surface Block 
            { 28, new BlockCost() { type = 28, costGold = 10, costSteel = 2, costWood = 0, costFabric = 0, costFuel = 0.2f } }, // Steering Hinge 
            { 13, new BlockCost() { type = 13, costGold = 10, costSteel = 2, costWood = 0, costFabric = 0, costFuel = 0.2f } }, // Steering Block 
            { 2, new BlockCost() { type = 2, costGold = 4, costSteel = 0, costWood = 2, costFabric = 0, costFuel = 1f } }, // Powered Wheel 
            { 40, new BlockCost() { type = 40, costGold = 2, costSteel = 0, costWood = 1, costFabric = 0, costFuel = 0 } }, // Unpowered Wheel 
            { 46, new BlockCost() { type = 46, costGold = 7, costSteel = 0, costWood = 3, costFabric = 0, costFuel = 2f } }, // Powered Large Wheel 
            { 60, new BlockCost() { type = 60, costGold = 4, costSteel = 0, costWood = 2, costFabric = 0, costFuel = 0 } }, // Unpowered Large Wheel 
            { 50, new BlockCost() { type = 50, costGold = 1, costSteel = 0, costWood = 1, costFabric = 0, costFuel = 0 } }, // Small Wheel 
            { 38, new BlockCost() { type = 38, costGold = 3, costSteel = 1, costWood = 2, costFabric = 0, costFuel = 0 } }, // Unpowered Cog 
            { 39, new BlockCost() { type = 39, costGold = 6, costSteel = 2, costWood = 1, costFabric = 0, costFuel = 1f } }, // Powered Cog 
            { 51, new BlockCost() { type = 51, costGold = 5, costSteel = 2, costWood = 2, costFabric = 0, costFuel = 0 } }, // Unpowered Large Cog 
            { 19, new BlockCost() { type = 19, costGold = 5, costSteel = 1, costWood = 0, costFabric = 0, costFuel = 0 } }, // Swivel Joint 
            { 5, new BlockCost() { type = 5, costGold = 5, costSteel = 1, costWood = 0, costFabric = 0, costFuel = 0 } }, // Hinge 
            { 44, new BlockCost() { type = 44, costGold = 5, costSteel = 1, costWood = 0, costFabric = 0, costFuel = 0 } }, // Ball Joint 
            { 76, new BlockCost() { type = 76, costGold = 6, costSteel = 1, costWood = 0, costFabric = 0, costFuel = 0 } }, // Axle Linkage 
            { 22, new BlockCost() { type = 22, costGold = 8, costSteel = 2, costWood = 0, costFabric = 0, costFuel = 0.2f } }, // Spinning Block 
            { 16, new BlockCost() { type = 16, costGold = 7, costSteel = 2, costWood = 0, costFabric = 0, costFuel = 0 } }, // Suspension 
            { 42, new BlockCost() { type = 42, costGold = 3, costSteel = 1, costWood = 1, costFabric = 0, costFuel = 0 } }, // Slider 
            { 4, new BlockCost() { type = 4, costGold = 4, costSteel = 1, costWood = 0, costFabric = 0, costFuel = 0 } }, // Decoupler 
            { 18, new BlockCost() { type = 18, costGold = 10, costSteel = 2, costWood = 0, costFabric = 0, costFuel = 0 } }, // Piston 
            { 27, new BlockCost() { type = 27, costGold = 10, costSteel = 2, costWood = 0, costFabric = 0, costFuel = 0 } }, // Grabber 
            { 9, new BlockCost() { type = 9, costGold = 7, costSteel = 2, costWood = 0, costFabric = 0, costFuel = 0 } }, // Contractible Spring 
            { 45, new BlockCost() { type = 45, costGold = 6, costSteel = 0, costWood = 2, costFabric = 1, costFuel = 0 } }, // Winch 
            { 20, new BlockCost() { type = 20, costGold = 2, costSteel = 1, costWood = 0, costFabric = 0, costFuel = 0 } }, // Metal Spike 
            { 3, new BlockCost() { type = 3, costGold = 2, costSteel = 1, costWood = 0, costFabric = 0, costFuel = 0 } }, // Metal Blade 
            { 17, new BlockCost() { type = 17, costGold = 5, costSteel = 2, costWood = 0, costFabric = 0, costFuel = 0.5f } }, // Circular Saw 
            { 11, new BlockCost() { type = 11, costGold = 10, costSteel = 3, costWood = 0, costFabric = 0, costFuel = 0 } }, // Cannon 
            { 48, new BlockCost() { type = 48, costGold = 10, costSteel = 3, costWood = 0, costFabric = 0, costFuel = 0.5f } }, // Drill 
            { 77, new BlockCost() { type = 77, costGold = 6, costSteel = 2, costWood = 0, costFabric = 0, costFuel = 0 } }, // Metal Jaw 
            { 53, new BlockCost() { type = 53, costGold = 10, costSteel = 2, costWood = 0, costFabric = 0, costFuel = 0 } }, // Shrapnel Cannon 
            { 61, new BlockCost() { type = 61, costGold = 6, costSteel = 1, costWood = 2, costFabric = 1, costFuel = 0 } }, // Crossbow 
            { 21, new BlockCost() { type = 21, costGold = 6, costSteel = 2, costWood = 1, costFabric = 0, costFuel = 0 } }, // Flamethrower 
            { 62, new BlockCost() { type = 62, costGold = 6, costSteel = 1, costWood = 0, costFabric = 2, costFuel = 0 } }, // Vacuum 
            { 56, new BlockCost() { type = 56, costGold = 10, costSteel = 2, costWood = 1, costFabric = 0, costFuel = 0.5f } }, // Water Cannon 
            { 47, new BlockCost() { type = 47, costGold = 3, costSteel = 1, costWood = 0, costFabric = 0, costFuel = 0 } }, // Torch 
            { 23, new BlockCost() { type = 23, costGold = 15, costSteel = 0, costWood = 0, costFabric = 0, costFuel = 0 } }, // Bomb 
            { 54, new BlockCost() { type = 54, costGold = 10, costSteel = 0, costWood = 0, costFabric = 0, costFuel = 0 } }, // Remote Grenade 
            { 59, new BlockCost() { type = 59, costGold = 10, costSteel = 0, costWood = 0, costFabric = 0, costFuel = 0 } }, // Explosive Rocket 
            { 31, new BlockCost() { type = 31, costGold = 5, costSteel = 0, costWood = 0, costFabric = 0, costFuel = 0 } }, // Flaming Ball 
            { 36, new BlockCost() { type = 36, costGold = 5, costSteel = 0, costWood = 0, costFabric = 0, costFuel = 0 } }, // Boulder 
            { 24, new BlockCost() { type = 24, costGold = 2, costSteel = 1, costWood = 0, costFabric = 0, costFuel = 0 } }, // Armor Plate (Small) 
            { 32, new BlockCost() { type = 32, costGold = 3, costSteel = 2, costWood = 0, costFabric = 0, costFuel = 0 } }, // Armor Plate (Large) 
            { 29, new BlockCost() { type = 29, costGold = 3, costSteel = 2, costWood = 0, costFabric = 0, costFuel = 0 } }, // Armor Plate (Round) 
            { 10, new BlockCost() { type = 10, costGold = 1, costSteel = 0, costWood = 1, costFabric = 0, costFuel = 0 } }, // Wooden Panel 
            { 73, new BlockCost() { type = 73, costGold = 5, costSteel = 0, costWood = 4, costFabric = 0, costFuel = 0 } }, // Build Surface 
            { 49, new BlockCost() { type = 49, costGold = 1, costSteel = 0, costWood = 1, costFabric = 0, costFuel = 0 } }, // Grip Pad 
            { 33, new BlockCost() { type = 33, costGold = 4, costSteel = 2, costWood = 0, costFabric = 0, costFuel = 0 } }, // Plow 
            { 37, new BlockCost() { type = 37, costGold = 4, costSteel = 1, costWood = 0, costFabric = 0, costFuel = 0 } }, // Half Pipe 
            { 30, new BlockCost() { type = 30, costGold = 5, costSteel = 1, costWood = 0, costFabric = 0, costFuel = 0 } }, // Holder 
            { 6, new BlockCost() { type = 6, costGold = 4, costSteel = 1, costWood = 0, costFabric = 0, costFuel = 0 } }, // Spike Ball 
            { 14, new BlockCost() { type = 14, costGold = 8, costSteel = 0, costWood = 1, costFabric = 2, costFuel = 1.2f } }, // Flying Block 
            { 26, new BlockCost() { type = 26, costGold = 3, costSteel = 0, costWood = 2, costFabric = 0, costFuel = 0 } }, // Propeller 
            { 55, new BlockCost() { type = 55, costGold = 2, costSteel = 0, costWood = 1, costFabric = 0, costFuel = 0 } }, // Small Propeller 
            { 25, new BlockCost() { type = 25, costGold = 6, costSteel = 1, costWood = 1, costFabric = 3, costFuel = 0 } }, // Wing 
            { 34, new BlockCost() { type = 34, costGold = 4, costSteel = 0, costWood = 1, costFabric = 3, costFuel = 0 } }, // Wing Panel 
            { 35, new BlockCost() { type = 35, costGold = 5, costSteel = 1, costWood = 0, costFabric = 0, costFuel = 0 } }, // Ballast 
            { 1050, new BlockCost() { type = 1050, costGold = 5, costSteel = 1, costWood = 1, costFabric = 0, costFuel = 0, fuelCount = 1000 } }, // fuel tank
            { 1051, new BlockCost() { type = 1051, costGold = 5, costSteel = 1, costWood = 0, costFabric = 0, costFuel = 0 } }, // fuel tank
            { 43, new BlockCost() { type = 43, costGold = 3, costSteel = 0, costWood = 0, costFabric = 2, costFuel = 0 } }, // Balloon 
            { 74, new BlockCost() { type = 74, costGold = 30, costSteel = 0, costWood = 3, costFabric = 15, costFuel = 3.5f } }, // Hot Air Balloon 
            { 65, new BlockCost() { type = 65, costGold = 6, costSteel = 1, costWood = 1, costFabric = 0, costFuel = 0 } }, // Sensor 
            { 66, new BlockCost() { type = 66, costGold = 6, costSteel = 1, costWood = 1, costFabric = 0, costFuel = 0 } }, // Timer 
            { 67, new BlockCost() { type = 67, costGold = 6, costSteel = 1, costWood = 1, costFabric = 0, costFuel = 0 } }, // Altimeter 
            { 68, new BlockCost() { type = 68, costGold = 6, costSteel = 1, costWood = 1, costFabric = 0, costFuel = 0 } }, // Logic Gate 
            { 69, new BlockCost() { type = 69, costGold = 6, costSteel = 1, costWood = 1, costFabric = 0, costFuel = 0 } }, // Anglometer 
            { 70, new BlockCost() { type = 70, costGold = 6, costSteel = 1, costWood = 1, costFabric = 0, costFuel = 0 } }, // Speedometer 
            { 75, new BlockCost() { type = 75, costGold = 6, costSteel = 1, costWood = 1, costFabric = 0, costFuel = 0 } }, // Length Detector 
            { 57, new BlockCost() { type = 57, costGold = 0, costSteel = 0, costWood = 0, costFabric = 0, costFuel = 0 } }, // Pin Block 
            { 58, new BlockCost() { type = 58, costGold = 0, costSteel = 0, costWood = 0, costFabric = 0, costFuel = 0 } }, // Camera Block 
            { 71, new BlockCost() { type = 71, costGold = 0, costSteel = 0, costWood = 0, costFabric = 0, costFuel = 0 } }, // Wood plane dot 
            { 72, new BlockCost() { type = 72, costGold = 0, costSteel = 0, costWood = 0, costFabric = 0, costFuel = 0 } }, // Wood plane dot2 
            { 86, new BlockCost() { type = 86, costGold = 2, costSteel = 0, costWood = 1, costFabric = 0, costFuel = 0 } }, // skateboard wheel
            { 88, new BlockCost() { type = 88, costGold = 5, costSteel = 6, costWood = 0, costFabric = 0, costFuel = 0 } }, // fly wheel
            { 87, new BlockCost() { type = 87, costGold = 6, costSteel = 2, costWood = 1, costFabric = 0, costFuel = 0 } }, // bouncy pad
            { 89, new BlockCost() { type = 89, costGold = 4, costSteel = 0, costWood = 1, costFabric = 1, costFuel = 0 } }, // drag block
            { 85, new BlockCost() { type = 85, costGold = 3, costSteel = 0, costWood = 3, costFabric = 0, costFuel = 0 } }, // wooden corner block
            { 82, new BlockCost() { type = 82, costGold = 3, costSteel = 0, costWood = 3, costFabric = 0, costFuel = 0.1f } }, // buoyancy (smol)
            { 83, new BlockCost() { type = 83, costGold = 12, costSteel = 0, costWood = 10, costFabric = 0, costFuel = 0.3f } }, // buoyancy (big)
            { 79, new BlockCost() { type = 83, costGold = 4, costSteel = 1, costWood = 1, costFabric = 0, costFuel = 0.2f } }, //rudder (морской руль)
            { 80, new BlockCost() { type = 80, costGold = 4, costSteel = 0, costWood = 2, costFabric = 0, costFuel = 1f } }, //nautical screw
            { 81, new BlockCost() { type = 81, costGold = 1, costSteel = 0, costWood = 1, costFabric = 0, costFuel = 0 } }, //paddle (весло)
            { 78, new BlockCost() { type = 78, costGold = 12, costSteel = 1, costWood = 4, costFabric = 10, costFuel = 0 } }, //Sail
            { 84, new BlockCost() { type = 84, costGold = 10, costSteel = 3, costWood = 3, costFabric = 0, costFuel = 0 } } //harpoon
        };
    }
}