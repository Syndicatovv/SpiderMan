using UnityEngine;
using System.Collections;

public class EnemyHealth : MonoBehaviour
{
    private Animator anim;

    [Header("Health")]
    [SerializeField] private float maxHealth = 100f;

    private float currentHealth;
    private bool isDead;
    public bool IsDead => isDead;
    void Start()
    {
        anim = GetComponentInChildren<Animator>();
        currentHealth = maxHealth;
    }
    void Update()
    {
        
    }

    public void TakeDamage(float damage)
    {
        if (isDead) return;

        currentHealth -= damage;

        Debug.Log($"{gameObject.name} received {damage} damage. Health: {currentHealth}");

        PlayReaction("Hit");

        if (currentHealth <= 0) DieWithKnockout();
    }

    public void TakeHeavyDamage(float damage)
    {
        if (isDead) return;

        currentHealth -= damage;

        Debug.Log("HeavyHit reaction triggered");

        PlayReaction("HeavyHit");

        Debug.Log($"{gameObject.name} received heavy damage. Health: {currentHealth}");

        if (currentHealth < 0) DieWithKnockout();
    }

    public void TakeSuperDamage()
    {
        if (isDead) return;

        isDead = true;
        StopChasing();

        PlayReaction("Knockback");

        Debug.Log($"{gameObject.name} was defeated by a super attack!");
    }

    private void DieWithKnockout()
    {
        if (isDead)
            return;
        
        isDead = true;
        StopChasing();

        PlayReaction("Knockout");

        Debug.Log($"{gameObject.name} was knocked out!");
    }

    private void StopChasing()
    {
        EnemyChase enemyChase = GetComponent<EnemyChase>();

        if (enemyChase != null)
            enemyChase.StopChasing();
    }

    private void PlayReaction(string triggerName)
    {
        if (anim != null) anim.SetTrigger(triggerName);
    }
}
