using UnityEngine;
namespace ClashOfCities.Presentation
{
    // Runtime meshes, like runtime materials, must be released when a match closes.
    public sealed class ProceduralMeshOwner : MonoBehaviour
    {
        public Mesh Value;
        void OnDestroy(){if(Value!=null)Destroy(Value);}
    }
}
