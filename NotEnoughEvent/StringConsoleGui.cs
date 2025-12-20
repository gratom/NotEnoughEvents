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

        private int moneyMaxOrder = 0;
        private Order order = null;

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
            DrawResourceField("G", ref moneyMaxOrder);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("G0", GUILayout.Width(30)))
            {
                StaticRes.Data.gold--;
                order = OrderRandomizer.GenerateRandomOrder(PlaceTag.ground, 0, moneyMaxOrder);
            }
            if (GUILayout.Button("G1", GUILayout.Width(30)))
            {
                StaticRes.Data.gold--;
                order = OrderRandomizer.GenerateRandomOrder(PlaceTag.ground, 1, moneyMaxOrder);
            }
            if (GUILayout.Button("G2", GUILayout.Width(30)))
            {
                StaticRes.Data.gold--;
                order = OrderRandomizer.GenerateRandomOrder(PlaceTag.ground, 2, moneyMaxOrder);
            }
            if (GUILayout.Button("Water", GUILayout.Width(100)))
            {
                StaticRes.Data.gold -= 2;
                order = OrderRandomizer.GenerateRandomOrder(PlaceTag.water, 0, moneyMaxOrder);
            }
            if (GUILayout.Button("Air", GUILayout.Width(45)))
            {
                StaticRes.Data.gold -= 2;
                order = OrderRandomizer.GenerateRandomOrder(PlaceTag.air, 0, moneyMaxOrder);
            }
            GUILayout.EndHorizontal();

            DrawOrder(order, textStyle);

            GUILayout.Space(10);
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

        private void DrawOrder(Order order, GUIStyle textStyle)
        {
            if (order == null)
            {
                GUILayout.Label("<i>No active order</i>", textStyle);
                return;
            }

            // Маршрут
            GUILayout.Label(
                $"<b>{order.from.name}</b> → <b>{order.to.name}</b>",
                textStyle
            );

            // Товар
            GUILayout.Label(
                $"<color=#C0E0FF>Product:</color> {order.product.name} × {order.productCount}",
                textStyle
            );

            // Цены
            GUILayout.Label(
                $"<color=#FFD700>Buy:</color> {order.totalBuyPrice}   " +
                $"<color=#7CFC00>Sell:</color> {order.totalSellPrice}",
                textStyle
            );

            // Бонус
            if (order.additionPrice != 0)
            {
                GUILayout.Label(
                    $"<color=#87CEFA>Bonus:</color> {order.additionPrice}",
                    textStyle
                );
            }

            // Прибыль
            Color profitColor = new Color(0.5f, 1f, 0.5f);

            GUILayout.Label(
                $"<b><color=#{ColorUtility.ToHtmlStringRGB(profitColor)}>Profit: {order.totalProfit}</color></b>",
                textStyle
            );
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