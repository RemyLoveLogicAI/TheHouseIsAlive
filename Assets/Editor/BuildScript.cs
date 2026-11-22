using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEditor.XR.OpenXR.Features;
using UnityEngine.XR.Management;
using UnityEngine.XR.OpenXR;
using TheHouseIsAlive;

namespace TheHouseIsAlive.Editor
{
    /// <summary>
    /// Automated batch build and scene generation pipeline for Meta Quest 3.
    /// Configures Android ARM64, IL2CPP, Vulkan, and MR passthrough scene architecture.
    /// </summary>
    public static class BuildScript
    {
        private const string ScenePath = "Assets/Scenes/MainMRScene.unity";
        private const string ApkPath = "Builds/TheHouseIsAlive.apk";
        private const string BundleId = "com.LoveLogicAI.TheHouseIsAlive";

        private const string XRSettingsPath = "Assets/XR/XRGeneralSettingsPerBuildTarget.asset";
        private const string OpenXRLoaderType = "UnityEngine.XR.OpenXR.OpenXRLoader";
        private static readonly string[] RequiredOpenXRFeatures =
        {
            "MetaQuestFeature", "MetaXRFeature", "OculusTouchControllerProfile"
        };

        /// <summary>
        /// Ensures Android uses the OpenXR loader with Meta Quest support enabled.
        /// Without this the APK launches as a flat 2D Android app.
        /// </summary>
        [MenuItem("TheHouseIsAlive/Configure XR for Quest")]
        public static void ConfigureXR()
        {
            const BuildTargetGroup android = BuildTargetGroup.Android;

            EditorBuildSettings.TryGetConfigObject(XRGeneralSettings.k_SettingsKey,
                out XRGeneralSettingsPerBuildTarget perTarget);
            if (perTarget == null)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(XRSettingsPath));
                perTarget = ScriptableObject.CreateInstance<XRGeneralSettingsPerBuildTarget>();
                AssetDatabase.CreateAsset(perTarget, XRSettingsPath);
                EditorBuildSettings.AddConfigObject(XRGeneralSettings.k_SettingsKey, perTarget, true);
            }

            if (!perTarget.HasSettingsForBuildTarget(android)) perTarget.CreateDefaultSettingsForBuildTarget(android);
            if (!perTarget.HasManagerSettingsForBuildTarget(android)) perTarget.CreateDefaultManagerSettingsForBuildTarget(android);

            XRGeneralSettings general = perTarget.SettingsForBuildTarget(android);
            general.InitManagerOnStart = true;
            bool assigned = XRPackageMetadataStore.AssignLoader(perTarget.ManagerSettingsForBuildTarget(android),
                OpenXRLoaderType, android);
            Debug.Log($"[BuildScript] OpenXR loader assigned to Android: {assigned}");

            FeatureHelpers.RefreshFeatures(android);
            OpenXRSettings openxr = OpenXRSettings.GetSettingsForBuildTargetGroup(android);
            if (openxr == null)
            {
                Debug.LogError("[BuildScript] OpenXR settings for Android not found; XR not configured.");
                return;
            }

            foreach (var feature in openxr.GetFeatures())
            {
                if (feature == null) continue;
                if (RequiredOpenXRFeatures.Contains(feature.GetType().Name) && !feature.enabled)
                {
                    feature.enabled = true;
                    EditorUtility.SetDirty(feature);
                }
            }

            string enabled = string.Join(", ", openxr.GetFeatures().Where(f => f != null && f.enabled)
                .Select(f => f.GetType().Name));
            Debug.Log($"[BuildScript] OpenXR Android features enabled: {enabled}");

            // Configure Meta Quest Project Config (Passthrough, Scene, Anchors) and generate AndroidManifest
            try
            {
                var ovrConfig = OVRProjectConfig.CachedProjectConfig;
                if (ovrConfig != null)
                {
                    ovrConfig.insightPassthroughSupport = OVRProjectConfig.FeatureSupport.Required;
                    ovrConfig.sceneSupport = OVRProjectConfig.FeatureSupport.Required;
                    ovrConfig.anchorSupport = OVRProjectConfig.AnchorSupport.Enabled;
                    OVRProjectConfig.CommitProjectConfig(ovrConfig);
                    Debug.Log("[BuildScript] OVRProjectConfig configured: Passthrough=Required, Scene=Required, Anchor=Enabled.");

                    OVRManifestPreprocessor.GenerateOrUpdateAndroidManifest(true);
                    Debug.Log("[BuildScript] AndroidManifest.xml generated/updated with Scene & Passthrough declarations.");
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[BuildScript] OVRProjectConfig/Manifest setup warning: {ex.Message}");
            }

            EditorUtility.SetDirty(perTarget);
            EditorUtility.SetDirty(general);
            AssetDatabase.SaveAssets();
        }

        [MenuItem("TheHouseIsAlive/Generate Main Scene")]
        public static void GenerateMainScene()
        {
            Debug.Log("[BuildScript] Generating Main MR Scene...");
            ConfigureXR();

            string sceneDir = Path.GetDirectoryName(ScenePath);
            if (!string.IsNullOrEmpty(sceneDir) && !Directory.Exists(sceneDir))
            {
                Directory.CreateDirectory(sceneDir);
            }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 1. Camera Rig & Passthrough Camera (Solid black background for MR Passthrough)
            // Meta's runtime types ship in the Oculus.VR assembly (asmdef Oculus.VR), not in an
            // assembly named Meta.XR.SDK.Core, so a guessed assembly name resolves to null and
            // the rig silently disappears from the scene. Resolve, then log which assembly won.
            GameObject cameraRig = null;
            Type ovrRigType = ResolveType("OVRCameraRig", "Oculus.VR", "Meta.XR.SDK.Core");

            if (ovrRigType != null)
            {
                cameraRig = new GameObject("OVRCameraRig", ovrRigType);
                Debug.Log($"[BuildScript] OVRCameraRig resolved from {ovrRigType.Assembly.GetName().Name}.");

                Type ovrManagerType = ResolveType("OVRManager", "Oculus.VR", "Meta.XR.SDK.Core");
                if (ovrManagerType != null)
                {
                    var ovrManager = cameraRig.AddComponent(ovrManagerType);
                    var managerSo = new SerializedObject(ovrManager);
                    var passthroughProperty = managerSo.FindProperty("isInsightPassthroughEnabled");
                    if (passthroughProperty != null)
                    {
                        passthroughProperty.boolValue = true;
                        managerSo.ApplyModifiedPropertiesWithoutUndo();
                        Debug.Log($"[BuildScript] OVRManager added from {ovrManagerType.Assembly.GetName().Name}; isInsightPassthroughEnabled = true.");
                    }
                    else
                    {
                        Debug.LogWarning("[BuildScript] OVRManager added but isInsightPassthroughEnabled was not found.");
                    }
                }
                else
                {
                    Debug.LogError("[BuildScript] OVRManager type not found; the scene cannot enable passthrough.");
                }

                Type passthroughLayerType = ResolveType("OVRPassthroughLayer", "Oculus.VR", "Meta.XR.SDK.Core");
                if (passthroughLayerType != null)
                {
                    cameraRig.AddComponent(passthroughLayerType);
                    Debug.Log($"[BuildScript] OVRPassthroughLayer added to cameraRig from {passthroughLayerType.Assembly.GetName().Name}.");
                }
            }
            else
            {
                Debug.LogError("[BuildScript] OVRCameraRig type not found; the scene has no XR camera rig.");
            }

            GameObject mainCamGo = new GameObject("Main Camera");
            mainCamGo.tag = "MainCamera";
            if (cameraRig != null)
            {
                mainCamGo.transform.SetParent(cameraRig.transform, false);
            }

            Camera mainCam = mainCamGo.AddComponent<Camera>();
            mainCam.clearFlags = CameraClearFlags.SolidColor;
            mainCam.backgroundColor = Color.black; // Required for Quest MR Passthrough
            mainCam.nearClipPlane = 0.1f;
            mainCam.farClipPlane = 50f;
            mainCamGo.AddComponent<AudioListener>();

            // 2. Directional Light
            GameObject dirLightGo = new GameObject("Directional Light");
            Light dirLight = dirLightGo.AddComponent<Light>();
            dirLight.type = LightType.Directional;
            dirLight.intensity = 0.8f;
            dirLight.color = new Color(0.95f, 0.95f, 1.0f);
            dirLightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            // 3. Core Horror & Gameplay Components
            GameObject directorGo = new GameObject("RoomScareDirector");
            RoomScareDirector director = directorGo.AddComponent<RoomScareDirector>();

            GameObject knockGo = new GameObject("SpatialKnockSequencer");
            SpatialKnockSequencer knock = knockGo.AddComponent<SpatialKnockSequencer>();

            GameObject flickerGo = new GameObject("FlickerLightingEffect");
            FlickerLightingEffect flicker = flickerGo.AddComponent<FlickerLightingEffect>();

            GameObject trackerGo = new GameObject("DontLookAwayTracker");
            DontLookAwayTracker tracker = trackerGo.AddComponent<DontLookAwayTracker>();

            GameObject shadowGo = new GameObject("ShadowEntityController");
            ShadowEntityController shadow = shadowGo.AddComponent<ShadowEntityController>();

            // Wire Serialized Properties
            SerializedObject directorSo = new SerializedObject(director);
            SerializedProperty prop;
            if ((prop = directorSo.FindProperty("knockSequencer")) != null) prop.objectReferenceValue = knock;
            if ((prop = directorSo.FindProperty("lightFlicker")) != null) prop.objectReferenceValue = flicker;
            if ((prop = directorSo.FindProperty("shadowEntity")) != null) prop.objectReferenceValue = shadow;
            if ((prop = directorSo.FindProperty("gazeTracker")) != null) prop.objectReferenceValue = tracker;
            directorSo.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject flickerSo = new SerializedObject(flicker);
            if ((prop = flickerSo.FindProperty("virtualRoomLight")) != null) prop.objectReferenceValue = dirLight;
            flickerSo.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject trackerSo = new SerializedObject(tracker);
            if ((prop = trackerSo.FindProperty("headTransform")) != null) prop.objectReferenceValue = mainCamGo.transform;
            trackerSo.ApplyModifiedPropertiesWithoutUndo();

            // Setup Shadow Entity visual representation
            GameObject creatureMeshGo = ProceduralCreatureMesh.CreateCreatureGameObject("ShadowMeshVisual");
            creatureMeshGo.transform.SetParent(shadowGo.transform, false);
            Renderer meshRenderer = creatureMeshGo.GetComponent<Renderer>();

            SerializedObject shadowSo = new SerializedObject(shadow);
            if ((prop = shadowSo.FindProperty("entityMeshRenderer")) != null) prop.objectReferenceValue = meshRenderer;
            if ((prop = shadowSo.FindProperty("peekPivot")) != null) prop.objectReferenceValue = shadowGo.transform;
            shadowSo.ApplyModifiedPropertiesWithoutUndo();

            // MRUK manager anchor for physical room scanning & physical room occlusion
            Type mrukType = ResolveType("Meta.XR.MRUtilityKit.MRUK", "meta.xr.mrutilitykit", "Meta.XR.MRUtilityKit");
            if (mrukType != null)
            {
                var mrukGo = new GameObject("MRUK", mrukType);
                Debug.Log("[BuildScript] MRUK manager added to scene.");

                // Add EffectMesh for automatic physical wall and furniture depth occlusion
                Type effectMeshType = ResolveType("Meta.XR.MRUtilityKit.EffectMesh", "meta.xr.mrutilitykit", "Meta.XR.MRUtilityKit");
                if (effectMeshType != null)
                {
                    var effectMeshGo = new GameObject("MRUK_RoomOcclusion", effectMeshType);
                    effectMeshGo.transform.SetParent(mrukGo.transform, false);

                    var effectMesh = effectMeshGo.GetComponent(effectMeshType);
                    var effectMeshSo = new SerializedObject(effectMesh);

                    Shader occShader = Shader.Find("Universal Render Pipeline/VR/SpatialMapping/Occlusion")
                                    ?? Shader.Find("Hidden/Universal Render Pipeline/XR/XROcclusionMesh");
                    if (occShader != null)
                    {
                        Material occMat = new Material(occShader) { name = "M_PhysicalRoomOcclusion" };
                        var matProp = effectMeshSo.FindProperty("MeshMaterial");
                        if (matProp != null) matProp.objectReferenceValue = occMat;
                        effectMeshSo.ApplyModifiedPropertiesWithoutUndo();
                        Debug.Log("[BuildScript] EffectMesh configured with SpatialMapping Occlusion material.");
                    }
                }
            }

            // Save Scene
            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log($"[BuildScript] Main MR Scene saved successfully at {ScenePath}");

            // Add Scene to EditorBuildSettings
            var scenes = EditorBuildSettings.scenes.ToList();
            int index = scenes.FindIndex(s => s.path == ScenePath);
            if (index >= 0)
            {
                scenes[index] = new EditorBuildSettingsScene(ScenePath, true);
            }
            else
            {
                scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
            }
            EditorBuildSettings.scenes = scenes.ToArray();
            Debug.Log($"[BuildScript] {ScenePath} registered in EditorBuildSettings.");
        }

        /// <summary>
        /// Resolves a MonoBehaviours type by scanning the given assembly names first, then every
        /// loaded assembly. Meta splits its runtime types across assemblies (Oculus.VR,
        /// Meta.XR.MRUtilityKit, Meta.XR.Audio), so a hard-coded name has to be verified.
        /// </summary>
        private static Type ResolveType(string typeName, params string[] assemblyNames)
        {
            foreach (string assemblyName in assemblyNames)
            {
                Type qualified = null;
                try { qualified = Type.GetType($"{typeName}, {assemblyName}"); }
                catch (Exception) { /* fall through to the next probe */ }

                if (qualified == null)
                {
                    try { qualified = Assembly.Load(assemblyName)?.GetType(typeName); }
                    catch (Exception) { /* assembly not loadable by simple name */ }
                }

                if (qualified != null) return qualified;
            }

            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    Type found = assembly.GetType(typeName);
                    if (found != null) return found;
                }
                catch (Exception) { /* dynamic or partially loaded assembly */ }
            }

            return null;
        }

        [MenuItem("TheHouseIsAlive/Build Quest 3 APK")]
        public static void BuildQuestAPK()
        {
            Debug.Log("[BuildScript] Initiating Meta Quest 3 Android APK Build...");

            // 1. Ensure Main Scene exists
            if (!File.Exists(ScenePath))
            {
                Debug.Log($"[BuildScript] Scene {ScenePath} missing. Generating now...");
                GenerateMainScene();
            }

            // 2. Configure PlayerSettings for Quest 3 (Android ARM64, IL2CPP, Vulkan, Target SDK 34)
            Debug.Log("[BuildScript] Configuring Quest 3 PlayerSettings...");
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
            ConfigureXR();

            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, BundleId);
            PlayerSettings.Android.targetSdkVersion = (AndroidSdkVersions)34;
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.Vulkan });
            // Meta's OpenXR utilities plugin hard-require linear color space; the build fails outright otherwise.
            if (PlayerSettings.colorSpace != ColorSpace.Linear)
            {
                PlayerSettings.colorSpace = ColorSpace.Linear;
            }

            // 3. Build Player
            string buildDir = Path.GetDirectoryName(ApkPath);
            if (!string.IsNullOrEmpty(buildDir) && !Directory.Exists(buildDir))
            {
                Directory.CreateDirectory(buildDir);
            }

            var buildOptions = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = ApkPath,
                target = BuildTarget.Android,
                options = BuildOptions.None
            };

            Debug.Log($"[BuildScript] Running BuildPipeline.BuildPlayer -> {ApkPath}");
            BuildReport report = BuildPipeline.BuildPlayer(buildOptions);
            BuildSummary summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"[BuildScript] Quest 3 build SUCCEEDED: {summary.totalSize} bytes in {summary.totalTime.TotalSeconds:F1}s at {ApkPath}");
            }
            else
            {
                string error = $"[BuildScript] Quest 3 build FAILED: Result={summary.result}, Errors={summary.totalErrors}, Warnings={summary.totalWarnings}";
                Debug.LogError(error);
                throw new Exception(error);
            }
        }
    }
}
