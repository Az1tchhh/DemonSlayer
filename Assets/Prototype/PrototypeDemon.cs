using System;
using UnityEngine;

namespace DemonSlayer.Prototype
{
    [RequireComponent(typeof(Rigidbody2D), typeof(CombatHealth))]
    public sealed class PrototypeDemon : MonoBehaviour
    {
        [SerializeField] private PrototypePlayer player;
        [SerializeField] private float speed = 2f;
        [SerializeField] private int damage = 25;
        [SerializeField, Min(.01f)] private float attackDuration = .6f;
        public const float AttackContact = 5f / 12f;
        [SerializeField, Range(0, 1)] private float attackContact = AttackContact;
        [SerializeField, Min(.01f)] private float attackInterval = 1.2f;
        [SerializeField, Min(.01f)] private float detectionRange = 6f;
        [SerializeField, Min(0)] private float stoppingDistance = 1.1f;
        [SerializeField, Min(.01f)] private float meleeRange = 1.25f;
        [SerializeField, Min(.01f)] private float meleeHeight = 1.4f;
        [SerializeField] private Vector2 movementBounds = new Vector2(2, 11);
        private Rigidbody2D body;
        private float nextAttack, attackStarted;
        private bool attackActive, impactPending;
        private bool knockbackActive, knockbackLaunchPending;
        private float knockbackLift;
        private float knockbackEnds, knockbackDirection, knockbackSpeed;
        private readonly RaycastHit2D[] knockbackHits = new RaycastHit2D[16];
        public CombatHealth Health { get; private set; }
        public float Facing { get; private set; } = -1;
        public bool IsAttacking => attackActive;
        public bool IsKnockedBack => knockbackActive && Time.time < knockbackEnds && !Health.IsDead && isActiveAndEnabled;
        public float AttackProgress => Mathf.Clamp01((Time.time - attackStarted) / attackDuration);
        public event Action ResetPerformed;
        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            Health = GetComponent<CombatHealth>();
            Physics2D.IgnoreCollision(GetComponent<Collider2D>(), player.GetComponent<Collider2D>());
        }
        private void OnEnable()
        {
            if (Health != null) Health.Died += CancelCombat;
        }
        private void OnDisable()
        {
            if (Health != null) Health.Died -= CancelCombat;
            CancelCombat();
        }
        private void FixedUpdate()
        {
            if (Health.IsDead || player.Health.IsDead)
            {
                CancelCombat();
                body.linearVelocity = new Vector2(0, body.linearVelocity.y);
                return;
            }
            if (knockbackActive)
            {
                if (Time.time < knockbackEnds)
                {
                    if (knockbackLaunchPending)
                    {
                        body.linearVelocity = new Vector2(body.linearVelocity.x, knockbackLift);
                        knockbackLaunchPending = false;
                    }
                    Vector2 direction = new Vector2(knockbackDirection, 0);
                    float step = knockbackSpeed * Mathf.Min(Time.fixedDeltaTime, knockbackEnds - Time.time);
                    int count = body.Cast(direction, new ContactFilter2D { useTriggers = false }, knockbackHits, step + .02f);
                    for (int i = 0; i < count; i++)
                    {
                        var hit = knockbackHits[i];
                        if (hit.collider.GetComponentInParent<PrototypePlayer>() || Vector2.Dot(hit.normal, direction) >= -.1f) continue;
                        step = Mathf.Min(step, Mathf.Max(0, hit.distance - .01f));
                    }
                    body.linearVelocity = new Vector2(knockbackDirection * step / Time.fixedDeltaTime, body.linearVelocity.y);
                    return;
                }
                ClearKnockback();
            }
            Vector2 delta = player.transform.position - transform.position;
            if (attackActive)
            {
                body.linearVelocity = new Vector2(0, body.linearVelocity.y);
                if (impactPending && Time.time >= attackStarted + attackDuration * attackContact)
                {
                    impactPending = false;
                    // Recheck range and side at contact, so leaving the swipe makes it miss.
                    if (InMeleeRange(delta) && delta.x * Facing >= 0) player.ReceiveMeleeHit(damage, body.position);
                }
                if (Time.time >= attackStarted + attackDuration) attackActive = false;
                return;
            }
            if (Mathf.Abs(delta.x) > .1f) Facing = Mathf.Sign(delta.x);
            float horizontal = 0;
            if (delta.magnitude < detectionRange && Mathf.Abs(delta.x) > stoppingDistance)
                horizontal = Mathf.Sign(delta.x) * speed;
            if ((body.position.x < movementBounds.x && horizontal < 0) || (body.position.x > movementBounds.y && horizontal > 0)) horizontal = 0;
            body.linearVelocity = new Vector2(horizontal, body.linearVelocity.y);
            if (InMeleeRange(delta) && Time.time >= nextAttack)
            {
                attackStarted = Time.time;
                attackActive = impactPending = true;
                body.linearVelocity = new Vector2(0, body.linearVelocity.y);
                nextAttack = Time.time + Mathf.Max(attackDuration, attackInterval);
            }
        }
        private bool InMeleeRange(Vector2 delta) => Mathf.Abs(delta.x) < meleeRange && Mathf.Abs(delta.y) < meleeHeight;
        private void CancelAttack() => attackActive = impactPending = false;
        public void ApplyKnockback(float direction, float pushSpeed, float duration, float launchAngle = 0)
        {
            if (!isActiveAndEnabled || Health.IsDead || Mathf.Abs(direction) < .01f || pushSpeed <= 0 || duration <= 0) return;
            CancelAttack();
            knockbackDirection = Mathf.Sign(direction);
            knockbackSpeed = pushSpeed;
            // One upward launch; gravity bends the path afterwards. Zero preserves Nezuko's horizontal push.
            knockbackLift = pushSpeed * Mathf.Tan(Mathf.Clamp(launchAngle, 0, 75) * Mathf.Deg2Rad);
            knockbackLaunchPending = knockbackLift > 0;
            knockbackEnds = Time.time + duration;
            knockbackActive = true;
            nextAttack = Mathf.Max(nextAttack, knockbackEnds + .15f);
        }
        private void ClearKnockback()
        {
            if (knockbackActive && body) body.linearVelocity = new Vector2(0, body.linearVelocity.y);
            knockbackActive = knockbackLaunchPending = false;
        }
        private void CancelCombat() { CancelAttack(); ClearKnockback(); }
        public void ResetAt(Vector2 position)
        {
            body.position = position;
            body.linearVelocity = Vector2.zero;
            Health.Restore();
            CancelCombat();
            Facing = -1;
            nextAttack = Time.time + 1f;
            ResetPerformed?.Invoke();
        }
    }
}
