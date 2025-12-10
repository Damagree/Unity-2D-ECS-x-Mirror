using Mirror;
using System.Collections.Generic;

public struct AgentStateMessage : NetworkMessage
{
    public List<AgentState> states;
}

public struct AgentState
{
    public int id;
    public float x;
    public float y;
}
