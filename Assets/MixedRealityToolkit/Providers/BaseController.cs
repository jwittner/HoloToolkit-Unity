﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.MixedReality.Toolkit.Utilities;
using System;
using UnityEngine;

namespace Microsoft.MixedReality.Toolkit.Input
{
    /// <summary>
    /// Base Controller class to inherit from for all controllers.
    /// </summary>
    public abstract class BaseController : IMixedRealityController
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        protected BaseController(TrackingState trackingState, Handedness controllerHandedness, IMixedRealityInputSource inputSource = null, MixedRealityInteractionMapping[] interactions = null)
        {
            TrackingState = trackingState;
            ControllerHandedness = controllerHandedness;
            InputSource = inputSource;
            Interactions = interactions;

            IsPositionAvailable = false;
            IsPositionApproximate = false;
            IsRotationAvailable = false;

            Enabled = true;
        }

        /// <summary>
        /// The default interactions for this controller.
        /// </summary>
        public virtual MixedRealityInteractionMapping[] DefaultInteractions { get; } = null;

        /// <summary>
        /// The Default Left Handed interactions for this controller.
        /// </summary>
        public virtual MixedRealityInteractionMapping[] DefaultLeftHandedInteractions { get; } = null;

        /// <summary>
        /// The Default Right Handed interactions for this controller.
        /// </summary>
        public virtual MixedRealityInteractionMapping[] DefaultRightHandedInteractions { get; } = null;

        #region IMixedRealityController Implementation

        /// <inheritdoc />
        public bool Enabled { get; set; }

        /// <inheritdoc />
        public TrackingState TrackingState { get; protected set; }

        /// <inheritdoc />
        public Handedness ControllerHandedness { get; }

        /// <inheritdoc />
        public IMixedRealityInputSource InputSource { get; }

        public IMixedRealityControllerVisualizer Visualizer { get; protected set; }

        /// <inheritdoc />
        public bool IsPositionAvailable { get; protected set; }

        /// <inheritdoc />
        public bool IsPositionApproximate { get; protected set; }

        /// <inheritdoc />
        public bool IsRotationAvailable { get; protected set; }

        /// <inheritdoc />
        public MixedRealityInteractionMapping[] Interactions { get; private set; } = null;

        public Vector3 AngularVelocity { get; protected set; }

        public Vector3 Velocity { get; protected set; }

        /// <inheritdoc />
        public virtual bool IsInPointingPose => true;

        #endregion IMixedRealityController Implementation

        /// <summary>
        /// Sets up the configuration based on the Mixed Reality Controller Mapping Profile.
        /// </summary>
        [Obsolete("The second parameter is no longer used. This method now reads from the controller's input source.")]
        public bool SetupConfiguration(Type controllerType, InputSourceType inputSourceType = InputSourceType.Controller)
        {
            return SetupConfiguration(controllerType);
        }

        /// <summary>
        /// Sets up the configuration based on the Mixed Reality Controller Mapping Profile.
        /// </summary>
        /// <param name="controllerType">The type this controller represents.</param>
        public bool SetupConfiguration(Type controllerType)
        {
            if (IsControllerMappingEnabled())
            {
                if (GetControllerVisualizationProfile() != null &&
                    GetControllerVisualizationProfile().RenderMotionControllers)
                {
                    TryRenderControllerModel(controllerType, InputSource.SourceType);
                }

                // We can only enable controller profiles if mappings exist.
                var controllerMappings = GetControllerMappings();

                // Have to test that a controller type has been registered in the profiles,
                // else its Unity Input manager mappings will not have been set up by the inspector.
                bool profileFound = false;
                if (controllerMappings != null)
                {
                    for (int i = 0; i < controllerMappings.Length; i++)
                    {
                        if (controllerMappings[i].ControllerType.Type == controllerType)
                        {
                            profileFound = true;

                            // If it is an exact match, assign interaction mappings.
                            if (controllerMappings[i].Handedness == ControllerHandedness &&
                                controllerMappings[i].Interactions.Length > 0)
                            {
                                MixedRealityInteractionMapping[] profileInteractions = controllerMappings[i].Interactions;
                                MixedRealityInteractionMapping[] newInteractions = new MixedRealityInteractionMapping[profileInteractions.Length];

                                for (int j = 0; j < profileInteractions.Length; j++)
                                {
                                    newInteractions[j] = new MixedRealityInteractionMapping(profileInteractions[j]);
                                }

                                AssignControllerMappings(newInteractions);
                                break;
                            }
                        }
                    }
                }

                // If no controller mappings found, warn the user.  Does not stop the project from running.
                if (Interactions == null || Interactions.Length < 1)
                {
                    SetupDefaultInteractions(ControllerHandedness);

                    // We still don't have controller mappings, so this may be a custom controller. 
                    if (Interactions == null || Interactions.Length < 1)
                    {
                        Debug.LogWarning($"No Controller interaction mappings found for {controllerType}.");
                        return false;
                    }
                }

                if (!profileFound)
                {
                    Debug.LogWarning($"No controller profile found for type {controllerType}, please ensure all controllers are defined in the configured MixedRealityControllerConfigurationProfile.");
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Assign the default interactions based on controller handedness, if necessary. 
        /// </summary>
        public virtual void SetupDefaultInteractions(Handedness controllerHandedness)
        {
            switch (controllerHandedness)
            {
                case Handedness.Left:
                    AssignControllerMappings(DefaultLeftHandedInteractions);
                    break;
                case Handedness.Right:
                    AssignControllerMappings(DefaultRightHandedInteractions);
                    break;
                default:
                    AssignControllerMappings(DefaultInteractions);
                    break;
            }
        }

        /// <summary>
        /// Load the Interaction mappings for this controller from the configured Controller Mapping profile
        /// </summary>
        /// <param name="mappings">Configured mappings from a controller mapping profile</param>
        public void AssignControllerMappings(MixedRealityInteractionMapping[] mappings)
        {
            Interactions = mappings;
        }

        /// <summary>
        /// Try to render a controller model for this controller from the visualization profile.
        /// </summary>
        /// <param name="controllerType">The type of controller to load the model for.</param>
        /// <param name="inputSourceType">Whether the model represents a hand or a controller.</param>
        /// <returns>True if a model was successfully loaded or model rendering is disabled. False if a model tried to load but failed.</returns>
        protected virtual bool TryRenderControllerModel(Type controllerType, InputSourceType inputSourceType)
        {
            GameObject controllerModel = null;

            if (GetControllerVisualizationProfile() == null ||
                !GetControllerVisualizationProfile().RenderMotionControllers)
            {
                return true;
            }

            // If a specific controller template wants to override the global model, assign that instead.
            if (IsControllerMappingEnabled() &&
                GetControllerVisualizationProfile() != null &&
                inputSourceType == InputSourceType.Controller &&
                !(GetControllerVisualizationProfile().GetUseDefaultModelsOverride(controllerType, ControllerHandedness)))
            {
                controllerModel = GetControllerVisualizationProfile().GetControllerModelOverride(controllerType, ControllerHandedness);
            }

            // Get the global controller model for each hand.
            if (controllerModel == null &&
                GetControllerVisualizationProfile() != null)
            {
                if (inputSourceType == InputSourceType.Controller)
                {
                    if (ControllerHandedness == Handedness.Left &&
                        GetControllerVisualizationProfile().GlobalLeftHandModel != null)
                    {
                        controllerModel = GetControllerVisualizationProfile().GlobalLeftHandModel;
                    }
                    else if (ControllerHandedness == Handedness.Right &&
                        GetControllerVisualizationProfile().GlobalRightHandModel != null)
                    {
                        controllerModel = GetControllerVisualizationProfile().GlobalRightHandModel;
                    }
                }
                else if (inputSourceType == InputSourceType.Hand)
                {
                    if (ControllerHandedness == Handedness.Left &&
                        GetControllerVisualizationProfile().GlobalLeftHandVisualizer != null)
                    {
                        controllerModel = GetControllerVisualizationProfile().GlobalLeftHandVisualizer;
                    }
                    else if (ControllerHandedness == Handedness.Right &&
                        GetControllerVisualizationProfile().GlobalRightHandVisualizer != null)
                    {
                        controllerModel = GetControllerVisualizationProfile().GlobalRightHandVisualizer;
                    }
                }
            }

            if (controllerModel == null)
            {
                // no controller model available
                return false;
            }

            // If we've got a controller model prefab, then create it and place it in the scene.
            GameObject controllerObject = UnityEngine.Object.Instantiate(controllerModel);

            return TryAddControllerModelToSceneHierarchy(controllerObject);
        }

        protected bool TryAddControllerModelToSceneHierarchy(GameObject controllerObject)
        {
            if (controllerObject != null)
            {
                controllerObject.name = $"{ControllerHandedness}_{controllerObject.name}";

                MixedRealityPlayspace.AddChild(controllerObject.transform);

                Visualizer = controllerObject.GetComponent<IMixedRealityControllerVisualizer>();

                if (Visualizer != null)
                {
                    Visualizer.Controller = this;
                    return true;
                }
                else
                {
                    Debug.LogError($"{controllerObject.name} is missing a IMixedRealityControllerVisualizer component!");
                    return false;
                }
            }

            return false;
        }

        #region MRTK instance helpers

        protected static MixedRealityControllerVisualizationProfile GetControllerVisualizationProfile()
        {
            if (CoreServices.InputSystem?.InputSystemProfile != null)
            {
                return CoreServices.InputSystem.InputSystemProfile.ControllerVisualizationProfile;
            }

            return null;
        }

        protected static bool IsControllerMappingEnabled()
        {
            if (CoreServices.InputSystem?.InputSystemProfile != null)
            {
                return CoreServices.InputSystem.InputSystemProfile.IsControllerMappingEnabled;
            }

            return false;
        }

        protected static MixedRealityControllerMapping[] GetControllerMappings()
        {
            if (CoreServices.InputSystem?.InputSystemProfile != null &&
                CoreServices.InputSystem.InputSystemProfile.ControllerMappingProfile != null)
            {
                // We can only enable controller profiles if mappings exist.
                return CoreServices.InputSystem.InputSystemProfile.ControllerMappingProfile.MixedRealityControllerMappings;
            }

            return null;
        }

        #endregion MRTK instance helpers
    }
}
