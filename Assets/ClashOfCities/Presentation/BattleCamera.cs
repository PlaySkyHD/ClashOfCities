using UnityEngine;
namespace ClashOfCities.Presentation
{
    public sealed class BattleCamera : MonoBehaviour
    {
        public Transform A,B;
        Vector3 velocity;
        Camera cameraComponent;
        void Awake(){cameraComponent=GetComponent<Camera>();}
        bool Fits(Vector3 point,Vector3 position,Quaternion rotation)
        {
            Vector3 local=Quaternion.Inverse(rotation)*(point-position);
            if(local.z<=0)return false;
            float halfHeight=local.z*Mathf.Tan(cameraComponent.fieldOfView*.5f*Mathf.Deg2Rad);
            float viewportY=.5f+local.y/(halfHeight*2);
            float viewportX=.5f+local.x/(halfHeight*2*cameraComponent.aspect);
            // Keep feet, faces and floating health bars inside the clear area between HUD panels.
            return viewportY>.285f&&viewportY<.735f&&viewportX>.08f&&viewportX<.92f;
        }
        void LateUpdate()
        {
            Vector3 center=A!=null&&B!=null?(A.position+B.position)*.5f:Vector3.zero;
            float distance=A!=null&&B!=null?Vector3.Distance(A.position,B.position):12;
            float framing=Mathf.Clamp(8.6f+distance*.63f,10.3f,27);
            Vector3 desired=center+new Vector3(0,framing*.68f,-framing);
            Quaternion rotation=Quaternion.LookRotation(center+Vector3.up*.95f-desired);
            if(cameraComponent!=null&&A!=null&&B!=null)
            {
                for(int i=0;i<24;i++)
                {
                    if(Fits(A.position-Vector3.up*.2f,desired,rotation)&&Fits(A.position+Vector3.up*2.9f,desired,rotation)&&Fits(B.position-Vector3.up*.2f,desired,rotation)&&Fits(B.position+Vector3.up*2.9f,desired,rotation))break;
                    framing*=1.055f;desired=center+new Vector3(0,framing*.68f,-framing);rotation=Quaternion.LookRotation(center+Vector3.up*.95f-desired);
                }
            }
            // Pull back faster than zooming in so a lateral dodge does not disappear under the HUD.
            float smoothing=(transform.position-center).sqrMagnitude<(desired-center).sqrMagnitude?.13f:.45f;
            transform.position=Vector3.SmoothDamp(transform.position,desired,ref velocity,smoothing);
            transform.rotation=Quaternion.Slerp(transform.rotation,Quaternion.LookRotation(center+Vector3.up*.95f-transform.position),Time.deltaTime*8);
        }
    }
}
