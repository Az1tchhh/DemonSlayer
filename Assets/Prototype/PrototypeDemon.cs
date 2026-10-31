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
        private Rigidbody2D body;
        private float nextAttack, attackStarted;
        private bool attackActive, impactPending;
        public CombatHealth Health { get; private set; }
        public float Facing { get; private set; } = -1;
        public bool IsAttacking => attackActive;
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
            if (Health != null) Health.Died += CancelAttack;
        }
        private void OnDisable()
        {
            if (Health != null) Health.Died -= CancelAttack;
            CancelAttack();
        }
        private void FixedUpdate()
        {
            if (Health.IsDead || player.Health.IsDead)
            {
                CancelAttack();
                body.linearVelocity = new Vector2(0, body.linearVelocity.y);
                return;
            }
            Vector2 delta = player.transform.position - transform.position;
            if (attackActive)
            {
                body.linearVelocity = new Vector2(0, body.linearVelocity.y);
                if (impactPending && Time.time >= attackStarted + attackDuration * AttackContact)
                {
                    impactPending = false;
                    // Recheck range and side at contact, so leaving the swipe makes it miss.
                    if (InMeleeRange(delta) && delta.x * Facing >= 0) player.ReceiveHit(damage);
                }
                if (Time.time >= attackStarted + attackDuration) attackActive = false;
                return;
            }
            if (Mathf.Abs(delta.x) > .1f) Facing = Mathf.Sign(delta.x);
            float horizontal = 0;
            if (delta.magnitude < 6 && Mathf.Abs(delta.x) > 1.1f)
                horizontal = Mathf.Sign(delta.x) * speed;
            if ((body.position.x < 2 && horizontal < 0) || (body.position.x > 11 && horizontal > 0)) horizontal = 0;
            body.linearVelocity = new Vector2(horizontal, body.linearVelocity.y);
            if (InMeleeRange(delta) && Time.time >= nextAttack)
            {
                attackStarted = Time.time;
                attackActive = impactPending = true;
                body.linearVelocity = new Vector2(0, body.linearVelocity.y);
                nextAttack = Time.time + 1.2f;
            }
        }
        private static bool InMeleeRange(Vector2 delta) => Mathf.Abs(delta.x) < 1.25f && Mathf.Abs(delta.y) < 1.4f;
        private void CancelAttack() => attackActive = impactPending = false;
        public void ResetAt(Vector2 position)
        {
            body.position = position;
            body.linearVelocity = Vector2.zero;
            Health.Restore();
            CancelAttack();
            Facing = -1;
            nextAttack = Time.time + 1f;
            ResetPerformed?.Invoke();
        }
    }
}
