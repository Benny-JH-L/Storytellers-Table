
using System;
using System.Collections.Generic;
using UnityEngine;

public class WorldMap : MapBase
{
    public override TileBase CreateTileDataInstance(HexCoord hexCoord)
    {
        // Whenever the base generation method asks for data, pass the specialized object back
        WorldTile newWorldData = new WorldTile();
        newWorldData.stageMapID = $"Stage_Linked_To_{hexCoord.q}_{hexCoord.r}"; // ids need to be thought of still

        return newWorldData;
    }

    public override void Setup()
    {
        Debug.Log("world hey");
    }
}
