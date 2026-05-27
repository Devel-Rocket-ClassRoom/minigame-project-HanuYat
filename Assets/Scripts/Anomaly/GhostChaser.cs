using System;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class GhostChaser : MonoBehaviour
{
    public static event Action OnPlayerCaught;

    [SerializeField]
    private NavMeshAgent agent;

    [SerializeField]
    private Transform player;

    [SerializeField]
    private float chaseSpeed = 1.0f;

    private bool armed;
    private bool chasing;
    private Vector3 spawnPosition;
    private Quaternion spawnRotation;

    private void Awake()
    {
        if (agent == null)
            agent = GetComponent<NavMeshAgent>();

        spawnPosition = transform.position;
        spawnRotation = transform.rotation;

        agent.speed = chaseSpeed;
        agent.isStopped = true;
    }

    public void SetArmed(bool on)
    {
        armed = on;
        chasing = false;

        if (on)
        {
            if (agent.isOnNavMesh)
                agent.Warp(spawnPosition);
            else
                transform.position = spawnPosition;
            transform.rotation = spawnRotation;
        }

        if (agent.isOnNavMesh)
            agent.isStopped = true;
    }

    public void StartChasing()
    {
        if (!armed)
            return;
        if (chasing)
            return;
        chasing = true;
        if (agent.isOnNavMesh)
            agent.isStopped = false;
    }

    private void Update()
    {
        if (!armed || !chasing || player == null)
            return;
        if (!agent.isOnNavMesh)
            return;
        agent.SetDestination(player.position);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!armed || !chasing)
            return;
        if (!other.CompareTag("Player"))
            return;

        chasing = false;
        if (agent.isOnNavMesh)
            agent.isStopped = true;

        OnPlayerCaught?.Invoke();
    }
}
