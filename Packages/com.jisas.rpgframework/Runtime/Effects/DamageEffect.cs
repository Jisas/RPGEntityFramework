using UnityEngine;
using System;

namespace RPGFramework
{
    public class DamageEffect : IEffect<Enemy>
    {
        // Ejecución del efecto
        public void Apply(Enemy target)
        {
            if (target == null) return;

            // Lógica del efecto
        }

        public void Cancel() { /*noop*/ }
    }
}