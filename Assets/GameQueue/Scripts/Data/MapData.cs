using System;
using System.Collections.Generic;

[Serializable]
public class MapData
{
    public int MapLevel;
    public int DummyType;
    public List<int> Map;
    public int NumQueue;
    public int NumPerRow;
    public int MaxMove;
}