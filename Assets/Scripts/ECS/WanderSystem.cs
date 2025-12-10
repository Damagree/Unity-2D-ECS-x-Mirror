using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine.AI;

[BurstCompile]
public partial struct WanderSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        float dt = SystemAPI.Time.DeltaTime;
        var gm = GameManager.Instance;

        foreach (var (agent, transform) in
            SystemAPI.Query<RefRW<Agent>, RefRW<LocalTransform>>())
        {
            float2 pos = transform.ValueRW.Position.xy;
            float2 dir = math.normalize(agent.ValueRW.Target - pos);


            if (float.IsNaN(pos.x) || float.IsNaN(pos.y))
                return;

            pos += dir * agent.ValueRW.Speed * dt;
            transform.ValueRW.Position.xy = pos;

            agent.ValueRW.RepathTimer -= dt;

            if (math.distance(pos, agent.ValueRW.Target) < 0.25f ||
                agent.ValueRW.RepathTimer <= 0f)
            {
                float angle = UnityEngine.Random.Range(0f, math.PI * 2f);
                float2 offset = new float2(math.cos(angle), math.sin(angle)) * gm.wanderRadius;
                float2 candidate = pos + offset;

                if (NavMeshPlusManager.Instance.SamplePosition(candidate, out var hit, 1.5f, NavMesh.AllAreas))
                {
                    agent.ValueRW.Target = new(hit.x, hit.y);
                }
                else
                {
                    agent.ValueRW.Target = pos;
                }

                agent.ValueRW.RepathTimer = gm.repathInterval;
            }
        }
    }
}
