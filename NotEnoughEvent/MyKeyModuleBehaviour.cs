using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Modding.Blocks;
using Modding.Modules;
using Modding.Serialization;
using UnityEngine;

namespace NEE.Blocks
{
    using System.Xml.Serialization;

    [XmlRoot("MyKeyModule")]
    public class MyKeyModule : BlockModule
    {
        [XmlElement][RequireToValidate] public MKeyReference MyMod_ActionKey;
    }

    public class MyKeyModuleBehaviour : BlockModuleBehaviour<MyKeyModule>
    {
        private MKey actionKey;

        public override void SafeAwake()
        {
        }

        private MKey[] keys;

        public override void OnSimulateStart()
        {
            visualUpdateTime = Time.time + VISUAL_RATE_TIME;
            actionKey = GetKey(Module.MyMod_ActionKey);

            ReadOnlyCollection<Block> blocks = Machine.SimulationBlocks;
            HashSet<MKey> keysHashSet = new HashSet<MKey>();
            foreach (Block block in blocks)
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
                    fuelConsumption += cost.costFuel;
                }
            }
            keys = keysHashSet.ToArray();

            maxFuel = fuelCount;
            criticalFuelLevel = maxFuel * 0.1f;

            isDisabled = false;
        }

        public override void OnSimulateStop()
        {
        }

        private float maxFuel = 0;
        private float fuelCount = 0;
        private float criticalFuelLevel = 0;

        private float fuelConsumption = 1;
        private bool isDisabled = false;

        private float visualUpdateTime = 0;
        private const float VISUAL_RATE_TIME = 0.2f;

        private void Update()
        {
            if (!IsSimulating)
            {
                return;
            }

            bool isHeld = false;
            for (int i = 0; i < keys.Length; i++)
            {
                if (keys[i].IsHeld)
                {
                    isHeld = true;
                    break;
                }
            }

            if (isHeld && !isDisabled)
            {
                fuelCount -= fuelConsumption * Time.deltaTime;
                if (fuelCount < 0)
                {
                    fuelCount = 0;
                    DisableAll();
                    isDisabled = true;
                }
            }

            if (visualUpdateTime < Time.time)
            {
                UpdateVisual();
                visualUpdateTime = Time.time + VISUAL_RATE_TIME;
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
            string output = $"fuel : {fuelCount:0.00} [{fuelConsumption:0.00}/s]\nleft {Math.Truncate(t.TotalMinutes):00}:{t.Seconds:00}" + (fuelCount < criticalFuelLevel ? "\nCRITICAL!" : "");
            StringConsoleGui.Instance.SetString(output);
        }

        private void DisableAll()
        {
            ReadOnlyCollection<Block> blocks = Machine.SimulationBlocks;
            foreach (Block block in blocks)
            {
                List<MKey> keyList = block.InternalObject.KeyList;
                foreach (MKey mKey in keyList)
                {
                    mKey.ignored = true;
                }
            }
        }
    }
}