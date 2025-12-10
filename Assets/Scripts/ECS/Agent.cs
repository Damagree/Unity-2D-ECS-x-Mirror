using Unity.Entities;
using Unity.Mathematics;

public struct Agent : IComponentData
{
    public int ID;
    public float2 Target;
    public float Speed;
    public float RepathTimer;
}
