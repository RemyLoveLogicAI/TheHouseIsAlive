using System.Collections.Generic;
using UnityEngine;

namespace TheHouseIsAlive
{
    /// <summary>
    /// Generates a procedural shadowy silhouette mesh at runtime
    /// with elongated limbs and glowing eyes for immediate mixed reality testing.
    /// </summary>
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class ProceduralCreatureMesh : MonoBehaviour
    {
        [Header("Appearance")]
        [SerializeField] private Color bodyColor = new Color(0.04f, 0.04f, 0.05f, 0.95f);
        [SerializeField] private Color eyeColor = new Color(0.9f, 0.8f, 0.2f, 1.0f);
        [SerializeField] private float creatureHeight = 1.6f;

        private void Awake()
        {
            BuildCreature();
        }

        /// <summary>
        /// Creates a standalone creature object with its procedural mesh.
        /// </summary>
        public static GameObject CreateCreatureGameObject(string name = "ShadowEntity_ProceduralMesh")
        {
            GameObject go = new GameObject(name);
            var creature = go.AddComponent<ProceduralCreatureMesh>();
            if (creature.GetComponent<MeshFilter>().sharedMesh == null)
            {
                creature.BuildCreature();
            }
            return go;
        }

        public void BuildCreature()
        {
            var meshFilter = GetComponent<MeshFilter>();
            var meshRenderer = GetComponent<MeshRenderer>();

            Mesh mesh = new Mesh { name = "Procedural_ShadowEntity" };

            List<Vector3> vertices = new List<Vector3>();
            List<int> triangles = new List<int>();

            // 1. Slender torso (elongated diamond/prism)
            Vector3 top = new Vector3(0, creatureHeight, 0);
            Vector3 center = new Vector3(0, creatureHeight * 0.55f, 0);
            Vector3 basePt = Vector3.zero;

            Vector3[] midRing = {
                new Vector3(-0.18f, center.y, 0),
                new Vector3(0, center.y, 0.12f),
                new Vector3(0.18f, center.y, 0),
                new Vector3(0, center.y, -0.12f)
            };

            // Head apex
            int topIdx = vertices.Count; vertices.Add(top);
            int baseIdx = vertices.Count; vertices.Add(basePt);

            int r0 = vertices.Count; vertices.Add(midRing[0]);
            int r1 = vertices.Count; vertices.Add(midRing[1]);
            int r2 = vertices.Count; vertices.Add(midRing[2]);
            int r3 = vertices.Count; vertices.Add(midRing[3]);

            // Upper pyramid
            AddTri(triangles, topIdx, r0, r1);
            AddTri(triangles, topIdx, r1, r2);
            AddTri(triangles, topIdx, r2, r3);
            AddTri(triangles, topIdx, r3, r0);

            // Lower pyramid
            AddTri(triangles, baseIdx, r1, r0);
            AddTri(triangles, baseIdx, r2, r1);
            AddTri(triangles, baseIdx, r3, r2);
            AddTri(triangles, baseIdx, r0, r3);

            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            meshFilter.sharedMesh = mesh;

            // Material: Shader fallback
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Unlit/Color");
            if (shader == null) shader = Shader.Find("Standard");
            if (shader != null)
            {
                Material mat = new Material(shader) { name = "M_ShadowEntity" };
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", bodyColor);
                else if (mat.HasProperty("_Color")) mat.SetColor("_Color", bodyColor);
                meshRenderer.sharedMaterial = mat;
            }

            // 2. Add glowing pinprick eyes
            CreateEye(new Vector3(-0.045f, creatureHeight * 0.88f, 0.09f));
            CreateEye(new Vector3(0.045f, creatureHeight * 0.88f, 0.09f));
        }

        private void CreateEye(Vector3 localPos)
        {
            GameObject eye = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            eye.name = "GlowingEye";
            eye.transform.SetParent(transform, false);
            eye.transform.localPosition = localPos;
            eye.transform.localScale = Vector3.one * 0.022f;

            var col = eye.GetComponent<Collider>();
            if (col != null)
            {
                if (Application.isPlaying) Destroy(col);
                else DestroyImmediate(col);
            }

            var ren = eye.GetComponent<Renderer>();
            Shader unlit = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
            if (unlit == null) unlit = Shader.Find("Standard");
            if (unlit != null)
            {
                Material eyeMat = new Material(unlit);
                if (eyeMat.HasProperty("_BaseColor")) eyeMat.SetColor("_BaseColor", eyeColor);
                else if (eyeMat.HasProperty("_Color")) eyeMat.SetColor("_Color", eyeColor);
                ren.sharedMaterial = eyeMat;
            }
        }

        private static void AddTri(List<int> tris, int a, int b, int c)
        {
            tris.Add(a);
            tris.Add(b);
            tris.Add(c);
        }
    }
}
