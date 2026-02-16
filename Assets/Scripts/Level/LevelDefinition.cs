using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "UnblockJam/LevelDefinition", fileName = "LevelDefinition")]
public class LevelDefinition : ScriptableObject
{
    [Serializable]
    public class BlockSpawn
    {
        public string id = "Block";
        public GameObject prefab;

        public Vector2Int size = new Vector2Int(1, 1);
        public BlockColor color = BlockColor.White;
        public MoveAxis moveAxis = MoveAxis.Free;

        [Tooltip("Bottom-left anchor cell")]
        public Vector2Int anchor;
    }

    public List<BlockSpawn> blocks = new List<BlockSpawn>();
}
