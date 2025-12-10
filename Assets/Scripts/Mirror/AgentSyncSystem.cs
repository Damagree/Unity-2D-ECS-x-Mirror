using Unity.Entities;
using Unity.Transforms;
using Mirror;
using System.Collections.Generic;

public partial class AgentSyncSystem : SystemBase
{
    float timer;

    protected override void OnUpdate()
    {
        timer += SystemAPI.Time.DeltaTime;
        if (timer < 0.05f) return; // 20 FPS sync
        timer = 0f;

        List<AgentState> list = new();

        foreach (var (agent, transform) in
                 SystemAPI.Query<RefRO<Agent>, RefRO<LocalTransform>>())
        {
            list.Add(new AgentState
            {
                id = agent.ValueRO.ID,
                x = transform.ValueRO.Position.x,
                y = transform.ValueRO.Position.y
            });
        }

        NetworkServer.SendToAll(new AgentStateMessage { states = list });
    }
}
