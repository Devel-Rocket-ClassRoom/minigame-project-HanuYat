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

    [SerializeField]
    private float repathInterval = 0.2f;

    private bool armed;
    private bool chasing;
    private float repathTimer;
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
        repathTimer = 0f;
        if (agent.isOnNavMesh)
        {
            agent.isStopped = false;
            if (player != null)
                agent.SetDestination(player.position);
        }
    }

    private void Update()
    {
        if (!armed || !chasing || player == null)
            return;
        if (!agent.isOnNavMesh)
            return;

        repathTimer -= Time.deltaTime;
        if (repathTimer > 0f)
            return;
        repathTimer = repathInterval;
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
