
using UnityEngine;
using UnityEngine.AI;

public class EnemyChase : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;

    [Header("Chase Settings")]
    [SerializeField] private float chaseDistance = 25f;
    [SerializeField] private float stopDistance = 2f;

    private NavMeshAgent agent;

    private bool isDead = false;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    private void Start()
    {
        if (target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");

            if (player != null)
                target = player.transform;
        }

        agent.stoppingDistance = stopDistance;
    }

    private void Update()
    {
        if (isDead)
            return;
        
        if (target == null || agent == null)
            return;

        if (!agent.isOnNavMesh)
            return;

        float distance = Vector3.Distance(
            transform.position,
            target.position
        );

        if (distance <= chaseDistance && distance > stopDistance)
        {
            agent.isStopped = false;
            agent.SetDestination(target.position);
        }
        else if (distance <= stopDistance)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }
        else
        {
            agent.isStopped = true;
            agent.ResetPath();
        }
    }
    public void StopChasing()
    {
        isDead = true;

        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.ResetPath();
            agent.velocity = Vector3.zero;
        }

        if (agent != null)
            agent.enabled = false;
    }
}