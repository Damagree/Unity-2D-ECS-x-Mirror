using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using UnityEngine;

public partial class AgentSpawnSystem : SystemBase
{
    protected override void OnStartRunning()
    {
        var gm = GameManager.Instance;
        var em = World.DefaultGameObjectInjectionWorld.EntityManager;

        for (int i = 0; i < gm.agentCount; i++)
        {
            Entity e = em.CreateEntity(
                typeof(Agent),
                typeof(LocalTransform)
            );

            float2 pos = new float2(
                UnityEngine.Random.Range(-8f, 8f),
                UnityEngine.Random.Range(-8f, 8f)
            );

            em.SetComponentData(e, new LocalTransform
            {
                Position = new float3(pos.x, pos.y, 0),
                Rotation = quaternion.identity,
                Scale = 1
            });

            em.SetComponentData(e, new Agent
            {
                ID = i,
                Target = pos,
                Speed = UnityEngine.Random.Range(gm.minSpeed, gm.maxSpeed),
                RepathTimer = 0f
            });
        }
    }

    protected override void OnUpdate() { }
}
