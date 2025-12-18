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

        private void OnDestroy()
        {
            StaticRes.Save();
        }

        private static List<Player> players;

        private void UpdatePlayers(Player _)
        {
            players = Player.GetAllPlayers();
        }

        public int machineGold { get; private set; }
        public int machineWood { get; private set; }
        public int machineSteel { get; private set; }
        public int machineFabric { get; private set; }

        public int machineFuel { get; private set; }
        
        private void UpdateBlocksCost(Block blck, bool isRemoving)
        {
            float sumGold = 0;
            float sumWood = 0;
            float sumSteel = 0;
            float sumFabric = 0;

            float sumFuelConsumption = 0;
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

            machineGold = (int)sumGold;
            machineWood = (int)sumWood;
            machineSteel = (int)sumSteel;
            machineFabric = (int)sumFabric;
            machineFuel = (int)sumFuelCount;
            
            //smth else
            if (!gui.IsFixed)
            {
            }


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
                sumFuelConsumption += b.fuelConsumption;
                sumFabric += b.costFabric;
                sumFuelCount += b.fuelCount;
            }

            string CostCombine()
            {

                float sec = 0;
                if (sumFuelConsumption != 0)
                {
                    sec = sumFuelCount / sumFuelConsumption;
                }
                TimeSpan t = TimeSpan.FromSeconds(Math.Truncate(sec));
                return $"<b>Machine cost:</b>\n" +
                       $"<color=#FFD700>   Gold:</color> {sumGold}\n" +
                       $"<color=#3b2715>   Wood:</color> {sumWood}\n" +
                       $"<color=#B0B0B0>   Steel:</color> {sumSteel}\n" +
                       $"<color=#C0A0FF>   Fabric:</color> {sumFabric}\n" +
                       $"<b>Fuels stats:</b>\n" +
                       $"<color=#FF6A00>   Fuel consumption:</color> {sumFuelConsumption:0.00} [{Math.Truncate(t.TotalMinutes):00}:{t.Seconds:00}] \n" +
                       $"<color=#ff984f>   Fuel count:</color> {sumFuelCount}\n";
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

}