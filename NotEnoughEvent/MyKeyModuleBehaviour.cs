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
        private Block[] fuelableBlocks;

        public override void OnSimulateStart()
        {
            visualUpdateTime = Time.time + VISUAL_RATE_TIME;
            actionKey = GetKey(Module.MyMod_ActionKey);

            ReadOnlyCollection<Block> blocks = Machine.SimulationBlocks;
            HashSet<MKey> keysHashSet = new HashSet<MKey>();
            List<Block> blks = new List<Block>();
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
                    fuelConsumption += cost.fuelConsumption;
                }

                if (block.IsFuelable())
                {
                    blks.Add(block);
                }
            }
            keys = keysHashSet.ToArray();
            fuelableBlocks = blks.ToArray();
            maxFuel = fuelCount;
            criticalFuelLevel = maxFuel * 0.1f;

            isDisabled = false;
        }

        public override void OnSimulateStop()
        {
        }

        public override void OnReload()
        {
            // float countOfNeededFuel = maxFuel - fuelCount;
            // int goldPrice = (int)(Math.Floor(countOfNeededFuel * Consts.FUEL_COST) + 1);
            // if (goldPrice < StaticRes.Data.gold)
            // {
            //     StaticRes.Data.gold -= goldPrice;
            // }
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
                if (KeysControlExtension.IsFuelConsumptionNow(fuelableBlocks[i]))
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
                block.TryStop();
            }
        }
    }

    public static class KeysControlExtension
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
    }
}