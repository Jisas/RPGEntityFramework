using UnityEngine;
using System;

namespace RPGFramework
{
    public class DamageEffect : IEffect<IDamagable>
    {
        // Ejecución del efecto
        public void Apply(IDamagable target)
        {
            if (target == null) return;

            // Lógica del efecto
        }

        public void Cancel() { /*noop*/ }
    }
}