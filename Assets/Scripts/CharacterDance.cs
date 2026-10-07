using UnityEngine;
using Invector.vCharacterController;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using TMPro;
using UnityEngine.UI;
public class CharacterDance : MonoBehaviour
{
    private Animator anim;

    private vThirdPersonController cc;
    private int jumpCount = 0;

    private int combatLayerIndex;
    private bool isPunching;

    [Header("UI")]
    [SerializeField] private TMP_Text superIndicator;
    [SerializeField] private Slider superTimerBar;
    [SerializeField] private GameObject superBackground;

    [Header("Combo")]
    [SerializeField] private float comboResetTime = 1.2f;
    [SerializeField] private float maxAttackDuration = 2f;
    private float attackStartTime;
    private int comboStep = 0;
    private float lastAttackTime;
    private string currentAttackState;
    private bool superReady = false;

    [SerializeField] private float superDuration = 3f;
    private float superReadyTime;

    [Header("Super Jump")]
    [SerializeField] private Rigidbody playerRb;
    [SerializeField] private float superJumpForce = 6f;

    [Header("Punch Hitbox")]
    [SerializeField] private Transform punchPoint;
    [SerializeField] private float punchRadius = 0.35f;

    [Header("Kick Hitbox")]
    [SerializeField] private Transform kickPoint;
    [SerializeField] private float kickRadius = 0.6f;

    [SerializeField] private LayerMask enemyLayers;

    private readonly HashSet<EnemyHealth> hitTargets = new();

    [Header("Punch Damage")]
    [SerializeField] private float punchDamage = 25f;
    [SerializeField] private float heavyPunchDamage = 40f;

    private bool attackHitConfirmed = false;

    [SerializeField] private float superGravityDelay = 1.2f;

    void Start()
    {
        anim = GetComponent<Animator>();
        cc = GetComponent<vThirdPersonController>();

        combatLayerIndex = anim.GetLayerIndex("CombatLayer");
        anim.SetLayerWeight(combatLayerIndex, 1f);

    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0)) TryPunch();
        if (Input.GetKeyDown(KeyCode.E)) TrySuperAttack();

        UpdatePunchState();
        UpdateComboTimeout();
        UpdateSuperIndicator();

        if (cc.isGrounded && jumpCount == 2)
        {
            jumpCount = 3;

            cc.lockMovement = true;

            anim.CrossFadeInFixedTime("BraceDropState", 0.2f);
        }

        if (jumpCount == 3)
        {
            if (anim.GetCurrentAnimatorStateInfo(0).IsName("Free Locomotion"))
            {
                jumpCount = 0;

                cc.lockMovement = false;
            }
        }


        if (Input.GetKeyDown(KeyCode.Space))
        {
            if(!cc.isGrounded && jumpCount == 1)
            {
                jumpCount = 2;

                cc._rigidbody.AddForce(Vector3.up * 13f, ForceMode.VelocityChange);

                anim.CrossFadeInFixedTime("DoubleJumpState", 0.3f);
            }
        }

        if (cc.isGrounded && jumpCount != 3)
            jumpCount = 1;

        if (Input.GetKeyDown(KeyCode.G))
        {
            anim.Play("hip_hop");
        }

        if (Input.GetKeyDown(KeyCode.M))
        {
            anim.Play("moonwalk");
        }

        if (cc._rigidbody.linearVelocity.y < -2f)
            anim.SetBool("isFalling", true);
        else
            anim.SetBool("isFalling", false);
    }

    void TryPunch()
    {
        if (!cc.isGrounded || isPunching) return;

        if (Time.time - lastAttackTime > comboResetTime) comboStep = 0;

        if (comboStep >= 2) return;

        lastAttackTime = Time.time;
        attackHitConfirmed = false;

        hitTargets.Clear();

        isPunching = true;
        attackStartTime = Time.time;
        cc.lockMovement = true;

        switch (comboStep)
        {
            case 0:
                currentAttackState = "Punch";

                anim.ResetTrigger("HeavyKick");
                anim.ResetTrigger("FlyingPunch");
                anim.SetTrigger("Punch");
                break;
            case 1:
                currentAttackState = "HeavyKick";

                anim.ResetTrigger("Punch");
                anim.ResetTrigger("FlyingPunch");
                anim.SetTrigger("HeavyKick");

                break;
        }
        
    }

    void TrySuperAttack()
    {
        if (!cc.isGrounded || isPunching || !superReady) return;

        hitTargets.Clear();

        isPunching = true;
        attackStartTime = Time.time;
        cc.lockMovement = true;

        currentAttackState = "FlyingPunch";

        anim.ResetTrigger("Punch");
        anim.ResetTrigger("HeavyKick");

        cc._rigidbody.useGravity = false;

        anim.SetTrigger(currentAttackState);

        ApplySuperJump();

        CancelInvoke(nameof(EnableSuperGravity));
        Invoke(nameof(EnableSuperGravity), superGravityDelay);

        superReady = false;
        comboStep = 0;
    }

    void EnableSuperGravity()
    {
        if (cc != null && cc._rigidbody != null)
        {
            cc._rigidbody.useGravity = true;
        }
    }

    void UpdatePunchState()
    {
        if (!isPunching)
            return;

        AnimatorStateInfo stateInfo =
            anim.GetCurrentAnimatorStateInfo(combatLayerIndex);

        bool animationFinished = stateInfo.normalizedTime >= 0.9f;
        bool timeoutReached = Time.time - attackStartTime >= maxAttackDuration;

        if (animationFinished || timeoutReached)
        {
            isPunching = false;
            cc.lockMovement = false;

            CancelInvoke(nameof(EnableSuperGravity));
            EnableSuperGravity();
        }
    }

    void UpdateComboTimeout()
    {
        if (superReady && Time.time - superReadyTime > superDuration) {
            comboStep = 0;
            superReady = false;
        }

        if (comboStep > 0 && !isPunching && Time.time - lastAttackTime > comboResetTime && !superReady) comboStep = 0;
    }

    public void PerformPunchHit()
    {
        Transform currentPoint;
        float currentRadius;

        if (currentAttackState == "HeavyKick" ||
            currentAttackState == "FlyingPunch")
        {
            currentPoint = kickPoint;
            currentRadius = kickRadius;
        }
        else
        {
            currentPoint = punchPoint;
            currentRadius = punchRadius;
        }

        if (currentPoint == null)
        {
            Debug.LogWarning("Current hit point is not assigned!");
            return;
        }

        Collider[] hits = Physics.OverlapSphere(currentPoint.position, currentRadius);

        foreach (Collider hit in hits)
        {
            EnemyHealth enemy = hit.GetComponentInParent<EnemyHealth>();

            if (enemy == null) continue;
            if (enemy.IsDead) continue;

            if (hitTargets.Contains(enemy)) continue;

            hitTargets.Add(enemy);

            if (!attackHitConfirmed)
            {
                attackHitConfirmed = true;

                if (currentAttackState == "Punch")
                {
                    comboStep = 1;
                    Debug.Log("FIRST HIT CONFIRMED");
                }
                else if (currentAttackState == "HeavyKick")
                {
                    comboStep = 2;

                    superReady = true;
                    superReadyTime = Time.time;

                    Debug.Log("HEAVY HIT CONFIRMED — SUPER IS READY");
                }
            }

            switch (currentAttackState)
            {
                case "Punch":
                    enemy.TakeDamage(punchDamage);
                    break;

                case "HeavyKick":
                    enemy.TakeHeavyDamage(heavyPunchDamage);
                    break;

                case "FlyingPunch":
                    enemy.TakeSuperDamage();
                    break;
            }
        }
    }

    void UpdateSuperIndicator()
    {
        if (superIndicator == null || superTimerBar == null || superBackground == null) return;

        if (superReady)
        {
            float remainingTime = superDuration - (Time.time - superReadyTime);
            remainingTime = Mathf.Max(0f, remainingTime);

            float progress = remainingTime / superDuration;

            superIndicator.text = $"SUPER READY";

            superTimerBar.value = progress;

            superIndicator.gameObject.SetActive(true);
            superTimerBar.gameObject.SetActive(true);
            superBackground.SetActive(true);
        }
        else {
            superIndicator.gameObject.SetActive(false);
            superTimerBar.gameObject.SetActive(false);
            superBackground.SetActive(false);
        }
    }

    void ApplySuperJump()
    {
        if (cc == null || cc._rigidbody == null)
            return;

        Rigidbody rb = cc._rigidbody;

        Vector3 velocity = rb.linearVelocity;
        velocity.y = 0f;
        rb.linearVelocity = velocity;

        rb.AddForce(
            Vector3.up * superJumpForce,
            ForceMode.VelocityChange
        );
    }
}
