using UnityEngine;

namespace ClashOfCities.Presentation
{
    public sealed class OwnedMaterial : MonoBehaviour
    {
        public Material Value;
        void OnDestroy() { if (Value != null) Destroy(Value); }
    }
}
