namespace Minecraft.Core.Utilities.Spatial;

public enum Direction : byte
{
    Back = 0,  //Side facing negative Z
    Right = 1, //Side facing positive X
    Front = 2, //Side facing positive Z
    Left = 3,  //Side facing negative X
    Top = 4,   //Side facing positive Y
    Bottom = 5 //Side facing negative Y
};
