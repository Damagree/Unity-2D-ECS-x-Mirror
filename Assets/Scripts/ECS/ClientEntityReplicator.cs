using UnityEngine;
using Mirror;
using System.Collections.Generic;

public class ClientEntityReplicator : MonoBehaviour
{
    public GameObject agentPrefab;

    Dictionary<int, Transform> visuals = new();

    void Awake()
    {
        NetworkClient.RegisterHandler<AgentStateMessage>(OnState);
    }

    void OnState(AgentStateMessage msg)
    {
        foreach (var s in msg.states)
        {
            if (!visuals.TryGetValue(s.id, out var t))
            {
                t = Instantiate(agentPrefab).transform;
                t.name = "Agent_" + s.id;
                visuals[s.id] = t;
            }

            t.position = new Vector2(s.x, s.y);
        }
    }
}
