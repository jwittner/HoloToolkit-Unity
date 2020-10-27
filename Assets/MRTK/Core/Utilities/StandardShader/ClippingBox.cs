// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using UnityEngine;

namespace Microsoft.MixedReality.Toolkit.Utilities
{
    /// <summary>
    /// Component to animate and visualize a box that can be used with 
    /// per pixel based clipping.
    /// </summary>
    [ExecuteInEditMode]
    [AddComponentMenu("Scripts/MRTK/Core/ClippingBox")]
    public class ClippingBox : ClippingPrimitive
    {
        /// <summary>
        /// The property name of the clip box inverse transformation matrix within the shader.
        /// </summary>
        protected int clipBoxInverseTransformID;
        private Matrix4x4 clipBoxInverseTransform;


        /// <inheritdoc />
        protected override string Keyword
        {
            get { return "_CLIPPING_BOX"; }
        }

        /// <inheritdoc />
        protected override string ClippingSideProperty
        {
            get { return "_ClipBoxSide"; }
        }

        /// <summary>
        /// Renders a visual representation of the clipping primitive when selected.
        /// </summary>
        protected void OnDrawGizmosSelected()
        {
            if (enabled)
            {
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.DrawWireCube(Vector3.zero, Vector3.one);

                //Gizmos.color = Color.blue;
                //Gizmos.matrix = Matrix4x4.identity;
                //Gizmos.DrawWireCube(transform.)

                //Bounds boxBounds = new Bounds(Vector3.zero, Vector3.one).Transform(transform.localToWorldMatrix);
                //return !boxBounds.Intersects(bounds);
            }
        }

        /// <inheritdoc />
        protected override void Initialize()
        {
            base.Initialize();

            clipBoxInverseTransformID = Shader.PropertyToID("_ClipBoxInverseTransform");
        }

        protected override void BeginUpdateShaderProperties()
        {   
            clipBoxInverseTransform = transform.worldToLocalMatrix;

            base.BeginUpdateShaderProperties();
        }

        private Vector3[] corners;
        protected override bool Cull(Bounds bounds)
        {
            if(ClippingSide == Side.Inside) { return false; }

            Bounds boxBounds = new Bounds(Vector3.zero, Vector3.one).Transform(transform.localToWorldMatrix);
            return !boxBounds.Intersects(bounds);
        }

        protected override void UpdateShaderProperties(MaterialPropertyBlock materialPropertyBlock)
        {
            materialPropertyBlock.SetMatrix(clipBoxInverseTransformID, clipBoxInverseTransform);
        }
    }
}
