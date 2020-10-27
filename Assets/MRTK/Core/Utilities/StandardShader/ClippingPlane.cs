// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using UnityEngine;

namespace Microsoft.MixedReality.Toolkit.Utilities
{
    /// <summary>
    /// Component to animate and visualize a plane that can be used with 
    /// per pixel based clipping.
    /// </summary>
    [ExecuteInEditMode]
    [AddComponentMenu("Scripts/MRTK/Core/ClippingPlane")]
    public class ClippingPlane : ClippingPrimitive
    {
        /// <summary>
        /// The property name of the clip plane data within the shader.
        /// </summary>
        protected int clipPlaneID;
        private Vector4 clipPlane;

        /// <inheritdoc />
        protected override string Keyword
        {
            get { return "_CLIPPING_PLANE"; }
        }

        /// <inheritdoc />
        protected override string ClippingSideProperty
        {
            get { return "_ClipPlaneSide"; }
        }

        /// <summary>
        /// Renders a visual representation of the clipping primitive when selected.
        /// </summary>
        protected void OnDrawGizmosSelected()
        {
            if (enabled)
            {
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.DrawWireCube(Vector3.zero, new Vector3(1.0f, 0.0f, 1.0f));
                Gizmos.DrawLine(Vector3.zero, Vector3.up * -0.5f);
            }
        }

        /// <inheritdoc />
        protected override void Initialize()
        {
            base.Initialize();

            clipPlaneID = Shader.PropertyToID("_ClipPlane");
        }

        protected override void BeginUpdateShaderProperties()
        {
            Vector3 up = transform.up;
            clipPlane = new Vector4(up.x, up.y, up.z, Vector3.Dot(up, transform.position));

            base.BeginUpdateShaderProperties();
        }

        private Vector3[] corners;
        protected override bool Cull(Bounds bounds)
        {
            Vector3 planePosition = new Vector3(
                clipPlane.x * clipPlane.w,
                clipPlane.y * clipPlane.w,
                clipPlane.z * clipPlane.w);

            bounds.GetCornerPositions(ref corners);
            foreach (Vector3 point in corners)
            {
                Vector3 delta = new Vector3(
                    point.x - planePosition.x,
                    point.y - planePosition.y,
                    point.z - planePosition.z);

                float distance = delta.x * clipPlane.x + delta.y * clipPlane.y + delta.z + clipPlane.z;

                switch (ClippingSide)
                {
                    case Side.Inside: if (distance <= 0f) { return false; } break;
                    case Side.Outside: if (distance >= 0f) { return false; } break;
                }
            }

            return true;
        }

        /// <inheritdoc />
        protected override void UpdateShaderProperties(MaterialPropertyBlock materialPropertyBlock)
        {
            materialPropertyBlock.SetVector(clipPlaneID, clipPlane);
        }
    }
}
