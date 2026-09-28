using UnityEngine;
namespace ClashOfCities.Presentation
{
    public sealed class CombatPulse : MonoBehaviour
    {
        float age,size,duration=.55f;
        public void Initialize(float targetSize,float seconds=.55f){size=targetSize;duration=seconds;transform.localScale=Vector3.one*.1f;}
        void Update()
        {
            age+=Time.deltaTime;float t=Mathf.Clamp01(age/duration);
            transform.localScale=new Vector3(1,.12f,1)*Mathf.Lerp(.1f,size,t);
            // Thin the ring near its end instead of covering either fighter with an opaque sphere.
            if(t>.7f)transform.localScale=new Vector3(transform.localScale.x,Mathf.Max(.001f,(1-t)*.4f),transform.localScale.z);
            if(age>=duration)Destroy(gameObject);
        }
    }
}
