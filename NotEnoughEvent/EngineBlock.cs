using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using Modding.Blocks;
using Modding.Common;
using UnityEngine;

namespace NEE.Blocks
{
    public class EngineBlock : Modding.BlockScript
    {
        public override void OnBlockPlaced()
        {
            base.OnBlockPlaced();
            Debug.Log("Engine OnBlockPlaced");
        }

        private MKey[] keys;
        private Block[] fuelableBlocks;

        private float maxFuel = 0;
        private float fuelCount = 0;
        private float criticalFuelLevel = 0;

        private float fuelConsumption = 1;
        private bool isDisabled = false;

        private float visualUpdateTime = 0;
        private const float VISUAL_RATE_TIME = 0.2f;

        public string machineEngineOutput;

        public override void OnSimulateStart()
        {
            visualUpdateTime = Time.time + VISUAL_RATE_TIME;

            ReadOnlyCollection<Block> blocks;
            if (StatMaster.isMP)
            {
                blocks = Player.GetLocalPlayer().Machine.SimulationBlocks;
            }
            else
            {
                blocks = Machine.SimulationBlocks;
            }
            HashSet<MKey> keysHashSet = new HashSet<MKey>();
            List<Block> blks = new List<Block>();
            foreach (Block block in blocks)
            {
                if (block.IsFuelable())
                {
                    List<MKey> keyList = block.InternalObject.KeyList;
                    foreach (MKey mKey in keyList)
                    {
                        keysHashSet.Add(mKey);
                    }

                    BlockCost cost = block.ToBlockCost();
                    if (cost != null)
                    {
                        fuelCount += cost.fuelCount;
                        fuelConsumption += cost.fuelConsumption;
                    }

                    blks.Add(block);
                }
            }
            keys = keysHashSet.ToArray();
            fuelableBlocks = blks.ToArray();
            maxFuel = fuelCount;
            criticalFuelLevel = maxFuel * 0.1f;

            StringConsoleGui.Instance.CacheEngineBlock();

            isDisabled = false;
        }

        private void Update()
        {
            if (!IsSimulating)
            {
                return;
            }

            if (visualUpdateTime < Time.time)
            {
                UpdateVisual();
                visualUpdateTime = Time.time + VISUAL_RATE_TIME;
            }

            if (isDisabled)
            {
                return;
            }

            bool isFuelConsumption = false;
            for (int i = 0; i < fuelableBlocks.Length; i++)
            {
                if (fuelableBlocks[i].IsFuelConsumptionNow())
                {
                    isFuelConsumption = true;
                    goto fuelPart;
                }
            }

            for (int i = 0; i < keys.Length; i++)
            {
                if (keys[i].IsHeld)
                {
                    isFuelConsumption = true;
                    goto fuelPart;
                }
            }

            fuelPart:
            if (isFuelConsumption)
            {
                fuelCount -= fuelConsumption * Time.deltaTime;
                if (fuelCount < 0)
                {
                    fuelCount = 0;
                    DisableAll();
                    isDisabled = true;
                }
            }
        }

        private void DisableAll()
        {
            ReadOnlyCollection<Block> blocks = Machine.SimulationBlocks;
            foreach (Block block in blocks)
            {
                block.TryStop();
            }
        }

        private void UpdateVisual()
        {
            float timeSec = 0;
            if (fuelConsumption != 0)
            {
                timeSec = fuelCount / fuelConsumption;
            }
            TimeSpan t = TimeSpan.FromSeconds(Math.Truncate(timeSec));
            machineEngineOutput = $"fuel : {fuelCount:0.00} [{fuelConsumption:0.00}/s]\nleft {Math.Truncate(t.TotalMinutes):00}:{t.Seconds:00}" + (fuelCount < criticalFuelLevel ? "\nCRITICAL!" : "");

            StringConsoleGui.Instance.UpdateState();
        }

    }
}