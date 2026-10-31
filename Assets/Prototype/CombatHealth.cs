using System;
using UnityEngine;

namespace DemonSlayer.Prototype
{
    public sealed class CombatHealth : MonoBehaviour
    {
        [SerializeField] private int maximum = 100;
        public int Current { get; private set; }
        public int Maximum => maximum;
        public bool IsDead => Current <= 0;
        public float LastDamageTime { get; private set; } = float.NegativeInfinity;
        public float DeathTime { get; private set; } = float.NegativeInfinity;
        public event Action Died;

        private void Awake() => Restore();

        public void Damage(int amount)
        {
            if (IsDead || amount <= 0) return;
            LastDamageTime = Time.time;
            Current = Mathf.Max(0, Current - amount);
            if (IsDead)
            {
                DeathTime = Time.time;
                Died?.Invoke();
            }
        }

        public void Restore()
        {
            Current = maximum;
            LastDamageTime = DeathTime = float.NegativeInfinity;
        }
    }
}
