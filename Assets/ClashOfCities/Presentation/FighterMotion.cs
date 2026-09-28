using System.Collections.Generic;
using UnityEngine;
using ClashOfCities.Core;

namespace ClashOfCities.Presentation
{
    public sealed partial class FighterView
    {
        Transform spine;
        readonly List<Transform> softJoints=new List<Transform>();
        readonly List<Quaternion> softRotations=new List<Quaternion>();
        readonly List<Rigidbody> bones=new List<Rigidbody>();
        readonly List<Collider> boneColliders=new List<Collider>();
        Vector3 torsoSpring,torsoVelocity,previousVelocity;
        float gait,locomotion;
        bool ragdollActive;
        readonly Vector3[] plantedFeet=new Vector3[2];
        readonly Vector3[] swingStart=new Vector3[2];
        readonly Vector3[] swingEnd=new Vector3[2];
        readonly float[] swingStartPhase=new float[2];
        readonly bool[] reachStep=new bool[2];
        readonly float[] reachStepTime=new float[2];
        readonly float[] settleElapsed=new float[2];
        readonly bool[] footPlanted=new bool[2];
        readonly bool[] footSettling=new bool[2];
        public bool PhysicalCollapse { get { return ragdollActive; } }
        public bool LeftFootPlanted { get { return footPlanted[0]; } }
        public bool RightFootPlanted { get { return footPlanted[1]; } }
        public Vector3 LeftFootTarget { get { return plantedFeet[0]; } }
        public Vector3 RightFootTarget { get { return plantedFeet[1]; } }
        public Vector3 LeftFootPosition { get { return leftKnee.TransformPoint(new Vector3(0,-.48f,.1f)); } }
        public Vector3 RightFootPosition { get { return rightKnee.TransformPoint(new Vector3(0,-.48f,.1f)); } }
        void BuildMotion()
        {
            gait=Mathf.PI*2f*.28f;
            plantedFeet[0]=transform.TransformPoint(new Vector3(-.20f,.075f,.30f));
            plantedFeet[1]=transform.TransformPoint(new Vector3(.20f,.075f,.30f));
            swingEnd[0]=plantedFeet[0];swingEnd[1]=plantedFeet[1];
            footPlanted[0]=footPlanted[1]=true;
            spine=Pivot("Flexible spine",body,new Vector3(0,1.13f,0));
            var upper=new List<Transform>();
            foreach(Transform child in body)if(child!=spine&&child!=leftHip&&child!=rightHip)upper.Add(child);
            foreach(var child in upper)child.SetParent(spine,true);
            Ball("Pelvis",body,new Vector3(0,1.06f,0),new Vector3(.53f,.29f,.37f),coat);
            foreach(var joint in new[]{leftHip,rightHip,leftKnee,rightKnee,leftArm,rightArm,leftElbow,rightElbow})
                Ball("Rounded joint",joint,Vector3.zero,Vector3.one*(joint==leftHip||joint==rightHip?.24f:.18f),coat*.8f);
            foreach(var joint in new[]{leftArm,rightArm,leftElbow,rightElbow,head}){softJoints.Add(joint);softRotations.Add(joint.localRotation);}
            var torso=Bone(body,null,new Vector3(0,1.35f,0),.30f,.9f,9);
            Bone(head,torso,Vector3.zero,.22f,.5f,3);
            var la=Bone(leftArm,torso,new Vector3(0,-.17f,0),.105f,.36f,1.4f);
            var ra=Bone(rightArm,torso,new Vector3(0,-.17f,0),.105f,.36f,1.4f);
            Bone(leftElbow,la,new Vector3(0,-.17f,0),.085f,.35f,1);
            Bone(rightElbow,ra,new Vector3(0,-.17f,0),.085f,.35f,1);
            var lh=Bone(leftHip,torso,new Vector3(0,-.30f,0),.12f,.60f,3);
            var rh=Bone(rightHip,torso,new Vector3(0,-.30f,0),.12f,.60f,3);
            Bone(leftKnee,lh,new Vector3(0,-.20f,.03f),.105f,.48f,2);
            Bone(rightKnee,rh,new Vector3(0,-.20f,.03f),.105f,.48f,2);
            for(int i=0;i<boneColliders.Count;i++)for(int j=i+1;j<boneColliders.Count;j++)Physics.IgnoreCollision(boneColliders[i],boneColliders[j]);
        }
        // Animated kinematic bones must not interpolate cached physics poses over their
        // moving parents. Interpolation is enabled only after ragdoll takes ownership.
        public bool AnimatedJointsAttached
        {
            get
            {
                if(ragdollActive)return true;
                foreach(var rb in bones)if(!rb.isKinematic||rb.interpolation!=RigidbodyInterpolation.None)return false;
                return Vector3.Distance(head.localPosition,new Vector3(0,.99f,0))<.005f;
            }
        }
        Rigidbody Bone(Transform t,Rigidbody parent,Vector3 center,float radius,float height,float mass)
        {
            var collider=t.gameObject.AddComponent<CapsuleCollider>();collider.center=center;collider.radius=radius;collider.height=height;collider.enabled=false;boneColliders.Add(collider);
            var rb=t.gameObject.AddComponent<Rigidbody>();rb.mass=mass;rb.isKinematic=true;rb.interpolation=RigidbodyInterpolation.None;rb.drag=.25f;rb.angularDrag=2;rb.solverIterations=12;rb.maxAngularVelocity=8;bones.Add(rb);
            if(parent!=null)
            {
                var joint=t.gameObject.AddComponent<CharacterJoint>();joint.connectedBody=parent;joint.anchor=Vector3.zero;joint.enableProjection=true;
                joint.lowTwistLimit=new SoftJointLimit{limit=-30};joint.highTwistLimit=new SoftJointLimit{limit=30};joint.swing1Limit=new SoftJointLimit{limit=55};joint.swing2Limit=new SoftJointLimit{limit=35};
            }
            return rb;
        }
        void Collapse()
        {
            if(ragdollActive)return;ragdollActive=true;
            foreach(var collider in boneColliders)collider.enabled=true;
            Vector3 away=(transform.position-enemyPosition);away.y=0;if(away.sqrMagnitude<.01f)away=-transform.forward;away.Normalize();
            foreach(var rb in bones){rb.interpolation=RigidbodyInterpolation.Interpolate;rb.isKinematic=false;rb.velocity=new Vector3((float)Fighter.VelocityX,0,(float)Fighter.VelocityZ)*.3f+away*1.2f;}
            bones[0].AddForceAtPosition(away*20+Vector3.up*3,head.position,ForceMode.Impulse);
            marker.gameObject.SetActive(false);shield.gameObject.SetActive(false);chargeSphere.gameObject.SetActive(false);
            if(chargeGuide!=null)chargeGuide.gameObject.SetActive(false);
        }
        void WeightedMotion(float delta,float speed,Vector3 localVelocity)
        {
            float dt=Mathf.Min(delta,.04f);if(dt<=0)return;
            float targetLocomotion=Mathf.Clamp01(speed/3f);
            locomotion=Mathf.MoveTowards(locomotion,targetLocomotion,dt*(targetLocomotion>locomotion?7f:4f));
            // One distance-driven cycle drives both feet, torso, head and arm counter-swing.
            // A 1.2 m stride gives a relaxed cadence at normal movement speeds.
            gait+=speed*dt*(Mathf.PI*2f/1.2f);
            Vector3 acceleration=(localVelocity-previousVelocity)/dt;previousVelocity=localVelocity;
            Vector3 desired=new Vector3(Mathf.Clamp(acceleration.z*-.28f,-7,7),Mathf.Sin(gait)*locomotion*3.2f+attackPulse*18,Mathf.Clamp(acceleration.x*.45f,-9,9));
            desired.x+=hitPulse*18;
            // Damped spring integrated with bounded substeps: mass, restoring force and damping.
            int steps=Mathf.Max(1,Mathf.CeilToInt(dt/.008f));float h=dt/steps;
            for(int i=0;i<steps;i++){torsoVelocity+=(90*(desired-torsoSpring)-17*torsoVelocity)*h;torsoSpring+=torsoVelocity*h;}
            spine.localRotation=Quaternion.Euler(torsoSpring);
            float blend=1-Mathf.Exp(-18*dt);
            for(int i=0;i<softJoints.Count;i++){softRotations[i]=Quaternion.Slerp(softRotations[i],softJoints[i].localRotation,blend);softJoints[i].localRotation=softRotations[i];}
            if(Fighter.DodgeRemaining<=0)
            {
                SolveFoot(0,leftHip,leftKnee,localVelocity,dt);
                SolveFoot(1,rightHip,rightKnee,localVelocity,dt);
            }
            else
            {
                // Keep the shoes on the floor while the torso ducks and rolls through a dodge.
                // The short root-relative slide reads as a low dash without changing movement.
                footSettling[0]=footSettling[1]=false;reachStep[0]=reachStep[1]=false;
                plantedFeet[0]=transform.TransformPoint(new Vector3(-.20f,.075f,.1f));
                plantedFeet[1]=transform.TransformPoint(new Vector3(.20f,.075f,.1f));
                footPlanted[0]=footPlanted[1]=true;
                SetLegPose(0,leftHip,leftKnee,plantedFeet[0]);
                SetLegPose(1,rightHip,rightKnee,plantedFeet[1]);
            }
        }
        void SolveFoot(int side,Transform hip,Transform knee,Vector3 velocity,float dt)
        {
            float speed=velocity.magnitude;
            float phase=Mathf.Repeat(gait/(Mathf.PI*2)+(side==1?.5f:0),1);
            Vector3 worldVelocity=transform.TransformDirection(velocity);
            Vector3 travel=worldVelocity.sqrMagnitude>.01f?worldVelocity.normalized:transform.forward;
            Vector3 rest=body.TransformPoint(hip.localPosition+new Vector3(0,0,.1f));
            rest.y=transform.position.y+.075f;
            // A reversal or turn can consume the planted leg's reach before its scheduled
            // swing. Lift from that contact instead of dragging it or stretching the knee.
            if(!reachStep[side]&&Vector3.Distance(plantedFeet[side],hip.position)>1.055f)
            {
                reachStep[side]=true;reachStepTime[side]=0;footSettling[side]=false;
                swingStart[side]=plantedFeet[side];
                swingEnd[side]=rest+worldVelocity*.14f+travel*(speed>.08f?.12f:0);
            }
            if(reachStep[side])
            {
                reachStepTime[side]+=dt;
                float t=Mathf.Clamp01(reachStepTime[side]/.14f);
                plantedFeet[side]=Vector3.Lerp(swingStart[side],swingEnd[side],Mathf.SmoothStep(0,1,t))+Vector3.up*Mathf.Sin(t*Mathf.PI)*.10f;
                footPlanted[side]=false;
                if(t>=1){reachStep[side]=false;footPlanted[side]=true;plantedFeet[side]=swingEnd[side];}
                SetLegPose(side,hip,knee,plantedFeet[side]);return;
            }
            const float stanceFraction=.56f;
            bool stance=phase<stanceFraction;
            if(speed<.08f&&!footPlanted[side]&&!footSettling[side])
            {
                footSettling[side]=true;settleElapsed[side]=0;swingStart[side]=plantedFeet[side];
            }
            if(footSettling[side])
            {
                settleElapsed[side]+=dt;
                float settle=Mathf.SmoothStep(0,1,Mathf.Clamp01(settleElapsed[side]/.16f));
                plantedFeet[side]=Vector3.Lerp(swingStart[side],rest,settle);
                if(settle>=1){footSettling[side]=false;footPlanted[side]=true;plantedFeet[side]=rest;}
                SetLegPose(side,hip,knee,plantedFeet[side]);
                return;
            }
            if(speed<.08f&&footPlanted[side])
            {
                SetLegPose(side,hip,knee,plantedFeet[side]);
                return;
            }
            if(stance)
            {
                if(!footPlanted[side])
                {
                    // The swing endpoint is predicted from the movement distance still left
                    // in this half cycle, so landing and stance begin at the same world point.
                    plantedFeet[side]=swingEnd[side];
                    footPlanted[side]=true;
                }
            }
            else
            {
                if(footPlanted[side])
                {
                    swingStart[side]=plantedFeet[side];
                    // Entry can occur mid-cycle after idle or a dodge; start from the held foot.
                    swingStartPhase[side]=phase;
                    float swingDistance=(1f-phase)*1.2f;
                    float landingLead=stanceFraction*1.2f*.5f;
                    swingEnd[side]=rest+travel*(swingDistance+landingLead);
                }
                footPlanted[side]=false;float t=(phase-swingStartPhase[side])/Mathf.Max(.01f,1f-swingStartPhase[side]);
                t=Mathf.Clamp01(t);
                float eased=Mathf.SmoothStep(0,1,t);
                plantedFeet[side]=Vector3.Lerp(swingStart[side],swingEnd[side],eased)+Vector3.up*Mathf.Sin(t*Mathf.PI)*.16f*locomotion;
            }
            SetLegPose(side,hip,knee,plantedFeet[side]);
        }
        void SetLegPose(int side,Transform hip,Transform knee,Vector3 ankle)
        {
            float a=.60f,b=.49f;
            Vector3 delta=ankle-hip.position;float d=Mathf.Clamp(delta.magnitude,.16f,a+b-.008f);
            Vector3 down=delta.normalized;
            Vector3 bend=Vector3.ProjectOnPlane(transform.forward,down).normalized;
            if(bend.sqrMagnitude<.001f)bend=Vector3.ProjectOnPlane(Vector3.forward,down).normalized;
            float along=(a*a-b*b+d*d)/(2*d);float outward=Mathf.Sqrt(Mathf.Max(0,a*a-along*along));Vector3 joint=hip.position+down*along+bend*outward;
            hip.rotation=Quaternion.FromToRotation(hip.TransformDirection(Vector3.down),joint-hip.position)*hip.rotation;
            knee.rotation=Quaternion.FromToRotation(knee.TransformDirection(new Vector3(0,-.48f,.1f)),ankle-knee.position)*knee.rotation;
        }
    }
}
