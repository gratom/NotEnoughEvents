using System.Collections.Generic;
using System.Collections.ObjectModel;
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

    public class MyKeyModuleBehaviour
        : BlockModuleBehaviour<MyKeyModule>
    {
        private MKey actionKey;

        public override void SafeAwake()
        {
            actionKey = GetKey(Module.MyMod_ActionKey);
        }

        public override void OnSimulateStart()
        {
            actionKey = GetKey(Module.MyMod_ActionKey);
            fuelCount = 100;
            isDisabled = false;
        }

        public override void OnSimulateStop()
        {
            // при необходимости
        }

        private float fuelCount = 100;
        private bool isDisabled = false;

        private void Update()
        {
            if (!IsSimulating)
            {
                return;
            }
            StringConsoleGui.Instance.SetString($"fuel : {fuelCount:0.00}");
            if (actionKey.IsDown && !isDisabled)
            {
                fuelCount -= 1 * Time.deltaTime;
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
                List<MKey> keyList = block.InternalObject.KeyList;
                foreach (MKey mKey in keyList)
                {
                    mKey.ignored = true;
                }
            }
        }
    }

    public class FuelTank : Modding.BlockScript
    {
        public override void OnBlockPlaced()
        {
            base.OnBlockPlaced();
            Debug.Log("FuelTank OnBlockPlaced");
        }
    }
}