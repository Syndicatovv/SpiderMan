using UnityEngine;
using UnityEngine.AI;

public class EnemyAnimation : MonoBehaviour
{
    private NavMeshAgent agent;
    private Animator animator;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
    }

    private void Update()
    {
        if (agent == null || animator == null)
            return;

        // Скорость движения врага
        float speed = agent.velocity.magnitude;

        // Передаём скорость в Animator Invector
        float inputMagnitude = Mathf.Clamp01(speed / agent.speed);

        animator.SetFloat("InputMagnitude", inputMagnitude, 0.1f, Time.deltaTime);
    }
}