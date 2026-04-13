using System;
using UnityEngine;

[Serializable]
public class OccluderEntry
{
    public bool enabled = true;
    public MeshRenderer renderer;
    [Range(0f, 1f)] public float opacity = 1f;

    [Space]

    public bool invert = false;
    [Tooltip("Expands the meshes shape in the cookie.")]
    public int dilateRadius = 0;
    [Tooltip("Shrinks the meshes shape in the cookie.")]
    public int erodeRadius = 0;
}
