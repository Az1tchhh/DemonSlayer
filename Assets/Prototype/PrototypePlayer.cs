using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DemonSlayer.Prototype
{
    [RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D), typeof(CombatHealth))]
    public sealed class PrototypePlayer : MonoBehaviour
    {
        [SerializeField] private InputActionAsset inputTemplate;
        [SerializeField] private float speed = 6f;
        [SerializeField] private float jumpSpeed = 10f;
        [SerializeField] private float kataCooldown = 3f;
        [SerializeField, Min(.01f)] private float basicSlashDuration = .28f;
        [SerializeField, Min(.01f)] private float waterSlashDuration = .45f;
        [SerializeField, Min(.01f)] private float waterWheelDuration = .65f;
        [SerializeField, Min(.01f)] private float waterWheelCooldown = 5f;
        [SerializeField] private float waterWheelSpeed = 6f;
        [SerializeField] private float waterWheelHop = 6f;
        [SerializeField] private int waterWheelDamage = 45;
        [SerializeField, Min(.01f)] private float flowingDanceDuration = .8f;
        [SerializeField, Min(.01f)] private float flowingDanceCooldown = 6f;
        [SerializeField] private float flowingDanceSpeed = 8f;
        [SerializeField] private int flowingDanceDamage = 35;
        [SerializeField, Min(.01f)] private float hinokamiDanceDuration = .9f;
        [SerializeField, Min(.01f)] private float hinokamiDanceCooldown = 8f;
        [SerializeField] private int hinokamiDanceDamage = 65;
        [SerializeField, Min(.01f)] private float clearBlueSkyDuration = 1f;
        [SerializeField, Min(.01f)] private float clearBlueSkyCooldown = 10f;
        [SerializeField] private float clearBlueSkyHop = 7f;
        [SerializeField] private int clearBlueSkyDamage = 55;
        public const float SkyLaunch = .14f;
        public const float SkyContactStart = .28f;
        public const float SkyContactEnd = .62f;
        public const float SkySpinEnd = .70f;
        public const float SkyRadius = 2.2f;
        public const float HinokamiContact = .32f / .9f;
        public const float HinokamiReach = 2.6f;
        public const float HinokamiHeight = 2.6f;
        public const float HinokamiHeightOffset = .35f;
        public const float DanceAdvanceStart = .10f / .8f;
        public const float DanceContactStart = .18f / .8f;
        public const float DanceContactEnd = .58f / .8f;
        public const float DanceAdvanceEnd = .60f / .8f;
        public const float DanceWidth = 3f;
        public const float DanceHeight = 1.6f;
        public const float DanceOffset = .55f;
        public const float WheelLaunch = .08f / .65f;
        public const float WheelContactStart = .16f / .65f;
        public const float WheelContactEnd = .42f / .65f;
        public const float WheelSpinEnd = .50f / .65f;
        public const float WheelRadius = 1.55f;
        public const float BasicSlashContact = 2f / 7f;
        public const float WaterSlashContact = 1f / 3f;
        public const float WaterSlashReach = 3f;
        public const float WaterSlashHeight = 1.7f;
        public const float AttackStartOffset = .45f;
        [SerializeField] private SpriteRenderer attackIndicator;
        [SerializeField] private Transform sword;
        private InputActionAsset controls;
        private InputAction move, jump, attack, kata, wheel, dance, hinokami, sky;
        private Rigidbody2D body;
        private BoxCollider2D shape;
        private CombatHealth health;
        private readonly RaycastHit2D[] groundHits = new RaycastHit2D[4];
        private readonly Collider2D[] targets = new Collider2D[8];
        private readonly HashSet<PrototypeDemon> hitDemons = new HashSet<PrototypeDemon>();
        private float facing = 1, nextAttack, nextKata, nextWheel, nextDance, nextHinokami, nextSky, invulnerableUntil;
        private float attackStarted;
        private bool basicSlashActive, waterSlashActive, impactPending;
        private bool waterWheelActive, wheelLaunchPending;
        private bool flowingDanceActive;
        private bool hinokamiDanceActive;
        private bool clearBlueSkyActive, skyLaunchPending;
        private bool jumpQueued;
        public bool Grounded { get; private set; }
        public float KataRemaining => Mathf.Max(0, nextKata - Time.time);
        public float WheelRemaining => Mathf.Max(0, nextWheel - Time.time);
        public float DanceRemaining => Mathf.Max(0, nextDance - Time.time);
        public float HinokamiRemaining => Mathf.Max(0, nextHinokami - Time.time);
        public float SkyRemaining => Mathf.Max(0, nextSky - Time.time);
        public bool IsClearBlueSky => clearBlueSkyActive;
        public float ClearBlueSkyProgress => Mathf.Clamp01((Time.time - attackStarted) / clearBlueSkyDuration);
        public CombatHealth Health => health;
        public float Facing => facing;
        public bool IsBasicSlashing => basicSlashActive;
        public bool IsWaterSlashing => waterSlashActive;
        public bool IsWaterWheeling => waterWheelActive;
        public bool IsFlowingDancing => flowingDanceActive;
        public bool IsHinokamiDancing => hinokamiDanceActive;
        public bool IsAttacking => basicSlashActive || waterSlashActive || waterWheelActive || flowingDanceActive || hinokamiDanceActive || clearBlueSkyActive;
        public float HinokamiDanceProgress => Mathf.Clamp01((Time.time - attackStarted) / hinokamiDanceDuration);
        public float FlowingDanceProgress => Mathf.Clamp01((Time.time - attackStarted) / flowingDanceDuration);
        public float WaterWheelProgress => Mathf.Clamp01((Time.time - attackStarted) / waterWheelDuration);
        public float BasicSlashProgress => Mathf.Clamp01((Time.time - attackStarted) / basicSlashDuration);
        public float WaterSlashProgress => Mathf.Clamp01((Time.time - attackStarted) / waterSlashDuration);
        public event Action ResetPerformed;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            shape = GetComponent<BoxCollider2D>();
            health = GetComponent<CombatHealth>();
            attackIndicator.enabled = false;
            controls = Instantiate(inputTemplate);
            var map = controls.FindActionMap("Player", true);
            move = map.FindAction("Move", true);
            jump = map.FindAction("Jump", true);
            attack = map.FindAction("Attack", true);
            attack.AddBinding("<Keyboard>/j");
            kata = map.AddAction("PrototypeWaterSlash", InputActionType.Button, "<Keyboard>/q");
            wheel = map.AddAction("PrototypeWaterWheel", InputActionType.Button, "<Keyboard>/e");
            dance = map.AddAction("PrototypeFlowingDance", InputActionType.Button, "<Keyboard>/f");
            sky = map.AddAction("PrototypeClearBlueSky", InputActionType.Button, "<Keyboard>/h");
            hinokami = map.AddAction("PrototypeHinokamiDance", InputActionType.Button, "<Keyboard>/g");
        }
        private void OnEnable()
        {
            if (controls != null) controls.FindActionMap("Player").Enable();
            if (health != null) health.Died += CancelAttack;
        }
        private void OnDisable()
        {
            if (controls != null) controls.Disable();
            if (health != null) health.Died -= CancelAttack;
            CancelAttack();
            jumpQueued = false;
        }
        private void OnDestroy() { if (controls != null) Destroy(controls); }

        private void Update()
        {
            if (health.IsDead) { attackIndicator.enabled = false; return; }
            // One clock drives both the contact hit and the sampled animation pose.
            float impactDelay = hinokamiDanceActive ? hinokamiDanceDuration * HinokamiContact : waterSlashActive ? waterSlashDuration * WaterSlashContact : basicSlashDuration * BasicSlashContact;
            if (impactPending && Time.time >= attackStarted + impactDelay)
            {
                impactPending = false;
                if (hinokamiDanceActive) ResolveHinokamiHit();
                else ResolveHit(waterSlashActive);
            }
            if (waterWheelActive && WaterWheelProgress >= WheelContactStart && WaterWheelProgress <= WheelContactEnd)
                ResolveWheelHits();
            if (clearBlueSkyActive && ClearBlueSkyProgress >= SkyContactStart && ClearBlueSkyProgress <= SkyContactEnd)
                ResolveSkyHits();
            if (flowingDanceActive && FlowingDanceProgress >= DanceContactStart && FlowingDanceProgress <= DanceContactEnd)
                ResolveDanceHits();
            if (IsAttacking && Time.time >= nextAttack)
                basicSlashActive = waterSlashActive = waterWheelActive = wheelLaunchPending = flowingDanceActive = hinokamiDanceActive = clearBlueSkyActive = skyLaunchPending = false;
            float x = move.ReadValue<Vector2>().x;
            if (!IsAttacking && Mathf.Abs(x) > .1f) facing = Mathf.Sign(x);
            sword.localPosition = new Vector3(facing * .65f, .1f, 0);
            if (jump.WasPressedThisFrame() && !waterWheelActive && !flowingDanceActive && !hinokamiDanceActive && !clearBlueSkyActive) jumpQueued = true;
            if (sky.WasPressedThisFrame()) TryClearBlueSky();
            else if (hinokami.WasPressedThisFrame()) TryHinokamiDance();
            else if (dance.WasPressedThisFrame()) TryFlowingDance();
            else if (wheel.WasPressedThisFrame()) TryWaterWheel();
            else if (kata.WasPressedThisFrame()) TryAttack(true);
            else if (attack.WasPressedThisFrame()) TryAttack(false);
        }

        private void FixedUpdate()
        {
            Grounded = false;
            var filter = new ContactFilter2D { useLayerMask = true, layerMask = 1, useTriggers = false };
            int count = shape.Cast(Vector2.down, filter, groundHits, .08f);
            for (int i = 0; i < count; i++) if (groundHits[i].normal.y > .6f) Grounded = true;
            if (health.IsDead)
            {
                body.linearVelocity = new Vector2(0, body.linearVelocity.y);
                jumpQueued = false;
                return;
            }
            var velocity = body.linearVelocity;
            if (clearBlueSkyActive)
            {
                velocity.x = 0;
                float progress = ClearBlueSkyProgress;
                if (skyLaunchPending && progress >= SkyLaunch && progress < SkySpinEnd)
                {
                    velocity.y = Mathf.Max(velocity.y, clearBlueSkyHop);
                    skyLaunchPending = false;
                }
            }
            else if (hinokamiDanceActive)
            {
                // The heavy strike commits to a planted stance while gravity remains active.
                velocity.x = 0;
            }
            else if (flowingDanceActive)
            {
                float progress = FlowingDanceProgress;
                velocity.x = progress >= DanceAdvanceStart && progress < DanceAdvanceEnd ? facing * flowingDanceSpeed : 0;
            }
            else if (waterWheelActive)
            {
                float progress = WaterWheelProgress;
                bool spinning = progress >= WheelLaunch && progress < WheelSpinEnd;
                velocity.x = spinning ? facing * waterWheelSpeed : 0;
                if (spinning && wheelLaunchPending)
                {
                    velocity.y = Mathf.Max(velocity.y, waterWheelHop);
                    wheelLaunchPending = false;
                }
            }
            else
            {
                velocity.x = move.ReadValue<Vector2>().x * speed;
                if (jumpQueued && Grounded) velocity.y = jumpSpeed;
            }
            jumpQueued = false;
            body.linearVelocity = velocity;
            if (body.position.y < -6) health.Damage(health.Maximum);
        }

        public bool TryAttack(bool water)
        {
            if (!isActiveAndEnabled || health.IsDead || Time.time < nextAttack || (water && Time.time < nextKata)) return false;
            nextAttack = Time.time + (water ? waterSlashDuration : basicSlashDuration);
            if (water) nextKata = Time.time + kataCooldown;
            attackStarted = Time.time;
            waterWheelActive = wheelLaunchPending = flowingDanceActive = hinokamiDanceActive = clearBlueSkyActive = skyLaunchPending = false;
            waterSlashActive = water;
            basicSlashActive = !water;
            impactPending = true;
            return true;
        }

        public bool TryWaterWheel()
        {
            if (!isActiveAndEnabled || health.IsDead || Time.time < nextAttack || Time.time < nextWheel) return false;
            attackStarted = Time.time;
            nextAttack = Time.time + waterWheelDuration;
            nextWheel = Time.time + waterWheelCooldown;
            basicSlashActive = waterSlashActive = impactPending = flowingDanceActive = hinokamiDanceActive = clearBlueSkyActive = skyLaunchPending = false;
            waterWheelActive = wheelLaunchPending = true;
            jumpQueued = false;
            hitDemons.Clear();
            return true;
        }

        public bool TryFlowingDance()
        {
            if (!isActiveAndEnabled || health.IsDead || Time.time < nextAttack || Time.time < nextDance ||
                !Grounded || Mathf.Abs(body.linearVelocity.y) > .5f) return false;
            attackStarted = Time.time;
            nextAttack = Time.time + flowingDanceDuration;
            nextDance = Time.time + flowingDanceCooldown;
            basicSlashActive = waterSlashActive = waterWheelActive = wheelLaunchPending = impactPending = hinokamiDanceActive = clearBlueSkyActive = skyLaunchPending = false;
            flowingDanceActive = true;
            jumpQueued = false;
            hitDemons.Clear();
            return true;
        }

        private void ResolveDanceHits()
        {
            Vector2 center = body.position + Vector2.right * facing * DanceOffset;
            var filter = new ContactFilter2D { useLayerMask = true, layerMask = 1 << 2, useTriggers = false };
            int count = Physics2D.OverlapBox(center, new Vector2(DanceWidth, DanceHeight), 0, filter, targets);
            for (int i = 0; i < count; i++)
            {
                var demon = targets[i].GetComponentInParent<PrototypeDemon>();
                if (demon != null && hitDemons.Add(demon)) demon.Health.Damage(flowingDanceDamage);
            }
        }

        public bool TryHinokamiDance()
        {
            if (!isActiveAndEnabled || health.IsDead || Time.time < nextAttack || Time.time < nextHinokami ||
                !Grounded || Mathf.Abs(body.linearVelocity.y) > .5f) return false;
            attackStarted = Time.time;
            nextAttack = Time.time + hinokamiDanceDuration;
            nextHinokami = Time.time + hinokamiDanceCooldown;
            basicSlashActive = waterSlashActive = waterWheelActive = wheelLaunchPending = flowingDanceActive = false;
            clearBlueSkyActive = skyLaunchPending = false;
            hinokamiDanceActive = impactPending = true;
            jumpQueued = false;
            return true;
        }

        private void ResolveHinokamiHit()
        {
            Vector2 center = body.position + new Vector2(facing * (AttackStartOffset + HinokamiReach / 2), HinokamiHeightOffset);
            var filter = new ContactFilter2D { useLayerMask = true, layerMask = 1 << 2, useTriggers = false };
            int count = Physics2D.OverlapBox(center, new Vector2(HinokamiReach, HinokamiHeight), 0, filter, targets);
            hitDemons.Clear();
            for (int i = 0; i < count; i++)
            {
                var demon = targets[i].GetComponentInParent<PrototypeDemon>();
                if (demon != null && hitDemons.Add(demon)) demon.Health.Damage(hinokamiDanceDamage);
            }
        }

        public bool TryClearBlueSky()
        {
            if (!isActiveAndEnabled || health.IsDead || Time.time < nextAttack || Time.time < nextSky) return false;
            attackStarted = Time.time;
            nextAttack = Time.time + clearBlueSkyDuration;
            nextSky = Time.time + clearBlueSkyCooldown;
            basicSlashActive = waterSlashActive = waterWheelActive = wheelLaunchPending = flowingDanceActive = hinokamiDanceActive = impactPending = false;
            clearBlueSkyActive = skyLaunchPending = true;
            jumpQueued = false;
            hitDemons.Clear();
            return true;
        }

        private void ResolveSkyHits()
        {
            var filter = new ContactFilter2D { useLayerMask = true, layerMask = 1 << 2, useTriggers = false };
            int count = Physics2D.OverlapCircle(body.position, SkyRadius, filter, targets);
            for (int i = 0; i < count; i++)
            {
                var demon = targets[i].GetComponentInParent<PrototypeDemon>();
                if (demon != null && hitDemons.Add(demon)) demon.Health.Damage(clearBlueSkyDamage);
            }
        }

        private void ResolveWheelHits()
        {
            var filter = new ContactFilter2D { useLayerMask = true, layerMask = 1 << 2, useTriggers = false };
            int count = Physics2D.OverlapCircle(body.position, WheelRadius, filter, targets);
            for (int i = 0; i < count; i++)
            {
                var demon = targets[i].GetComponentInParent<PrototypeDemon>();
                if (demon != null && hitDemons.Add(demon)) demon.Health.Damage(waterWheelDamage);
            }
        }

        private void ResolveHit(bool water)
        {
            float reach = water ? WaterSlashReach : 1.25f;
            Vector2 center = (Vector2)transform.position + Vector2.right * facing * (AttackStartOffset + reach / 2);
            Vector2 size = new Vector2(reach, water ? WaterSlashHeight : 1.1f);
            var filter = new ContactFilter2D { useLayerMask = true, layerMask = 1 << 2, useTriggers = false };
            int count = Physics2D.OverlapBox(center, size, 0, filter, targets);
            hitDemons.Clear();
            for (int i = 0; i < count; i++)
            {
                var demon = targets[i].GetComponentInParent<PrototypeDemon>();
                if (demon != null && hitDemons.Add(demon)) demon.Health.Damage(water ? 40 : 20);
            }
        }

        private void CancelAttack()
        {
            if ((waterWheelActive || flowingDanceActive || hinokamiDanceActive || clearBlueSkyActive) && body != null) body.linearVelocity = new Vector2(0, body.linearVelocity.y);
            basicSlashActive = waterSlashActive = impactPending = false;
            waterWheelActive = wheelLaunchPending = flowingDanceActive = hinokamiDanceActive = clearBlueSkyActive = skyLaunchPending = false;
            hitDemons.Clear();
            if (attackIndicator != null) attackIndicator.enabled = false;
        }

        public void ReceiveHit(int damage)
        {
            if (Time.time < invulnerableUntil || health.IsDead) return;
            health.Damage(damage);
            invulnerableUntil = Time.time + .6f;
        }

        public void ResetAt(Vector2 position)
        {
            body.position = position;
            body.linearVelocity = Vector2.zero;
            health.Restore();
            CancelAttack();
            nextAttack = nextKata = nextWheel = nextDance = nextHinokami = nextSky = 0;
            invulnerableUntil = Time.time + .6f;
            jumpQueued = false;
            facing = 1;
            attackIndicator.enabled = false;
            ResetPerformed?.Invoke();
        }
    }
}
