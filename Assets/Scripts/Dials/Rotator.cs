﻿using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace LevelUP.Dial
{
    public class Rotator : MonoBehaviour
    {

        public float left_bound = 135f;
        public float right_bound = 45f;

        [SerializeField] Transform linkedDial;
        [SerializeField] private int snapRotationAmount = 25;
        [SerializeField] private float angleTolerance;
        [SerializeField] private GameObject RighthandModel;
        [SerializeField] private GameObject LefthandModel;
        [SerializeField] bool shouldUseDummyHands;

        private XRBaseInteractor interactor;
        private float startAngle;
        private bool requiresStartAngle = true;
        private bool shouldGetHandRotation = false;
        private XRGrabInteractable grabInteractor => GetComponentInChildren<XRGrabInteractable>();

        private void OnEnable()
        {
            grabInteractor.selectEntered.AddListener(GrabbedBy);
            grabInteractor.selectExited.AddListener(GrabEnd);
        }
        private void OnDisable()
        {
            grabInteractor.selectEntered.RemoveListener(GrabbedBy);
            grabInteractor.selectExited.RemoveListener(GrabEnd);
        }

        private void GrabEnd(SelectExitEventArgs arg0)
        {
            shouldGetHandRotation = false;
            requiresStartAngle = true;
            HandModelVisibility(false);
        }

        private void GrabbedBy(SelectEnterEventArgs arg0)
        {

            var comp = GetComponent<XRGrabInteractable>();
            Debug.Log("interactable is null: " + comp == null);

            interactor = comp.selectingInteractor;

            Debug.Log("Interactor is null: " + interactor == null);

            //var directint = interactor.GetComponent<XRDirectInteractor>();
            
            //directint.hideControllerOnSelect = true;

            Debug.Log("grabbed");

            shouldGetHandRotation = true;
            startAngle = 0f;

            HandModelVisibility(true);
        }

        private void HandModelVisibility(bool visibilityState)
        {
            if (!shouldUseDummyHands)
                return;

            if (interactor.CompareTag("RightHand"))
                RighthandModel.SetActive(visibilityState);
            else
                LefthandModel.SetActive(visibilityState);
        }

        void Update()
        {
            if (shouldGetHandRotation)
            {
                var rotationAngle = GetInteractorRotation(); //gets the current controller angle
                GetRotationDistance(rotationAngle);
            }
        }

        public float GetInteractorRotation() => interactor.GetComponent<Transform>().eulerAngles.z;

        #region TheMath!
        private void GetRotationDistance(float currentAngle)
        {
            if (!requiresStartAngle)
            {
                var angleDifference = Mathf.Abs(startAngle - currentAngle);

                if (angleDifference > angleTolerance)
                {
                    if (angleDifference > 270f) //checking to see if the user has gone from 0-360 - a very tiny movement but will trigger the angletolerance
                    {
                        float angleCheck;

                        if (startAngle < currentAngle) 
                        {
                            angleCheck = CheckAngle(currentAngle, startAngle);

                            if (angleCheck < angleTolerance)
                                return;
                            else
                            {


                                if(linkedDial.localEulerAngles.z + snapRotationAmount > right_bound && linkedDial.localEulerAngles.z + snapRotationAmount <left_bound)
                                {
                                    return;
                                }

                                RotateDialClockwise();
                                startAngle = currentAngle;
                            }
                        }
                        else if (startAngle > currentAngle) 
                        {
                            angleCheck = CheckAngle(currentAngle, startAngle);

                            if (angleCheck < angleTolerance)
                                return;
                            else
                            {


                                if (linkedDial.localEulerAngles.z - snapRotationAmount > right_bound && linkedDial.localEulerAngles.z - snapRotationAmount < left_bound)
                                {
                                    return;
                                }



                                RotateDialAntiClockwise();
                                startAngle = currentAngle;
                            }
                        }
                    }
                    else
                    {
                        if (startAngle < currentAngle)
                        {
                            if (linkedDial.localEulerAngles.z - snapRotationAmount > right_bound && linkedDial.localEulerAngles.z - snapRotationAmount < left_bound)
                            {
                                return;
                            }



                            RotateDialAntiClockwise();
                            startAngle = currentAngle;
                        }
                        else if (startAngle > currentAngle)
                        {

                            if (linkedDial.localEulerAngles.z + snapRotationAmount > right_bound && linkedDial.localEulerAngles.z + snapRotationAmount < left_bound)
                            {
                                return;
                            }


                            RotateDialClockwise();
                            startAngle = currentAngle;
                        }
                    }
                }
            }
            else
            {
                requiresStartAngle = false;
                startAngle = currentAngle;
            }
        }
        #endregion

        private float CheckAngle(float currentAngle, float startAngle) => (360f - currentAngle) + startAngle;

        private void RotateDialClockwise()
        {
            linkedDial.localEulerAngles = new Vector3(linkedDial.localEulerAngles.x, 
                                                      linkedDial.localEulerAngles.y, 
                                                      linkedDial.localEulerAngles.z + snapRotationAmount);

            if (TryGetComponent<IDial>(out IDial dial))
                dial.DialChanged(linkedDial.localEulerAngles.z);

        }

        private void RotateDialAntiClockwise()
        {
            linkedDial.localEulerAngles = new Vector3(linkedDial.localEulerAngles.x, 
                                                      linkedDial.localEulerAngles.y, 
                                                      linkedDial.localEulerAngles.z - snapRotationAmount);

            if(TryGetComponent<IDial>(out IDial dial))
                dial.DialChanged(linkedDial.localEulerAngles.z);
        }
    }
}
