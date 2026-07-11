using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "MapDatabase", menuName = "Scriptable Objects/MapDatabase")]
public class MapDatabase : ScriptableObject
{
    public List<MapData> maps;
}
