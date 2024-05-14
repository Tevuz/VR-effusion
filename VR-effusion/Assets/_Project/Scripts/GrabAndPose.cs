using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.XR.Interaction.Toolkit;

#if UNITY_EDITOR
using UnityEditor;
#endif


namespace VREffusion {
    public class GrabAndPose : MonoBehaviour {
        public float poseTransitionDuration = 0.2f;
        public HandData rightHandPose;
        public HandData leftHandPose;

        private Vector3 startingHandPosition;
        private Vector3 finalHandPosition;
        private Quaternion startingHandRotation;
        private Quaternion finalHandRotation;

        private Quaternion[] startingFingerRotations;
        private Quaternion[] finalFingerRotations;

        // Start is called before the first frame update
        void Start() {
            XRGrabInteractable grabInteractable = GetComponent<XRGrabInteractable>();
            grabInteractable.selectEntered.AddListener(SetupPose);
            grabInteractable.selectExited.AddListener(UnSetPose);

            rightHandPose.gameObject.SetActive(false);

            leftHandPose.gameObject.SetActive(false);
        }


        public void SetupPose(BaseInteractionEventArgs args) {
            HandData handData = null;
            if (args.interactorObject is XRDirectInteractor) {
                handData = args.interactorObject.transform.GetComponentInChildren<HandData>();
            } else if (args.interactorObject is XRRayInteractor) {
                HandData[] components = args.interactorObject.transform.parent.GetComponentsInChildren<HandData>();
                handData = components.First(o => o.transform.parent.tag == args.interactorObject.transform.tag);
            }

            if (handData is null)
                return;

            handData.animator.enabled = false;
            SetHandDataValues(handData, handData.handType == HandData.HandModelType.Right ? rightHandPose : leftHandPose);
            SetHandData(handData, finalHandPosition, finalHandRotation, finalFingerRotations);
        }

        public void UnSetPose(BaseInteractionEventArgs args) {
            HandData handData = null;
            if (args.interactorObject is XRDirectInteractor) {
                handData = args.interactorObject.transform.GetComponentInChildren<HandData>();
            } else if (args.interactorObject is XRRayInteractor) {
                HandData[] components = args.interactorObject.transform.parent.GetComponentsInChildren<HandData>();
                handData = components.First(o => o.transform.parent.tag == args.interactorObject.transform.tag);
            }

            if (handData is null)
                return;

            handData.animator.enabled = true;
            SetHandData(handData, startingHandPosition, startingHandRotation, startingFingerRotations);
        }

        public void SetHandDataValues(HandData h1, HandData h2) {
            startingHandPosition = new Vector3(h1.root.localPosition.x / h1.root.localScale.x,
                h1.root.localPosition.y / h1.root.localScale.y, h1.root.localPosition.z / h1.root.localScale.z);
            finalHandPosition = new Vector3(h2.root.localPosition.x / h2.root.localScale.x,
                h2.root.localPosition.y / h2.root.localScale.y, h2.root.localPosition.z / h2.root.localScale.z);

            startingHandRotation = h1.root.localRotation;
            finalHandRotation = h2.root.localRotation;

            startingFingerRotations = new Quaternion[h1.fingerBones.Length];
            finalFingerRotations = new Quaternion[h2.fingerBones.Length];

            for (int i = 0; i < h1.fingerBones.Length; i++) {
                startingFingerRotations[i] = h1.fingerBones[i].localRotation;
                finalFingerRotations[i] = h2.fingerBones[i].localRotation;

            }
        }

        public void SetHandData(HandData h, Vector3 newPosition, Quaternion newRotation,
            Quaternion[] newBonesRotation) {
            h.root.localPosition = newPosition;
            h.root.localRotation = newRotation;

            for (int i = 0; i < newBonesRotation.Length; i++) {
                h.fingerBones[i].localRotation = newBonesRotation[i];
            }
        }

        public IEnumerator SetHandDataRoutine(HandData h, Vector3 newPosition, Quaternion newRotation,
            Quaternion[] newBonesRotation, Vector3 startingPosition, Quaternion startingRotation,
            Quaternion[] startingBonesRotation) {
            float timer = 0;

            while (timer < poseTransitionDuration) {
                Vector3 p = Vector3.Lerp(startingPosition, newPosition, timer / poseTransitionDuration);
                Quaternion r = Quaternion.Lerp(startingRotation, newRotation, timer / poseTransitionDuration);

                h.root.localPosition = p;
                h.root.localRotation = r;

                for (int i = 0; i < newBonesRotation.Length; i++) {
                    h.fingerBones[i].localRotation = Quaternion.Lerp(startingBonesRotation[i], newBonesRotation[i],
                        timer / poseTransitionDuration);
                }

                timer += Time.deltaTime;
                yield return null;
            }

        }

#if UNITY_EDITOR

        [MenuItem("Tools/Mirror Selected Left To Right Grab Pose")]
        public static void MirrorLeftToRightPose() {
            Debug.Log("MIRROR RIGHT POSE");
            GrabAndPose handPose = Selection.activeGameObject.GetComponent<GrabAndPose>();
            handPose.MirrorPose(handPose.rightHandPose, handPose.leftHandPose);
        }

        [MenuItem("Tools/Mirror Selected Right To Left Grab Pose")]
        public static void MirrorRightToLeftPose() {
            Debug.Log("MIRROR RIGHT POSE");
            GrabAndPose handPose = Selection.activeGameObject.GetComponent<GrabAndPose>();
            handPose.MirrorPose(handPose.leftHandPose, handPose.rightHandPose);
        }

#endif
        public void MirrorPose(HandData poseToMirror, HandData poseUsedToMirror) {
            Vector3 mirroredPosition = poseUsedToMirror.root.localPosition;
            mirroredPosition.x *= -1;


            Quaternion mirroredQuaternion = poseUsedToMirror.root.localRotation;
            mirroredQuaternion.y *= -1;
            mirroredQuaternion.z *= -1;


            poseToMirror.root.localPosition = mirroredPosition;
            poseToMirror.root.localRotation = mirroredQuaternion;

            for (int i = 0; i < poseUsedToMirror.fingerBones.Length; i++) {
                poseToMirror.fingerBones[i].localRotation = poseUsedToMirror.fingerBones[i].localRotation;
            }
        }
    }
}