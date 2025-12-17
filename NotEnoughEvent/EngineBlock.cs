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
    }
}