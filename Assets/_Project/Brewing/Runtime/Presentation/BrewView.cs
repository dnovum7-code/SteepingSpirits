using UnityEngine;
using SteepingSpirits.Brewing.Core;
using SteepingSpirits.Brewing.Data;
using SteepingSpirits.Brewing.Flow;

namespace SteepingSpirits.Brewing.Presentation
{
    /// <summary>Base for brewing views: read-only access to the session and tuning.</summary>
    public abstract class BrewView : MonoBehaviour
    {
        [SerializeField] protected BrewSessionController controller;

        protected BrewSession Session => controller != null ? controller.Session : null;
        protected PresentationParams Look => controller != null && controller.Tuning != null ? controller.Tuning.presentation : null;
        protected BrewConfig Sim => controller != null && controller.Tuning != null ? controller.Tuning.simulation : null;

        public void Bind(BrewSessionController controller)
        {
            this.controller = controller;
        }

        protected static IBrewParticles AsParticles(MonoBehaviour behaviour)
        {
            return behaviour as IBrewParticles;
        }
    }
}
