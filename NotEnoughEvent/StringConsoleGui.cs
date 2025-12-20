using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using InternalModding.Blocks;
using Modding;
using Modding.Blocks;
using Modding.Common;
using NEE.Blocks;
using UnityEngine;

namespace NEE
{
    public class StringConsoleGui : SingleInstance<StringConsoleGui>
    {
        public override string Name => "StringConsoleGui";

        private Rect windowRect = new Rect(0, 80, 400, 250);
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

        private EngineBlock cachedEngineBlock;

        public void CacheEngineBlock()
        {
            if (StatMaster.isMP)
            {
                ReadOnlyCollection<Block> blocks = Player.GetLocalPlayer().Machine.SimulationBlocks;
                for (int i = 0; i < blocks.Count; i++)
                {
                    if (blocks[i].ToBlockCost().type == 1051)
                    {
                        cachedEngineBlock = blocks[i].InternalObject.gameObject.GetComponent<EngineBlock>();
                    }
                }
            }
            else
            {
                List<BlockBehaviour> blocks = Machine.Active().SimulationBlocks;
                for (int i = 0; i < blocks.Count; i++)
                {
                    if (blocks[i].ToBlockCost()?.type == 1051)
                    {
                        cachedEngineBlock = blocks[i].gameObject.GetComponent<EngineBlock>();
                    }
                }
            }
        }

        public void UpdateState()
        {
            if (cachedEngineBlock != null)
            {
                SetString(cachedEngineBlock.machineEngineOutput);
            }
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

        public bool IsFixed => isFixed;
        private bool isFixed = false;

        private void DrawWindow(int id)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Resources", textStyle);
            GUILayout.FlexibleSpace();
            isFixed = GUILayout.Toggle(
                isFixed,
                "fixed",
                GUILayout.Width(60)
            );
            GUILayout.EndHorizontal();

            GUILayout.Space(5);
            GUILayout.BeginHorizontal();
            if (isFixed)
            {
                DrawResourceField("G", ref StaticRes.Data.gold);
                DrawResourceField("W", ref StaticRes.Data.wood);
                DrawResourceField("S", ref StaticRes.Data.steel);
                DrawResourceField("F", ref StaticRes.Data.fabric);
            }
            else
            {
                DrawResourceLabel("G", StaticRes.Data.gold - MainEventer.Instance.machineGold);
                DrawResourceLabel("W", StaticRes.Data.wood - MainEventer.Instance.machineWood);
                DrawResourceLabel("S", StaticRes.Data.steel - MainEventer.Instance.machineSteel);
                DrawResourceLabel("F", StaticRes.Data.fabric - MainEventer.Instance.machineFabric);
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(10);
            GUILayout.Label(currentText, textStyle);
            int goldPriceFuel = (int)(Math.Floor(MainEventer.Instance.machineFuel * Consts.FUEL_COST) + 1);
            int amortization = (int)(Math.Floor(MainEventer.Instance.machineGold * Consts.AMORTIZATION) + 1);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button($"Pay fuel + amortization [{goldPriceFuel + amortization}g]"))
            {
                StaticRes.Data.gold -= goldPriceFuel + amortization;
            }

            // if (StatMaster.SimulationState == SimulationState.GlobalSimulation || StatMaster.SimulationState == SimulationState.LocalSimulation)
            // {
            //     if (GUILayout.Button($"Pay fuel [{goldPriceFuel}g]"))
            //     {
            //         Block b = Player.GetLocalPlayer().Machine.SimulationBlocks.FirstOrDefault(x => x.ToBlockCost().type == 1051);
            //         if (b != null)
            //         {
            //             Debug.Log( $"script : {b.BlockScript.GetType()} | internal : {b.InternalObject.GetType()} | ");
            //             // StaticRes.Data.gold -= goldPriceFuel;
            //             // (b.InternalObject as EngineBlock)
            //         }
            //     }
            // }
            GUILayout.EndHorizontal();

            GUILayout.Space(10);
            GUILayout.Label("Random orders");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Ground"))
            {

            }
            if (GUILayout.Button("Water"))
            {

            }
            if (GUILayout.Button("Air"))
            {

            }
            GUILayout.EndHorizontal();

            if (GUILayout.Button("Patch triggers"))
            {
                List<InsigniaTrigger> list = Ext.FindAllWithComponent<InsigniaTrigger>();
                foreach (InsigniaTrigger trigger in list)
                {
                    if (trigger.logicName.Value == "REFRESH")
                    {
                        if (trigger.gameObject.GetComponent<TriggerExtension>() == null)
                        {
                            trigger.gameObject.AddComponent<TriggerExtension>();
                        }
                    }
                }
            }

            GUI.DragWindow();
        }

        private void DrawResourceField(string label, ref int value)
        {
            GUILayout.Label(label, GUILayout.Width(15));

            string text = value.ToString();
            string newText = GUILayout.TextField(text, 6, GUILayout.Width(60));

            if (newText != text)
            {
                if (int.TryParse(newText, out int parsed))
                {
                    value = parsed;
                }
            }

            GUILayout.Space(15);
        }

        private void DrawResourceLabel(string label, int value)
        {
            GUILayout.Label(label, GUILayout.Width(15));
            GUILayout.Label(value.ToString(), GUILayout.Width(60));
            GUILayout.Space(15);
        }


    }
}