using Unity.Entities;
using Unity.Mathematics;

[InternalBufferCapacity(8)]
public struct PathCorner : IBufferElementData
{
    public float2 pos;
}
