﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using UnityEngine;
using UnityEngine.VR.WSA;

namespace HoloToolkit.Unity
{
    /// <summary>
    /// StabilizationPlaneModifier handles the setting of the stabilization plane in several ways.
    /// </summary>
    public class StabilizationPlaneModifier : Singleton<StabilizationPlaneModifier>
    {
        [Tooltip("Checking enables SetFocusPointForFrame to set the stabilization plane.")]
        public bool SetStabilizationPlane = true;
        [Tooltip("Lerp speed when moving focus point closer.")]
        public float LerpStabilizationPlanePowerCloser = 4.0f;
        [Tooltip("Lerp speed when moving focus point farther away.")]
        public float LerpStabilizationPlanePowerFarther = 7.0f;

        [Tooltip("Used to temporarily override the location of the stabilization plane.")]
        public Transform TargetOverride;

        [Tooltip("Keeps track of position-based velocity for the target object.")]
        public bool TrackVelocity = false;

        [Tooltip("Use the GazeManager class to set the plane to the gazed upon hologram.")]
        public bool UseGazeManager = true;

        [Tooltip("Default distance to set plane if plane is gaze-locked.")]
        public float DefaultPlaneDistance = 2.0f;

        [Tooltip("Visualize the plane at runtime.")]
        public bool DrawGizmos = false;

        // Used for rendering plane gizmo.
        private Vector3 planePosition;

        private float currentPlaneDistance = 4.0f;

        private Vector3 previousPosition;

        private const float FOCUS_POINT_FRAMERATE = 60.0f;

        /// <summary>
        /// Updates the focus point for every frame after all objects have finished moving.
        /// </summary>
        private void LateUpdate()
        {
            if (SetStabilizationPlane && Camera.main != null)
            {
                if (TargetOverride != null)
                {
                    ConfigureTransformOverridePlane();
                }
                else if (UseGazeManager && GazeManager.Instance != null)
                {
                    ConfigureGazeManagerPlane();
                }
                else
                {
                    ConfigureFixedDistancePlane();
                }

#if UNITY_EDITOR
                if (DrawGizmos)
                {
                    OnDrawGizmos();
                }
#endif
            }
        }

        private void ConfigureTransformOverridePlane()
        {
            planePosition = TargetOverride.position;

            Vector3 velocity = Vector3.zero;
            if (TrackVelocity)
            {
                velocity = UpdateVelocity();
            }
            
            // Place the plane at the desired depth in front of the camera and billboard it to the camera.
            HolographicSettings.SetFocusPointForFrame(TargetOverride.position, -Camera.main.transform.forward, velocity);
        }

        private void ConfigureGazeManagerPlane()
        {
            Vector3 gazeOrigin = Camera.main.transform.position;
            Vector3 gazeDirection = Camera.main.transform.forward;
            float lastHitDistance = GazeManager.Instance.HitInfo.distance;

            // Calculate the delta between camera's position and current hit position.
            float focusPointDistance = (gazeOrigin - GazeManager.Instance.Position).magnitude;
            float lerpPower = focusPointDistance > currentPlaneDistance ? LerpStabilizationPlanePowerFarther
                                                                        : LerpStabilizationPlanePowerCloser;

            // Smoothly move the focus point from previous hit position to new position.
            currentPlaneDistance = Mathf.Lerp(currentPlaneDistance, focusPointDistance, lerpPower * Time.deltaTime);

            planePosition = gazeOrigin + (gazeDirection * currentPlaneDistance);

            HolographicSettings.SetFocusPointForFrame(planePosition, -gazeDirection, Vector3.zero);
        }

        private void ConfigureFixedDistancePlane()
        {
            float lerpPower = DefaultPlaneDistance > currentPlaneDistance ? LerpStabilizationPlanePowerFarther
                                                                          : LerpStabilizationPlanePowerCloser;

            // Smoothly move the focus point from previous hit position to new position.
            currentPlaneDistance = Mathf.Lerp(currentPlaneDistance, DefaultPlaneDistance, lerpPower * Time.deltaTime);

            planePosition = Camera.main.transform.position + (Camera.main.transform.forward * currentPlaneDistance);
            HolographicSettings.SetFocusPointForFrame(planePosition, -Camera.main.transform.forward, Vector3.zero);
        }

        private Vector3 UpdateVelocity()
        {
            Vector3 velocity = (TargetOverride.position - previousPosition) * FOCUS_POINT_FRAMERATE;
            previousPosition = TargetOverride.position;
            return velocity;
        }

        private void OnDrawGizmos()
        {
            if (UnityEngine.Application.isPlaying)
            {
                Vector3 focalPlaneNormal = -Camera.main.transform.forward;
                Vector3 planeUp = Vector3.Cross(Vector3.Cross(focalPlaneNormal, Vector3.up), focalPlaneNormal);
                Gizmos.matrix = Matrix4x4.TRS(planePosition, Quaternion.LookRotation(focalPlaneNormal, planeUp), new Vector3(4.0f, 3.0f, 0.01f));

                Color gizmoColor = Color.magenta;
                gizmoColor.a = 0.5f;
                Gizmos.color = gizmoColor;

                Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
                Gizmos.DrawCube(Vector3.zero, Vector3.one);
            }
        }
    }
}