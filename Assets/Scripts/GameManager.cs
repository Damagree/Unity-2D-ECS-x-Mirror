using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Agents")]
    public int agentCount = 5000;
    public float minSpeed = 1f;
    public float maxSpeed = 3f;

    [Header("AI Wander")]
    public float wanderRadius = 3f;
    public float repathInterval = 1.2f;

    void Awake()
    {
        Instance = this;
    }
}
