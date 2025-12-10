using Unity.Entities;
using Unity.Mathematics;

public struct NavPathRequest : IComponentData
{
    public int RequestId;     // optional
    public float2 Start;
    public float2 End;
    public int AreaMask;
    public int MaxCorners;    // how many corners to store
}
