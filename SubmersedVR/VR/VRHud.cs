using System;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace SubmersedVR
{
    extern alias SteamVRActions;
    extern alias SteamVRRef;
    using SteamVRActions.Valve.VR;
    using System.Collections.Generic;
    using System.Reflection.Emit;

    // Tweaks regarding the HUD of the game
    static class VRHud
    {
        internal static Transform screenCanvas;
        private static Transform overlayCanvas;
        private static Transform hud;

        private static Canvas staticHudCanvas = null;
        private static Canvas vehicleHudCanvas = null;
        private static Canvas subtitleCanvas = null;
        private static bool curveRefreshSubscribed = false;
        private static int curveRefreshTick = 0;
        private const int DynamicCurveRefreshInterval = 30;
        // private static OffsetCalibrationTool calibrationTool;

        public static void HideOverlays()
        {
            uGUI.main.overlays.gameObject.SetActive(false);
            uGUI.main.hud.gameObject.SetActive(false);
        }
        // TODO: Hud Distance needs dedicated canvas, since the Pips seem to assume the 1 meter canvas distance.
#if false
        public static float hudDistance = 1.0f;
        public static float HudDistance {
            get {
                return hudDistance;
            }
            set {
                hudDistance = value;
                if (staticHudCanvas == null || screenCanvas == null) {
                    return;
                }
                hud.transform.localPosition = Vector3.forward * (hudDistance - 1.0f);
            }
        }
        public static void OnHudDistanceChanged(float value) {
            HudDistance = value;
        }
#endif

        public static void SetupHandReticle(bool onLaserPointer, Camera uiCamera, Transform rightControllerUI)
        {
            if (onLaserPointer)
            {
                SetupHandReticleLaserPointer(uiCamera, rightControllerUI);
            }
            else
            {
                SetupHandReticleOnHand(uiCamera, rightControllerUI);
            }
        }

        public static void SetupHandReticleOnHand(Camera uiCamera, Transform rightControllerUI)
        {
            // Steal Reticle and attach to the right hand
            var handReticle = HandReticle.main.gameObject.WithParent(rightControllerUI.transform);
            handReticle.GetOrAddComponent<Canvas>().worldCamera = uiCamera;
            handReticle.transform.localEulerAngles = new Vector3(90, 0, 0);
            handReticle.transform.localPosition = new Vector3(0, 0, 0.05f);
            handReticle.transform.localScale = new Vector3(0.001f, 0.001f, 0.001f);
        }

        public static void SetupHandReticleLaserPointer(Camera uiCamera, Transform rightControllerUI)
        {
            var handReticle = HandReticle.main.gameObject.WithParent(VRCameraRig.instance.laserPointerUI.pointerDot.transform);
            handReticle.transform.localRotation = Quaternion.Euler(40, 0, 0);
            handReticle.transform.localPosition = new Vector3(0, -5, VRCameraRig.instance.laserPointerUI.pointerDot.transform.localPosition.z);//new Vector3(0, 0, 0.05f);
            handReticle.transform.localScale = VRCameraRig.instance.laserPointerUI.pointerDot.transform.localScale * 2;//new Vector3(0.001f, 0.001f, 0.001f);
        }

        public static void OnHandReticleSettingChanged(bool onLaserPointer)
        {
            var rig = VRCameraRig.instance;
            if (!rig)
            {
                return;
            }
            SetupHandReticle(onLaserPointer, rig.uiCamera, rig.rightControllerUI.transform);
        }

        private static Vector3 FootHudPosition() =>
            new Vector3(0.0f, 0.1f + Settings.HudVerticalOffset, 1.0f + Settings.HudDistance);

        private static Vector3 VehicleHudPosition() =>
            new Vector3(0.0f, 0.1f + Settings.VehicleHudVerticalOffset, 1.0f + Settings.VehicleHudDistance);

        private static Vector3 SubtitlePosition() =>
            Settings.SubtitleSyncWithHud
                ? FootHudPosition()
                : new Vector3(0.0f, 0.1f + Settings.SubtitleVerticalOffset, 1.0f + Settings.SubtitleDistance);

        public static void OnHudVerticalOffsetChanged(float value)
        {
            if (staticHudCanvas == null) return;
            staticHudCanvas.transform.localPosition = FootHudPosition();
            if (Settings.SubtitleSyncWithHud) UpdateSubtitleTransform();
        }

        public static void OnHudScaleChanged(float value)
        {
            if (staticHudCanvas == null) return;
            staticHudCanvas.transform.localScale = Vector3.one * 0.00085f * value;
            RefreshFootCurve();
            if (Settings.SubtitleSyncWithHud) UpdateSubtitleTransform();
        }

        public static void OnHudDistanceChanged(float value)
        {
            if (staticHudCanvas == null) return;
            staticHudCanvas.transform.localPosition = FootHudPosition();
            if (Settings.SubtitleSyncWithHud) UpdateSubtitleTransform();
        }

        public static void OnVehicleHudVerticalOffsetChanged(float value)
        {
            if (vehicleHudCanvas == null) return;
            vehicleHudCanvas.transform.localPosition = VehicleHudPosition();
        }

        public static void OnVehicleHudScaleChanged(float value)
        {
            if (vehicleHudCanvas == null) return;
            vehicleHudCanvas.transform.localScale = Vector3.one * 0.00085f * value;
            RefreshVehicleCurve();
        }

        public static void OnVehicleHudDistanceChanged(float value)
        {
            if (vehicleHudCanvas == null) return;
            vehicleHudCanvas.transform.localPosition = VehicleHudPosition();
        }

        private static void ApplyCurve(Canvas canvas, bool curved, float worldRadius, float canvasBaseScale)
        {
            if (canvas == null) return;
            float rPx = curved ? worldRadius / canvasBaseScale : 0f;
            var curveGroups = new HashSet<Transform>();
            var popupGroups = new HashSet<Transform>();
            foreach (var g in canvas.GetComponentsInChildren<Graphic>(true))
            {
                if (HudCurveDebug.ShouldSkipCurve(g.transform, canvas.transform))
                {
                    DisableCurveEffects(g);
                    continue;
                }

                Transform curveGroup = HudCurveDebug.GetCurveTransformRoot(g.transform, canvas.transform);
                if (curveGroup != null && curveGroups.Add(curveGroup))
                {
                    var groupEffect = curveGroup.gameObject.GetOrAddComponent<HudCurveTransformEffect>();
                    groupEffect.targetCanvas = canvas;
                    if (groupEffect.radiusPixels != rPx)
                    {
                        groupEffect.radiusPixels = rPx;
                        groupEffect.ForceApply();
                    }
                }

                Transform popupRoot = HudCurveDebug.GetPopupNotificationRoot(g.transform, canvas.transform);
                if (popupRoot != null && popupGroups.Add(popupRoot))
                {
                    var popupEffect = popupRoot.gameObject.GetOrAddComponent<HudPopupCurveTransformEffect>();
                    popupEffect.targetCanvas = canvas;
                    if (popupEffect.radiusPixels != rPx)
                    {
                        popupEffect.radiusPixels = rPx;
                        popupEffect.ForceApply();
                    }
                }

                if (g is TMPro.TextMeshProUGUI tmpText)
                {
                    var effect = g.gameObject.GetOrAddComponent<TmpHudCurveEffect>();
                    effect.targetCanvas = canvas;
                    if (effect.radiusPixels != rPx)
                    {
                        effect.radiusPixels = rPx;
                        effect.ForceApply("radiusChanged");
                    }
                }
                else
                {
                    var effect = g.gameObject.GetOrAddComponent<HudCurveEffect>();
                    effect.targetCanvas = canvas;
                    if (effect.radiusPixels != rPx)
                    {
                        effect.radiusPixels = rPx;
                        g.SetVerticesDirty();
                    }
                }
            }
        }

        private static void DisableCurveEffects(Graphic g)
        {
            var graphicEffect = g.GetComponent<HudCurveEffect>();
            if (graphicEffect != null && graphicEffect.radiusPixels != 0f)
            {
                graphicEffect.radiusPixels = 0f;
                g.SetVerticesDirty();
            }

            if (g is TMPro.TextMeshProUGUI tmpText)
            {
                var tmpEffect = tmpText.GetComponent<TmpHudCurveEffect>();
                if (tmpEffect != null && tmpEffect.radiusPixels != 0f)
                {
                    tmpEffect.radiusPixels = 0f;
                    tmpEffect.ForceApply("skipCurve");
                }
            }
        }

        private static void RefreshFootCurve() =>
            ApplyCurve(staticHudCanvas, Settings.HudCurved, Settings.HudCurveRadius, 0.00085f * Settings.HudScale);

        private static void RefreshVehicleCurve() =>
            ApplyCurve(vehicleHudCanvas, Settings.VehicleHudCurved, Settings.VehicleHudCurveRadius, 0.00085f * Settings.VehicleHudScale);

        private static void RefreshSubtitleCurve()
        {
            float scale = Settings.SubtitleSyncWithHud ? Settings.HudScale : Settings.SubtitleScale;
            ApplyCurve(subtitleCanvas, Settings.HudCurved, Settings.HudCurveRadius, 0.00085f * scale);
        }

        private static Transform GetFootHudParent()
        {
            var rig = VRCameraRig.instance;
            if (rig == null) return null;
            if (Settings.HudFollowHead)
            {
                // mainCamera tracks the HMD pose; uiCamera tracks body direction only
                return SNCameraRoot.main?.mainCamera?.transform ?? rig.uiCamera.transform;
            }
            return rig.uiRig.transform;
        }

        private static Camera GetFootHudCamera()
        {
            if (Settings.HudFollowHead)
                return SNCameraRoot.main?.mainCamera ?? VRCameraRig.instance.uiCamera;
            return VRCameraRig.instance.uiCamera;
        }

        public static void OnHudFollowHeadChanged(bool followHead)
        {
            var parent = GetFootHudParent();
            if (staticHudCanvas == null || parent == null) return;
            staticHudCanvas.transform.SetParent(parent, false);
            staticHudCanvas.worldCamera = GetFootHudCamera();
            staticHudCanvas.transform.localPosition = FootHudPosition();
            staticHudCanvas.transform.localRotation = Quaternion.identity;
        }

        public static void OnHudCurvedChanged(bool curved)
        {
            RefreshFootCurve();
            RefreshSubtitleCurve();
        }

        public static void OnHudCurveRadiusChanged(float r)
        {
            RefreshFootCurve();
            RefreshSubtitleCurve();
        }

        public static void OnVehicleHudCurvedChanged(bool curved) => RefreshVehicleCurve();
        public static void OnVehicleHudCurveRadiusChanged(float r) => RefreshVehicleCurve();

        private static void EnsureDynamicCurveRefresh()
        {
            if (curveRefreshSubscribed) return;
            Canvas.willRenderCanvases += OnWillRenderCurveRefresh;
            curveRefreshSubscribed = true;
        }

        private static void OnWillRenderCurveRefresh()
        {
            curveRefreshTick++;
            if (curveRefreshTick % DynamicCurveRefreshInterval != 0) return;

            if (Settings.HudCurved && staticHudCanvas != null)
                RefreshFootCurve();
            if (Settings.HudCurved && subtitleCanvas != null)
                RefreshSubtitleCurve();
            if (Settings.VehicleHudCurved && vehicleHudCanvas != null)
                RefreshVehicleCurve();
        }

        private static void UpdateSubtitleTransform()
        {
            if (subtitleCanvas == null) return;
            subtitleCanvas.transform.localPosition = SubtitlePosition();
            float scale = Settings.SubtitleSyncWithHud ? Settings.HudScale : Settings.SubtitleScale;
            subtitleCanvas.transform.localScale = Vector3.one * 0.00085f * scale;
            RefreshSubtitleCurve();
        }

        public static void OnSubtitleSyncChanged(bool sync) => UpdateSubtitleTransform();
        public static void OnSubtitleVerticalOffsetChanged(float value) { if (!Settings.SubtitleSyncWithHud) UpdateSubtitleTransform(); }
        public static void OnSubtitleScaleChanged(float value) { if (!Settings.SubtitleSyncWithHud) UpdateSubtitleTransform(); }
        public static void OnSubtitleDistanceChanged(float value) { if (!Settings.SubtitleSyncWithHud) UpdateSubtitleTransform(); }

        private static void MoveDialogueElementsToSubtitleCanvas()
        {
            if (screenCanvas == null || subtitleCanvas == null) return;

            Mod.logger.LogInfo("[VRHud] screenCanvas top-level children (before move):");
            foreach (Transform child in screenCanvas)
            {
                Mod.logger.LogInfo($"  '{child.name}' active={child.gameObject.activeSelf}");
                foreach (Transform grandchild in child)
                    Mod.logger.LogInfo($"    └─ '{grandchild.name}' active={grandchild.gameObject.activeSelf}");
            }

            var toMove = new List<Transform>();
            foreach (Transform child in screenCanvas)
            {
                var lower = child.name.ToLower();
                if (lower.Contains("subtitle") || lower.Contains("caption") ||
                    lower.Contains("dialogue") || lower.Contains("speaker") ||
                    lower.Contains("portrait") || lower.Contains("story") ||
                    lower.Contains("talking"))  // TalkingHead = 대화 캐릭터 초상화/이름
                {
                    toMove.Add(child);
                }
            }

            if (toMove.Count == 0)
            {
                Mod.logger.LogInfo("[VRHud] No dialogue elements found to move.");
            }
            else
            {
                foreach (var t in toMove)
                {
                    Mod.logger.LogInfo($"[VRHud] Moving to subtitleCanvas: '{t.name}'");
                    t.SetParent(subtitleCanvas.transform, false);
                }
            }

            RefreshSubtitleCurve();
        }

        public static Canvas CreateWorldCanvas(this GameObject go)
        {
            Canvas canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            go.layer = LayerID.UI;
            return canvas;
        }

        public static void Setup(Camera uiCamera, Transform rightControllerUI)
        {
            Mod.logger.LogDebug($"Setting up HUD for {uiCamera.name}");

            screenCanvas = uGUI.main.screenCanvas.gameObject.transform;
            overlayCanvas = uGUI.main.overlays.gameObject.transform.parent;
            hud = uGUI.main.hud.transform;

            if (staticHudCanvas == null)
            {
                var parent = GetFootHudParent() ?? VRCameraRig.instance.uiRig.transform;
                var go = new GameObject("StaticHUDCanvas").WithParent(parent);
                staticHudCanvas = go.CreateWorldCanvas();
                var rt = go.GetComponent<RectTransform>();
                go.transform.localScale = screenCanvas.localScale;
                rt.sizeDelta = screenCanvas.GetComponent<RectTransform>().sizeDelta;
                rt.anchoredPosition = screenCanvas.GetComponent<RectTransform>().anchoredPosition;
                go.transform.localPosition = FootHudPosition();
                go.transform.localRotation = Quaternion.identity;
            }
            else
            {
                var parent = GetFootHudParent() ?? VRCameraRig.instance.uiRig.transform;
                staticHudCanvas.transform.SetParent(parent, false);
                staticHudCanvas.transform.localPosition = FootHudPosition();
                staticHudCanvas.transform.localRotation = Quaternion.identity;
            }
            staticHudCanvas.worldCamera = GetFootHudCamera();

            if (vehicleHudCanvas == null)
            {
                var uiRig = VRCameraRig.instance.uiRig.transform;
                var go = new GameObject("VehicleHUDCanvas").WithParent(uiRig);
                vehicleHudCanvas = go.CreateWorldCanvas();
                var rt = go.GetComponent<RectTransform>();
                go.transform.localScale = screenCanvas.localScale;
                rt.sizeDelta = screenCanvas.GetComponent<RectTransform>().sizeDelta;
                rt.anchoredPosition = screenCanvas.GetComponent<RectTransform>().anchoredPosition;
                go.transform.localPosition = VehicleHudPosition();
                go.transform.localRotation = Quaternion.identity;
            }
            vehicleHudCanvas.worldCamera = uiCamera;

            if (subtitleCanvas == null)
            {
                var uiRig = VRCameraRig.instance.uiRig.transform;
                var go = new GameObject("SubtitleCanvas").WithParent(uiRig);
                subtitleCanvas = go.CreateWorldCanvas();
                var rt = go.GetComponent<RectTransform>();
                rt.sizeDelta = screenCanvas.GetComponent<RectTransform>().sizeDelta;
                rt.anchoredPosition = screenCanvas.GetComponent<RectTransform>().anchoredPosition;
                go.transform.localRotation = Quaternion.identity;
            }
            subtitleCanvas.worldCamera = uiCamera;

            MoveDialogueElementsToSubtitleCanvas();

            screenCanvas.SetParent(uiCamera.transform, true);
            overlayCanvas.SetParent(uiCamera.transform, true);
            hud.SetParent(staticHudCanvas.transform, false);

            //Makes the UI more comfortable to view
            screenCanvas.transform.localScale = new Vector3(0.00072f, 0.00072f, 0.00072f);
            overlayCanvas.transform.localScale = new Vector3(0.00032f, 0.00032f, 0.00032f);
            staticHudCanvas.transform.localScale = Vector3.one * 0.00085f * Settings.HudScale;
            vehicleHudCanvas.transform.localScale = Vector3.one * 0.00085f * Settings.VehicleHudScale;
            UpdateSubtitleTransform();

            RefreshFootCurve();
            RefreshVehicleCurve();
            RefreshSubtitleCurve();
            EnsureDynamicCurveRefresh();

            SetupHandReticle(Settings.PutHandReticleOnLaserPointer, uiCamera, rightControllerUI);
            Settings.PutHandReticleOnLaserPointerChanged -= OnHandReticleSettingChanged;
            Settings.PutHandReticleOnLaserPointerChanged += OnHandReticleSettingChanged;

            Settings.HudVerticalOffsetChanged -= OnHudVerticalOffsetChanged;
            Settings.HudVerticalOffsetChanged += OnHudVerticalOffsetChanged;
            Settings.HudScaleChanged -= OnHudScaleChanged;
            Settings.HudScaleChanged += OnHudScaleChanged;
            Settings.HudDistanceChanged -= OnHudDistanceChanged;
            Settings.HudDistanceChanged += OnHudDistanceChanged;
            Settings.VehicleHudVerticalOffsetChanged -= OnVehicleHudVerticalOffsetChanged;
            Settings.VehicleHudVerticalOffsetChanged += OnVehicleHudVerticalOffsetChanged;
            Settings.VehicleHudScaleChanged -= OnVehicleHudScaleChanged;
            Settings.VehicleHudScaleChanged += OnVehicleHudScaleChanged;
            Settings.VehicleHudDistanceChanged -= OnVehicleHudDistanceChanged;
            Settings.VehicleHudDistanceChanged += OnVehicleHudDistanceChanged;
            Settings.HudFollowHeadChanged -= OnHudFollowHeadChanged;
            Settings.HudFollowHeadChanged += OnHudFollowHeadChanged;
            Settings.HudCurvedChanged -= OnHudCurvedChanged;
            Settings.HudCurvedChanged += OnHudCurvedChanged;
            Settings.HudCurveRadiusChanged -= OnHudCurveRadiusChanged;
            Settings.HudCurveRadiusChanged += OnHudCurveRadiusChanged;
            Settings.VehicleHudCurvedChanged -= OnVehicleHudCurvedChanged;
            Settings.VehicleHudCurvedChanged += OnVehicleHudCurvedChanged;
            Settings.VehicleHudCurveRadiusChanged -= OnVehicleHudCurveRadiusChanged;
            Settings.VehicleHudCurveRadiusChanged += OnVehicleHudCurveRadiusChanged;

            Settings.SubtitleSyncWithHudChanged -= OnSubtitleSyncChanged;
            Settings.SubtitleSyncWithHudChanged += OnSubtitleSyncChanged;
            Settings.SubtitleVerticalOffsetChanged -= OnSubtitleVerticalOffsetChanged;
            Settings.SubtitleVerticalOffsetChanged += OnSubtitleVerticalOffsetChanged;
            Settings.SubtitleScaleChanged -= OnSubtitleScaleChanged;
            Settings.SubtitleScaleChanged += OnSubtitleScaleChanged;
            Settings.SubtitleDistanceChanged -= OnSubtitleDistanceChanged;
            Settings.SubtitleDistanceChanged += OnSubtitleDistanceChanged;

            WristHud.Setup();

            screenCanvas.GetComponent<uGUI_CanvasScaler>()?.SetDirty();
            screenCanvas.GetComponentsInChildren<uGUI_CanvasScaler>().ForEach(cs => cs.SetDirty());
            MiscSettings.cameraBobbing = VROptions.enableCinematics; 
        }

        public static void OnEnterVehicle()
        {
            var player = Player.main;
            if (player != null)
            {
                hud.SetParent(vehicleHudCanvas.transform, false);
                RefreshVehicleCurve();
            }
        }

        public static void OnExitVehicle()
        {
            hud.SetParent(staticHudCanvas.transform, false);
            RefreshFootCurve();
        }
    }

    static class WristHud
    {
        //private static TransformOffset wristOffset = new TransformOffset(new Vector3(-0.079f, 0.148f, -0.158f), new Vector3(350.494f, 88.400f, 244.161f));
        //This works for Valve Index
        private static TransformOffset wristOffset = new TransformOffset(new Vector3(-0.016f, 0.123f, -0.128f), new Vector3(15.494f, 74.4f, 245.161f));
        //private static TransformOffset wristOffset = new TransformOffset(new Vector3(-0.044f, 0.16f, -0.158f), new Vector3(15.494f, 88.400f, 244.161f));
        private static GameObject wristTarget;
        private static Canvas canvas;
        private static CanvasGroup canvasGroup;

        // Cached Values
        private static Transform hudContent;
        private static Transform uiCamera;
        private static Transform cachedIndexTip;
        private static FMODAsset turnOnSound;
        private static FMODAsset turnOffSound;

        // State
        public static bool isHudOn = true;
        private static bool touchingWrist = false;
        private static bool prevTouchingWrist = false;

        //public static GameObject pointerDot;
        public static TextMeshProUGUI entry;

        public static string AdjustHUD(float pX, float pY, float pZ, float aX, float aY, float aZ)
        {
            WristHud.wristOffset = new TransformOffset(new Vector3(WristHud.wristOffset.Pos.x + (pX/1000), WristHud.wristOffset.Pos.y + (pY/1000), WristHud.wristOffset.Pos.z + (pZ/1000)), new Vector3(WristHud.wristOffset.Angles.x + aX, WristHud.wristOffset.Angles.y + aY, WristHud.wristOffset.Angles.z + aZ));
            WristHud.wristOffset.Apply(WristHud.wristTarget.transform);
            return $"AdjustHUD\npX={WristHud.wristOffset.Pos.x.ToString("0.000")}\npY={WristHud.wristOffset.Pos.y.ToString("0.000")}\npZ={WristHud.wristOffset.Pos.z.ToString("0.000")}\naX={WristHud.wristOffset.Angles.x}\naY={WristHud.wristOffset.Angles.y}\naZ={WristHud.wristOffset.Angles.z}";
        }
        public static FMODAsset CreateFMODAsset(string eventPath)
        {
            FMODAsset asset = ScriptableObject.CreateInstance<FMODAsset>();
            asset.path = eventPath;
            return asset;
        }

        // Create Wrist World Canvas
        public static void Setup()
        {
            var rig = VRCameraRig.instance;
            uiCamera = rig.uiCamera.transform;
            hudContent = uGUI.main.hud.transform.GetChild(0);

            if (wristTarget == null)
            {
                wristTarget = new GameObject("WristTarget").WithParent(rig.leftControllerUI).ResetTransform();
                var wristCanvasGo = new GameObject("WristCanvas").WithParent(wristTarget).ResetTransform();
                canvas = wristCanvasGo.CreateWorldCanvas();
                canvasGroup = wristCanvasGo.AddComponent<CanvasGroup>();
                wristCanvasGo.transform.localScale = new Vector3(0.0004f, 0.0004f, 0.0004f);
                wristOffset.Apply(wristTarget.transform);

                GameObject obj = UnityEngine.Object.Instantiate(ErrorMessage.main.prefabMessage);
                entry = obj.GetComponent<TextMeshProUGUI>();
                entry.rectTransform.SetParent(wristCanvasGo.transform, false);
                //obj.SetActive(true);
                obj.layer = LayerMask.NameToLayer("UI");
                HideForScreenshots h = obj.AddComponent<HideForScreenshots>();
                h.type = HideForScreenshots.HideType.HUD;
                entry.horizontalAlignment = TMPro.HorizontalAlignmentOptions.Center;
                entry.enabled = false;
                entry.transform.localPosition = new Vector3(0f, 0.1f, 0f);
                entry.transform.localScale = new Vector3(0.66f, 1f, 1f);
                UpdateWristText();

/*
                Material newMaterial = new Material(ShaderManager.preloadedShaders.DebugDisplaySolid);
                newMaterial.SetColor(ShaderPropertyID._Color, Color.cyan);

                // Setup PointerDot at the end
                pointerDot = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                pointerDot.transform.localScale = new Vector3(0.03f, 0.03f, 0.03f);
                pointerDot.transform.SetParent(wristCanvasGo.transform, false);
                //pointerDot.transform.parent = wristCanvasGo.transform;
                LaserPointer.Destroy(pointerDot.GetComponent<SphereCollider>());
                pointerDot.GetComponent<Renderer>().material = newMaterial;
                pointerDot.SetActive(true);
                //wristDotOffset.Apply(pointerDot.transform);
*/

            //laserPointer = new GameObject(nameof(laserPointer)).WithParent(wristTarget).AddComponent<LaserPointer>();
            //laserPointer.gameObject.SetActive(true);

                //var cube = GameObject.CreatePrimitive(PrimitiveType.Cube).WithParent(wristTarget).ResetTransform();
                //cube.GetComponent<MeshRenderer>().sharedMaterial.color = new Color(0,1,1,1);
/*
                var primitive = GameObject.CreatePrimitive(PrimitiveType.Cube).WithParent(wristTarget).ResetTransform();
                //primitive.position = pos;
                primitive.transform.localScale = Vector3.one * .01f;
                primitive.name = "HI";
                primitive.SetActive(true);
            
                var renderer = primitive.GetComponent<MeshRenderer>();
                renderer.material.SetColor("_Color", Color.red);
*/
            }

            Settings.PutBarsOnWristChanged -= OnPutBarsOnHandChanged;
            Settings.PutBarsOnWristChanged += OnPutBarsOnHandChanged;
            Toggle(Settings.PutBarsOnWrist);

            turnOnSound = CreateFMODAsset("event:/tools/flashlight/turn_on");
            turnOffSound = CreateFMODAsset("event:/tools/flashlight/turn_off");
        }

        static void LogDescendants(Transform transform, int level) 
        {  
            foreach(Transform child in transform) {
                Mod.logger.LogInfo($"{new String('-', level)}{child.name}");
                LogDescendants(child, level + 1);   
            }
        }

        public static void UpdateWristText()
        {
            entry.text = isHudOn ? "HUD ON" : "HUD OFF";
        }
        public static Transform GetIndexFingerTip()
        {
            if (cachedIndexTip != null)
            {
                return cachedIndexTip;
            }
            var animator = Player.main?.playerAnimator;
            //animator.enabled = false;
            if (animator is Animator anim)
            {
                // TODO: Test this
                //LogDescendants(anim.transform, 1) ;
                //IK disabled
                var tip = anim.transform.Find("export_skeleton/head_rig/neck/chest/clav_R/clav_R_aim/shoulder_R/hand_R/hand_R_point_base/hand_R_point_mid/hand_R_point_tip_rig");
                if(tip == null)
                {
                    //IK enabled
                    tip = anim.transform.Find("export_skeleton/head_rig/neck/chest/clav_R/clav_R_aim/shoulder_R/elbow_R/hand_R/hand_R_point_base/hand_R_point_mid/hand_R_point_tip_rig");
                }
                if (tip != null)
                {
                    cachedIndexTip = tip;
                    return tip;
                }
            }
            return null;
        }

        public static void OnPutBarsOnHandChanged(bool isOn)
        {
            Toggle(isOn);
        }


        public static void OnUpdate()
        { 
            if (!uGUI.isMainLevel)
            {
                return;
            }
            var camPos = uiCamera.transform.position;
            var worldRigPos = VRCameraRig.instance.rigParentTarget.position;
            var wristPos = wristTarget.transform.position + wristTarget.transform.right * 0.05f + wristTarget.transform.up * 0.05f ; //centered on the display rather than the corner

            Vector3 wristDir = wristTarget.transform.TransformDirection(Vector3.forward);
            Vector3 toCam = (wristPos - camPos).normalized;

            float wristCamDot = Vector3.Dot(wristDir, toCam);
            bool isFacingCamera = wristCamDot > 0.1f;
            //DebugPanel.Show($"dot = {wristCamDot} <= {wristDir}, {toCam}, {camPos}");
            canvasGroup.alpha = Mathf.Max(wristCamDot, 0.0f);

            if (isFacingCamera && GetIndexFingerTip() is Transform indexTip)
            {
                entry.enabled = true;
                var uiIndexPos = indexTip.position - worldRigPos;
                var wristDistance = Vector3.Distance(uiIndexPos, wristPos);
                //DebugPanel.Show($"wristDistance = {wristDistance} <= uiPos{uiIndexPos}, {wristPos} uiIndexPos = {indexTip.transform.position} {indexTip.localPosition} {worldRigPos}", true);
                const float threshold = 0.05f;
                touchingWrist = wristDistance < threshold;
                if (touchingWrist && !prevTouchingWrist)
                {
                    isHudOn = !isHudOn;
                    UpdateWristText();
                    Utils.PlayFMODAsset(isHudOn ? turnOnSound : turnOffSound);
                    HapticsVR.PlayHaptics(0.0f, 0.1f, 10f, 0.8f, false, true, false);   
                }
                prevTouchingWrist = touchingWrist;
            }
        }

        public static void Toggle(bool isOn)
        {
            if (canvas == null)
            {
                Setup();
            }

            var barsPanel = uGUI.main.barsPanel;
            if (isOn)
            {
                // Move to wrist
                Mod.logger.LogDebug("Turning WristHud on");
                barsPanel.WithParent(canvas.transform).ResetTransform();
                barsPanel.GetComponent<RectTransform>().pivot = new Vector2(0, 0);
                //Switched this from LateUpdateLast to CanvasFirst because the VRHands calculations occur in LateUpdate
                //and were happening after the WristHud OnUpdate so finger position was being read as the default animated finger
                //position, not the user overridden VRHands finger position. CanvasFirst would generally be too late for rendering updates,
                //but since it's just a toggle, it should be fine
                ManagedUpdate.Subscribe(ManagedUpdate.Queue.CanvasFirst, new ManagedUpdate.OnUpdate(OnUpdate));
            }
            else
            {
                // Move back
                Mod.logger.LogDebug("Turning WristHud off");
                barsPanel.transform.SetParent(hudContent.transform, false);
                barsPanel.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 0.5f);
                barsPanel.GetComponent<RectTransform>().anchoredPosition = new Vector2(0.0f, 0.0f);
                ManagedUpdate.Unsubscribe(ManagedUpdate.Queue.CanvasFirst, new ManagedUpdate.OnUpdate(OnUpdate));
                isHudOn = true;
                UpdateWristText();
                entry.enabled = false;
            }

        }
    }

    internal static class HudCurveDebug
    {
        public static bool IsStatusIconPath(Transform transform, Transform stop)
        {
            Transform current = transform;
            while (current != null && current != stop)
            {
                if (current.name == "Icon")
                    return true;
                current = current.parent;
            }
            return false;
        }

        public static Transform GetStatusBarIconRoot(Transform transform, Transform stop)
        {
            Transform current = transform;
            while (current != null && current != stop)
            {
                if (current.name == "Icon" && current.parent != null && current.parent.name.EndsWith("Bar"))
                {
                    Transform parent = current.parent;
                    while (parent != null && parent != stop)
                    {
                        if (parent.name == "BarsPanel")
                            return current;
                        parent = parent.parent;
                    }
                }
                current = current.parent;
            }
            return null;
        }

        public static bool IsStatusBarIconPath(Transform transform, Transform stop) =>
            GetStatusBarIconRoot(transform, stop) != null;

        public static Transform GetPopupNotificationRoot(Transform transform, Transform stop)
        {
            Transform current = transform;
            while (current != null && current != stop)
            {
                if (current.parent != null && current.parent != stop && current.parent.name == "PopupNotification")
                    return current;
                current = current.parent;
            }
            return null;
        }

        public static Transform GetScannerIconRoot(Transform transform, Transform stop)
        {
            Transform current = transform;
            while (current != null && current != stop)
            {
                if (current.name == "ScannerIcon")
                    return current;
                current = current.parent;
            }
            return null;
        }

        public static Transform GetCurveTransformRoot(Transform transform, Transform stop)
        {
            Transform statusRoot = GetStatusBarIconRoot(transform, stop);
            if (statusRoot != null) return statusRoot;
            Transform scannerRoot = GetScannerIconRoot(transform, stop);
            if (scannerRoot != null) return scannerRoot;
            return null;
        }

        public static bool IsCurveTransformPath(Transform transform, Transform stop) =>
            GetCurveTransformRoot(transform, stop) != null;

        public static bool IsPopupNotificationPath(Transform transform, Transform stop) =>
            GetPopupNotificationRoot(transform, stop) != null;

        public static bool HasPopupVisualRoot(Transform transform, Transform stop)
        {
            Transform popupRoot = GetPopupNotificationRoot(transform, stop);
            var effect = popupRoot != null ? popupRoot.GetComponent<HudPopupCurveTransformEffect>() : null;
            return effect != null && effect.HasVisualRoot;
        }

        public static bool HasCurveTransformAncestor(Transform transform, Transform stop)
        {
            Transform current = transform;
            while (current != null && current != stop)
            {
                if (current.GetComponent<HudCurveTransformEffect>() != null)
                    return true;
                current = current.parent;
            }
            return false;
        }

        public static bool IsAnimationSensitivePath(Transform transform, Transform stop)
        {
            if (IsUnderNamedParent(transform, stop, "PinnedRecipes"))
                return false;

            Transform current = transform;
            while (current != null && current != stop)
            {
                string name = current.name;
                if (name.StartsWith("RecipeItem") || name.StartsWith("RecipeEntry"))
                    return true;
                current = current.parent;
            }
            return IsPopupNotificationPath(transform, stop) ||
                   IsCurveTransformPath(transform, stop) ||
                   HasCurveTransformAncestor(transform, stop);
        }

        public static bool IsUnderNamedParent(Transform transform, Transform stop, string parentName)
        {
            Transform current = transform;
            while (current != null && current != stop)
            {
                if (current.name == parentName)
                    return true;
                current = current.parent;
            }
            return false;
        }

        public static bool IsPowerIndicatorPath(Transform transform, Transform stop)
        {
            Transform current = transform;
            while (current != null && current != stop)
            {
                if (current.name.IndexOf("Power", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    current.name.IndexOf("PowerIndicator", StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
                current = current.parent;
            }
            return false;
        }

        public static bool ShouldSkipCurve(Transform transform, Transform stop)
        {
            return IsUnderNamedParent(transform, stop, "HandReticle");
        }

        public static bool IsDescendantOf(Transform transform, Transform ancestor)
        {
            Transform current = transform;
            while (current != null)
            {
                if (current == ancestor)
                    return true;
                current = current.parent;
            }
            return false;
        }

        public static string BuildPath(Transform transform, Transform stop)
        {
            var names = new List<string>();
            Transform current = transform;
            while (current != null)
            {
                names.Add(current.name);
                if (current == stop) break;
                current = current.parent;
            }
            names.Reverse();
            return string.Join("/", names.ToArray());
        }

        public static string BuildChainSnapshot(Transform transform, Transform stop)
        {
            var parts = new List<string>();
            Transform current = transform;
            int depth = 0;
            while (current != null && current != stop && depth < 8)
            {
                var rt = current as RectTransform;
                Vector3 lp = current.localPosition;
                Vector3 lr = current.localEulerAngles;
                Vector3 ls = current.localScale;
                string item = $"{current.name}:lp=({lp.x:F1},{lp.y:F1},{lp.z:F1}) lr=({lr.x:F1},{lr.y:F1},{lr.z:F1}) ls=({ls.x:F3},{ls.y:F3},{ls.z:F3})";
                if (rt != null)
                {
                    Vector3 ap = rt.anchoredPosition3D;
                    Rect rect = rt.rect;
                    item += $" ap=({ap.x:F1},{ap.y:F1},{ap.z:F1}) size=({rect.width:F1},{rect.height:F1})";
                }
                parts.Add(item);
                current = current.parent;
                depth++;
            }
            return string.Join(" <- ", parts.ToArray());
        }
    }

    // Moves a whole status icon card onto the cylinder while child meshes keep only relative curvature.
    public class HudCurveTransformEffect : MonoBehaviour
    {
        public Canvas targetCanvas;
        public float radiusPixels = 0f;
        private Vector3 appliedLocalOffset = Vector3.zero;
        private Transform cachedTransform;

        void Awake() => cachedTransform = transform;

        void OnEnable()
        {
            Canvas.willRenderCanvases += OnWillRenderCanvases;
            ForceApply();
        }

        void OnDisable()
        {
            Canvas.willRenderCanvases -= OnWillRenderCanvases;
            RemoveAppliedTransform();
        }

        private void OnWillRenderCanvases() => ForceApply();

        public void ForceApply()
        {
            if (cachedTransform == null) cachedTransform = transform;
            RemoveAppliedTransform();
            if (!enabled || radiusPixels <= 0f || targetCanvas == null || cachedTransform.parent == null) return;

            float anchorCanvasX = targetCanvas.transform.InverseTransformPoint(cachedTransform.position).x;
            float angle = anchorCanvasX / radiusPixels;
            float targetX = radiusPixels * Mathf.Sin(angle);
            float targetZ = -radiusPixels * (1f - Mathf.Cos(angle));
            Vector3 canvasDelta = new Vector3(targetX - anchorCanvasX, 0f, targetZ);
            Vector3 worldDelta = targetCanvas.transform.TransformVector(canvasDelta);
            appliedLocalOffset = cachedTransform.parent.InverseTransformVector(worldDelta);
            cachedTransform.localPosition += appliedLocalOffset;
        }

        private void RemoveAppliedTransform()
        {
            if (cachedTransform == null) cachedTransform = transform;
            if (appliedLocalOffset != Vector3.zero)
            {
                cachedTransform.localPosition -= appliedLocalOffset;
                appliedLocalOffset = Vector3.zero;
            }
        }
    }

    // Keeps popup notification animation on the original root, while moving only its visual children
    // onto the curved HUD surface.
    public class HudPopupCurveTransformEffect : MonoBehaviour
    {
        public Canvas targetCanvas;
        public float radiusPixels = 0f;
        public Transform visualRoot;
        private Transform cachedTransform;
        private float stableCanvasX = float.NaN;
        private int debugLogCount = 0;
        private const int MaxDebugLogs = 24;
        private const float MaxTangentAngle = 1.0471976f; // 60 degrees
        private const string VisualRootName = "__VRHudPopupCurveVisual";

        public bool HasVisualRoot => visualRoot != null;

        void Awake() => cachedTransform = transform;

        void OnEnable()
        {
            Canvas.willRenderCanvases += OnWillRenderCanvases;
            ForceApply();
        }

        void OnDisable()
        {
            Canvas.willRenderCanvases -= OnWillRenderCanvases;
            ResetVisualRoot();
        }

        private void OnWillRenderCanvases() => ForceApply();

        public void ForceApply()
        {
            if (cachedTransform == null) cachedTransform = transform;
            EnsureVisualRoot();
            if (visualRoot == null) return;

            if (!enabled || radiusPixels <= 0f || targetCanvas == null)
            {
                ResetVisualRoot();
                return;
            }

            float currentCanvasX = targetCanvas.transform.InverseTransformPoint(cachedTransform.position).x;
            if (float.IsNaN(stableCanvasX) || Mathf.Abs(currentCanvasX) < Mathf.Abs(stableCanvasX))
                stableCanvasX = currentCanvasX;

            float anchorAngle = stableCanvasX / radiusPixels;
            float tangentAngle = Mathf.Clamp(anchorAngle, -MaxTangentAngle, MaxTangentAngle);
            float targetX = radiusPixels * Mathf.Sin(anchorAngle);
            float targetZ = -radiusPixels * (1f - Mathf.Cos(anchorAngle));
            Vector3 canvasDelta = new Vector3(targetX - stableCanvasX, 0f, targetZ);
            Vector3 worldDelta = targetCanvas.transform.TransformVector(canvasDelta);
            Vector3 localDelta = cachedTransform.InverseTransformVector(worldDelta);

            visualRoot.localPosition = localDelta;
            visualRoot.localRotation = Quaternion.Euler(0f, tangentAngle * Mathf.Rad2Deg, 0f);
            visualRoot.localScale = Vector3.one;

            if (debugLogCount < MaxDebugLogs)
            {
                debugLogCount++;
                Mod.logger.LogInfo(
                    $"[VRHud/PopupRoot] #{debugLogCount} name='{cachedTransform.name}' currentX={currentCanvasX:F2} stableX={stableCanvasX:F2} " +
                    $"angle={anchorAngle * Mathf.Rad2Deg:F1} yaw={tangentAngle * Mathf.Rad2Deg:F1} " +
                    $"offset=({localDelta.x:F2},{localDelta.y:F2},{localDelta.z:F2}) " +
                    $"path='{HudCurveDebug.BuildPath(cachedTransform, targetCanvas.transform)}'");
            }
        }

        private void EnsureVisualRoot()
        {
            if (cachedTransform == null) cachedTransform = transform;
            if (visualRoot == null)
            {
                Transform existing = cachedTransform.Find(VisualRootName);
                if (existing != null)
                {
                    visualRoot = existing;
                }
                else
                {
                    var go = new GameObject(VisualRootName, typeof(RectTransform));
                    go.layer = cachedTransform.gameObject.layer;
                    visualRoot = go.transform;
                    visualRoot.SetParent(cachedTransform, false);
                    ConfigureVisualRect();
                }
            }

            ConfigureVisualRect();
            for (int i = cachedTransform.childCount - 1; i >= 0; i--)
            {
                Transform child = cachedTransform.GetChild(i);
                if (child == visualRoot)
                    continue;
                child.SetParent(visualRoot, false);
            }
        }

        private void ConfigureVisualRect()
        {
            if (!(visualRoot is RectTransform visualRect))
                return;

            visualRect.localScale = Vector3.one;
            if (cachedTransform is RectTransform rootRect)
            {
                visualRect.anchorMin = Vector2.zero;
                visualRect.anchorMax = Vector2.one;
                visualRect.offsetMin = Vector2.zero;
                visualRect.offsetMax = Vector2.zero;
                visualRect.pivot = rootRect.pivot;
            }
            else
            {
                visualRect.localPosition = Vector3.zero;
                visualRect.localRotation = Quaternion.identity;
            }
        }

        private void ResetVisualRoot()
        {
            if (visualRoot == null) return;
            visualRoot.localPosition = Vector3.zero;
            visualRoot.localRotation = Quaternion.identity;
            visualRoot.localScale = Vector3.one;
        }
    }

    // Applies cylindrical curve distortion to a UI Graphic's mesh vertices.
    // Canvas-space x is mapped to a cylinder of radius radiusPixels (canvas pixel units).
    public class HudCurveEffect : BaseMeshEffect
    {
        public Canvas targetCanvas;
        public float radiusPixels = 0f;
        private int debugLogCount = 0;
        private bool debugPathLogged = false;
        private int lastInputVertexCount = 0;
        private int lastSubdivideSegments = 1;
        private const int MaxGraphicDebugLogs = 40;
        private const float PopupRelativeDepthScale = 1f;
        private const float PopupMaxTangentAngle = 1.0471976f; // 60 degrees; avoids edge-on popup cards at screen extremes.
        private const int MaxGraphicCurveSegments = 48;
        private const float GraphicCurveSegmentWidth = 16f;

        public override void ModifyMesh(VertexHelper vh)
        {
            if (!IsActive() || radiusPixels <= 0f || targetCanvas == null) return;
            if (!HudCurveDebug.IsDescendantOf(graphic.transform, targetCanvas.transform))
            {
                LogDetachedSample();
                return;
            }

            Transform popupRoot = HudCurveDebug.GetPopupNotificationRoot(graphic.transform, targetCanvas.transform);
            if (popupRoot != null)
            {
                var popupEffect = popupRoot.GetComponent<HudPopupCurveTransformEffect>();
                if (popupEffect != null && popupEffect.HasVisualRoot)
                {
                    ModifyMeshPopupLocal(vh, popupEffect.visualRoot);
                    return;
                }
                ModifyMeshPopupArc(vh, popupRoot);
                return;
            }

            if (HudCurveDebug.IsCurveTransformPath(graphic.transform, targetCanvas.transform) ||
                HudCurveDebug.HasCurveTransformAncestor(graphic.transform, targetCanvas.transform))
            {
                ModifyMeshRelative(vh, "relative", 1f, null, false, false);
                return;
            }

            var graphicToCanvas = targetCanvas.transform.worldToLocalMatrix
                                  * graphic.transform.localToWorldMatrix;
            var canvasToGraphic = graphicToCanvas.inverse;

            var verts = new System.Collections.Generic.List<UIVertex>();
            vh.GetUIVertexStream(verts);
            verts = SubdivideMeshByX(verts, GetLocalToCanvasScale());
            bool hasBeforeBounds = false;
            bool hasAfterBounds = false;
            float beforeMinX = 0f, beforeMaxX = 0f, beforeMinZ = 0f, beforeMaxZ = 0f;
            float afterMinX = 0f, afterMaxX = 0f, afterMinZ = 0f, afterMaxZ = 0f;
            for (int i = 0; i < verts.Count; i++)
            {
                UIVertex v = verts[i];
                Vector3 before = v.position;
                if (!hasBeforeBounds)
                {
                    beforeMinX = beforeMaxX = before.x;
                    beforeMinZ = beforeMaxZ = before.z;
                    hasBeforeBounds = true;
                }
                else
                {
                    beforeMinX = Mathf.Min(beforeMinX, before.x);
                    beforeMaxX = Mathf.Max(beforeMaxX, before.x);
                    beforeMinZ = Mathf.Min(beforeMinZ, before.z);
                    beforeMaxZ = Mathf.Max(beforeMaxZ, before.z);
                }

                Vector3 cp = graphicToCanvas.MultiplyPoint3x4(v.position);
                float angle = cp.x / radiusPixels;
                cp.x = radiusPixels * Mathf.Sin(angle);
                cp.z -= radiusPixels * (1f - Mathf.Cos(angle));
                v.position = canvasToGraphic.MultiplyPoint3x4(cp);
                Vector3 after = v.position;
                if (!hasAfterBounds)
                {
                    afterMinX = afterMaxX = after.x;
                    afterMinZ = afterMaxZ = after.z;
                    hasAfterBounds = true;
                }
                else
                {
                    afterMinX = Mathf.Min(afterMinX, after.x);
                    afterMaxX = Mathf.Max(afterMaxX, after.x);
                    afterMinZ = Mathf.Min(afterMinZ, after.z);
                    afterMaxZ = Mathf.Max(afterMaxZ, after.z);
                }
                verts[i] = v;
            }
            vh.Clear();
            vh.AddUIVertexTriangleStream(verts);

            LogDebugSample("vertex", verts.Count, beforeMinX, beforeMaxX, beforeMinZ, beforeMaxZ,
                afterMinX, afterMaxX, afterMinZ, afterMaxZ);
        }

        private void ModifyMeshRelative(VertexHelper vh, string mode, float depthScale, Transform anchorTransform, bool includeAnchorOffset, bool preserveRelativeX)
        {
            float localToCanvasScale = GetLocalToCanvasScale();
            if (Mathf.Approximately(localToCanvasScale, 0f)) return;

            Vector3 pivotCanvas = targetCanvas.transform.InverseTransformPoint(graphic.transform.position);
            Vector3 anchorCanvas = anchorTransform != null
                ? targetCanvas.transform.InverseTransformPoint(anchorTransform.position)
                : pivotCanvas;
            float anchorAngle = anchorCanvas.x / radiusPixels;
            float anchorTargetX = radiusPixels * Mathf.Sin(anchorAngle);
            float anchorTargetZ = -radiusPixels * (1f - Mathf.Cos(anchorAngle));
            float anchorDeltaCanvasX = anchorTargetX - anchorCanvas.x;
            float anchorDeltaCanvasZ = anchorTargetZ;

            var verts = new System.Collections.Generic.List<UIVertex>();
            vh.GetUIVertexStream(verts);
            verts = SubdivideMeshByX(verts, localToCanvasScale);
            bool hasBeforeBounds = false;
            bool hasAfterBounds = false;
            float beforeMinX = 0f, beforeMaxX = 0f, beforeMinZ = 0f, beforeMaxZ = 0f;
            float afterMinX = 0f, afterMaxX = 0f, afterMinZ = 0f, afterMaxZ = 0f;
            for (int i = 0; i < verts.Count; i++)
            {
                UIVertex v = verts[i];
                Vector3 before = v.position;
                if (!hasBeforeBounds)
                {
                    beforeMinX = beforeMaxX = before.x;
                    beforeMinZ = beforeMaxZ = before.z;
                    hasBeforeBounds = true;
                }
                else
                {
                    beforeMinX = Mathf.Min(beforeMinX, before.x);
                    beforeMaxX = Mathf.Max(beforeMaxX, before.x);
                    beforeMinZ = Mathf.Min(beforeMinZ, before.z);
                    beforeMaxZ = Mathf.Max(beforeMaxZ, before.z);
                }

                float virtualX = pivotCanvas.x + before.x * localToCanvasScale;
                float angle = virtualX / radiusPixels;
                float targetX = radiusPixels * Mathf.Sin(angle);
                float targetZ = -radiusPixels * (1f - Mathf.Cos(angle));
                float relativeCanvasX = targetX - virtualX - anchorDeltaCanvasX;
                float relativeCanvasZ = (targetZ - anchorDeltaCanvasZ) * depthScale;
                float deltaCanvasX = includeAnchorOffset
                    ? anchorDeltaCanvasX + (preserveRelativeX ? 0f : relativeCanvasX)
                    : relativeCanvasX;
                float deltaCanvasZ = includeAnchorOffset ? anchorDeltaCanvasZ + relativeCanvasZ : relativeCanvasZ;
                v.position = before + new Vector3(deltaCanvasX / localToCanvasScale, 0f, deltaCanvasZ / localToCanvasScale);

                Vector3 after = v.position;
                if (!hasAfterBounds)
                {
                    afterMinX = afterMaxX = after.x;
                    afterMinZ = afterMaxZ = after.z;
                    hasAfterBounds = true;
                }
                else
                {
                    afterMinX = Mathf.Min(afterMinX, after.x);
                    afterMaxX = Mathf.Max(afterMaxX, after.x);
                    afterMinZ = Mathf.Min(afterMinZ, after.z);
                    afterMaxZ = Mathf.Max(afterMaxZ, after.z);
                }
                verts[i] = v;
            }
            vh.Clear();
            vh.AddUIVertexTriangleStream(verts);

            LogDebugSample(mode, verts.Count, beforeMinX, beforeMaxX, beforeMinZ, beforeMaxZ,
                afterMinX, afterMaxX, afterMinZ, afterMaxZ);
        }

        private void ModifyMeshPopupArc(VertexHelper vh, Transform anchorTransform)
        {
            float localToCanvasScale = GetLocalToCanvasScale();
            if (Mathf.Approximately(localToCanvasScale, 0f) || anchorTransform == null) return;

            Vector3 pivotCanvas = targetCanvas.transform.InverseTransformPoint(graphic.transform.position);
            Vector3 anchorCanvas = targetCanvas.transform.InverseTransformPoint(anchorTransform.position);
            float anchorAngle = anchorCanvas.x / radiusPixels;
            float tangentAngle = Mathf.Clamp(anchorAngle, -PopupMaxTangentAngle, PopupMaxTangentAngle);
            float anchorTargetX = radiusPixels * Mathf.Sin(anchorAngle);
            float anchorTargetZ = -radiusPixels * (1f - Mathf.Cos(anchorAngle));
            float anchorDeltaCanvasX = anchorTargetX - anchorCanvas.x;
            float cos = Mathf.Cos(tangentAngle);
            float sin = Mathf.Sin(tangentAngle);

            var verts = new System.Collections.Generic.List<UIVertex>();
            vh.GetUIVertexStream(verts);
            verts = SubdivideMeshByX(verts, localToCanvasScale);
            bool hasBeforeBounds = false;
            bool hasAfterBounds = false;
            float beforeMinX = 0f, beforeMaxX = 0f, beforeMinZ = 0f, beforeMaxZ = 0f;
            float afterMinX = 0f, afterMaxX = 0f, afterMinZ = 0f, afterMaxZ = 0f;
            for (int i = 0; i < verts.Count; i++)
            {
                UIVertex v = verts[i];
                Vector3 before = v.position;
                if (!hasBeforeBounds)
                {
                    beforeMinX = beforeMaxX = before.x;
                    beforeMinZ = beforeMaxZ = before.z;
                    hasBeforeBounds = true;
                }
                else
                {
                    beforeMinX = Mathf.Min(beforeMinX, before.x);
                    beforeMaxX = Mathf.Max(beforeMaxX, before.x);
                    beforeMinZ = Mathf.Min(beforeMinZ, before.z);
                    beforeMaxZ = Mathf.Max(beforeMaxZ, before.z);
                }

                float relativeCanvasX = (pivotCanvas.x - anchorCanvas.x) + before.x * localToCanvasScale;
                float localAngle = relativeCanvasX / radiusPixels;
                float localArcX = radiusPixels * Mathf.Sin(localAngle);
                float localArcZ = -radiusPixels * (1f - Mathf.Cos(localAngle)) * PopupRelativeDepthScale;
                float rotatedX = cos * localArcX + sin * localArcZ;
                float rotatedZ = -sin * localArcX + cos * localArcZ;
                float deltaCanvasX = anchorDeltaCanvasX + rotatedX - relativeCanvasX;
                float deltaCanvasZ = anchorTargetZ + rotatedZ;
                v.position = before + new Vector3(deltaCanvasX / localToCanvasScale, 0f, deltaCanvasZ / localToCanvasScale);

                Vector3 after = v.position;
                if (!hasAfterBounds)
                {
                    afterMinX = afterMaxX = after.x;
                    afterMinZ = afterMaxZ = after.z;
                    hasAfterBounds = true;
                }
                else
                {
                    afterMinX = Mathf.Min(afterMinX, after.x);
                    afterMaxX = Mathf.Max(afterMaxX, after.x);
                    afterMinZ = Mathf.Min(afterMinZ, after.z);
                    afterMaxZ = Mathf.Max(afterMaxZ, after.z);
                }
                verts[i] = v;
            }
            vh.Clear();
            vh.AddUIVertexTriangleStream(verts);

            LogDebugSample("popupArc", verts.Count, beforeMinX, beforeMaxX, beforeMinZ, beforeMaxZ,
                afterMinX, afterMaxX, afterMinZ, afterMaxZ);
        }

        private void ModifyMeshPopupLocal(VertexHelper vh, Transform visualRoot)
        {
            float localToVisualScale = GetLocalToAncestorScale(visualRoot);
            if (Mathf.Approximately(localToVisualScale, 0f) || visualRoot == null) return;

            Vector3 pivotVisual = visualRoot.InverseTransformPoint(graphic.transform.position);

            var verts = new System.Collections.Generic.List<UIVertex>();
            vh.GetUIVertexStream(verts);
            verts = SubdivideMeshByX(verts, localToVisualScale);
            bool hasBeforeBounds = false;
            bool hasAfterBounds = false;
            float beforeMinX = 0f, beforeMaxX = 0f, beforeMinZ = 0f, beforeMaxZ = 0f;
            float afterMinX = 0f, afterMaxX = 0f, afterMinZ = 0f, afterMaxZ = 0f;
            for (int i = 0; i < verts.Count; i++)
            {
                UIVertex v = verts[i];
                Vector3 before = v.position;
                if (!hasBeforeBounds)
                {
                    beforeMinX = beforeMaxX = before.x;
                    beforeMinZ = beforeMaxZ = before.z;
                    hasBeforeBounds = true;
                }
                else
                {
                    beforeMinX = Mathf.Min(beforeMinX, before.x);
                    beforeMaxX = Mathf.Max(beforeMaxX, before.x);
                    beforeMinZ = Mathf.Min(beforeMinZ, before.z);
                    beforeMaxZ = Mathf.Max(beforeMaxZ, before.z);
                }

                float cardX = pivotVisual.x + before.x * localToVisualScale;
                float angle = cardX / radiusPixels;
                float targetX = radiusPixels * Mathf.Sin(angle);
                float targetZ = -radiusPixels * (1f - Mathf.Cos(angle));
                float deltaCanvasX = targetX - cardX;
                float deltaCanvasZ = targetZ;
                v.position = before + new Vector3(deltaCanvasX / localToVisualScale, 0f, deltaCanvasZ / localToVisualScale);

                Vector3 after = v.position;
                if (!hasAfterBounds)
                {
                    afterMinX = afterMaxX = after.x;
                    afterMinZ = afterMaxZ = after.z;
                    hasAfterBounds = true;
                }
                else
                {
                    afterMinX = Mathf.Min(afterMinX, after.x);
                    afterMaxX = Mathf.Max(afterMaxX, after.x);
                    afterMinZ = Mathf.Min(afterMinZ, after.z);
                    afterMaxZ = Mathf.Max(afterMaxZ, after.z);
                }
                verts[i] = v;
            }
            vh.Clear();
            vh.AddUIVertexTriangleStream(verts);

            LogDebugSample("popupLocal", verts.Count, beforeMinX, beforeMaxX, beforeMinZ, beforeMaxZ,
                afterMinX, afterMaxX, afterMinZ, afterMaxZ);
        }

        private float GetLocalToCanvasScale()
        {
            float scale = 1f;
            Transform current = graphic.transform;
            Transform canvasTransform = targetCanvas.transform;
            while (current != null && current != canvasTransform)
            {
                scale *= current.localScale.x;
                current = current.parent;
            }
            return scale;
        }

        private float GetLocalToAncestorScale(Transform ancestor)
        {
            float scale = 1f;
            Transform current = graphic.transform;
            while (current != null && current != ancestor)
            {
                scale *= current.localScale.x;
                current = current.parent;
            }
            return current == ancestor ? scale : 0f;
        }

        private System.Collections.Generic.List<UIVertex> SubdivideMeshByX(System.Collections.Generic.List<UIVertex> source, float localToCanvasScale)
        {
            lastInputVertexCount = source != null ? source.Count : 0;
            lastSubdivideSegments = 1;

            if (source == null || source.Count < 3 || source.Count % 3 != 0 || Mathf.Approximately(localToCanvasScale, 0f))
                return source;

            GetXBounds(source, out float minX, out float maxX);
            float width = maxX - minX;
            float canvasWidth = Mathf.Abs(width * localToCanvasScale);
            int segments = Mathf.Clamp(Mathf.CeilToInt(canvasWidth / GraphicCurveSegmentWidth), 1, MaxGraphicCurveSegments);
            if (segments <= 1)
                return source;

            lastSubdivideSegments = segments;
            var result = new System.Collections.Generic.List<UIVertex>(source.Count * segments);
            float step = width / segments;

            for (int i = 0; i < source.Count; i += 3)
            {
                var triangle = new System.Collections.Generic.List<UIVertex>(3)
                {
                    source[i],
                    source[i + 1],
                    source[i + 2]
                };

                for (int s = 0; s < segments; s++)
                {
                    float stripMin = minX + step * s;
                    float stripMax = s == segments - 1 ? maxX : stripMin + step;
                    var clipped = ClipPolygonByX(triangle, stripMin, true);
                    clipped = ClipPolygonByX(clipped, stripMax, false);
                    AddTriangulatedPolygon(result, clipped);
                }
            }

            return result.Count > 0 ? result : source;
        }

        private void GetXBounds(System.Collections.Generic.List<UIVertex> verts, out float minX, out float maxX)
        {
            minX = maxX = verts[0].position.x;
            for (int i = 1; i < verts.Count; i++)
            {
                float x = verts[i].position.x;
                minX = Mathf.Min(minX, x);
                maxX = Mathf.Max(maxX, x);
            }
        }

        private System.Collections.Generic.List<UIVertex> ClipPolygonByX(System.Collections.Generic.List<UIVertex> polygon, float clipX, bool keepGreater)
        {
            var output = new System.Collections.Generic.List<UIVertex>();
            if (polygon == null || polygon.Count == 0)
                return output;

            UIVertex previous = polygon[polygon.Count - 1];
            bool previousInside = IsInsideX(previous, clipX, keepGreater);
            for (int i = 0; i < polygon.Count; i++)
            {
                UIVertex current = polygon[i];
                bool currentInside = IsInsideX(current, clipX, keepGreater);
                if (currentInside != previousInside)
                {
                    output.Add(IntersectAtX(previous, current, clipX));
                }
                if (currentInside)
                    output.Add(current);
                previous = current;
                previousInside = currentInside;
            }

            return output;
        }

        private bool IsInsideX(UIVertex v, float clipX, bool keepGreater)
        {
            return keepGreater ? v.position.x >= clipX : v.position.x <= clipX;
        }

        private UIVertex IntersectAtX(UIVertex a, UIVertex b, float x)
        {
            float dx = b.position.x - a.position.x;
            float t = Mathf.Approximately(dx, 0f) ? 0f : Mathf.Clamp01((x - a.position.x) / dx);
            UIVertex v = LerpVertex(a, b, t);
            v.position.x = x;
            return v;
        }

        private void AddTriangulatedPolygon(System.Collections.Generic.List<UIVertex> result, System.Collections.Generic.List<UIVertex> polygon)
        {
            if (polygon == null || polygon.Count < 3)
                return;

            UIVertex first = polygon[0];
            for (int i = 1; i < polygon.Count - 1; i++)
            {
                result.Add(first);
                result.Add(polygon[i]);
                result.Add(polygon[i + 1]);
            }
        }

        private UIVertex LerpVertex(UIVertex a, UIVertex b, float t)
        {
            UIVertex v = a;
            v.position = Vector3.Lerp(a.position, b.position, t);
            v.normal = Vector3.Lerp(a.normal, b.normal, t);
            v.tangent = Vector4.Lerp(a.tangent, b.tangent, t);
            v.color = Color32.Lerp(a.color, b.color, t);
            v.uv0 = Vector4.Lerp(a.uv0, b.uv0, t);
            v.uv1 = Vector4.Lerp(a.uv1, b.uv1, t);
            v.uv2 = Vector4.Lerp(a.uv2, b.uv2, t);
            v.uv3 = Vector4.Lerp(a.uv3, b.uv3, t);
            return v;
        }

        private void LogDetachedSample()
        {
            if (debugPathLogged || targetCanvas == null) return;
            debugPathLogged = true;
            Mod.logger.LogInfo(
                $"[VRHud/Graphic] detached skip type='{graphic.GetType().Name}' name='{graphic.name}' " +
                $"targetCanvas='{targetCanvas.name}' path='{HudCurveDebug.BuildPath(graphic.transform, targetCanvas.transform)}'");
        }

        private void LogDebugSample(string mode, int vertexCount,
            float beforeMinX, float beforeMaxX, float beforeMinZ, float beforeMaxZ,
            float afterMinX, float afterMaxX, float afterMinZ, float afterMaxZ)
        {
            if (debugLogCount >= MaxGraphicDebugLogs || vertexCount <= 0 || targetCanvas == null) return;
            bool interestingPath =
                HudCurveDebug.IsStatusIconPath(graphic.transform, targetCanvas.transform) ||
                HudCurveDebug.IsUnderNamedParent(graphic.transform, targetCanvas.transform, "PinnedRecipes") ||
                HudCurveDebug.IsUnderNamedParent(graphic.transform, targetCanvas.transform, "TalkingHead") ||
                HudCurveDebug.IsUnderNamedParent(graphic.transform, targetCanvas.transform, "SubtitleCanvas") ||
                HudCurveDebug.IsPopupNotificationPath(graphic.transform, targetCanvas.transform) ||
                HudCurveDebug.IsCurveTransformPath(graphic.transform, targetCanvas.transform) ||
                HudCurveDebug.IsPowerIndicatorPath(graphic.transform, targetCanvas.transform);
            if (!interestingPath) return;
            debugLogCount++;

            Vector3 pivotCanvas = targetCanvas.transform.InverseTransformPoint(graphic.transform.position);
            string parentName = graphic.transform.parent != null ? graphic.transform.parent.name : "<none>";
            float facingDot = Vector3.Dot(graphic.transform.forward, targetCanvas.transform.forward);
            Mod.logger.LogInfo(
                $"[VRHud/Graphic] #{debugLogCount} type='{graphic.GetType().Name}' name='{graphic.name}' parent='{parentName}' " +
                $"radius={radiusPixels:F2} pivot=({pivotCanvas.x:F2},{pivotCanvas.y:F2},{pivotCanvas.z:F2}) " +
                $"euler=({graphic.transform.localEulerAngles.x:F1},{graphic.transform.localEulerAngles.y:F1},{graphic.transform.localEulerAngles.z:F1}) " +
                $"dot={facingDot:F3} verts={lastInputVertexCount}->{vertexCount} segments={lastSubdivideSegments} mode={mode} " +
                $"beforeX=({beforeMinX:F2},{beforeMaxX:F2}) beforeZ=({beforeMinZ:F2},{beforeMaxZ:F2}) " +
                $"afterX=({afterMinX:F2},{afterMaxX:F2}) afterZ=({afterMinZ:F2},{afterMaxZ:F2}) " +
                $"span=({afterMaxX - afterMinX:F2},{afterMaxZ - afterMinZ:F2})");

            if (!debugPathLogged)
            {
                debugPathLogged = true;
                Mod.logger.LogInfo(
                    $"[VRHud/CurveNode] kind=Graphic path='{HudCurveDebug.BuildPath(graphic.transform, targetCanvas.transform)}' " +
                    $"chain='{HudCurveDebug.BuildChainSnapshot(graphic.transform, targetCanvas.transform)}'");
            }
        }
    }

    // Applies cylindrical curve distortion to TextMeshProUGUI vertices.
    // TMP bypasses BaseMeshEffect/IMeshModifier, so we hook TMPro_EventManager.TEXT_CHANGED_EVENT instead.
    // "Virtual Canvas X" approach: decouples curve calculation from 3D rotation.
    //   - virtualX = pivotCanvas.x + vertex.x * scale  (as if TMP were unrotated)
    //   - Deltas applied in local space; 3D rotation (flip) is handled by rendering of pre-curved vertices.
    public class TmpHudCurveEffect : MonoBehaviour
    {
        public Canvas targetCanvas;
        public float radiusPixels = 0f;
        private TMPro.TextMeshProUGUI tmp;
        private bool applying = false;
        private float lastPivotCanvasX = float.NaN;
        private int debugLogCount = 0;
        private int restoreLogCount = 0;
        private const int MaxDebugLogs = 16;
        private const int MaxRestoreLogs = 16;
        private const float PopupRelativeDepthScale = 1f;
        private const float PopupMaxTangentAngle = 1.0471976f; // 60 degrees; keeps popup text readable near screen edges.
        private Vector3[][] sourceVertices = null;
        private bool debugPathLogged = false;
        private bool meshRestored = false;
        private bool hiddenMeshCleared = false;

        void Awake() => tmp = GetComponent<TMPro.TextMeshProUGUI>();

        void OnEnable()
        {
            Canvas.willRenderCanvases += OnWillRenderCanvases;
            TMPro.TMPro_EventManager.TEXT_CHANGED_EVENT.Add(OnTextChanged);
            ForceApply("enable");
        }

        void OnDisable()
        {
            Canvas.willRenderCanvases -= OnWillRenderCanvases;
            TMPro.TMPro_EventManager.TEXT_CHANGED_EVENT.Remove(OnTextChanged);
            RestoreMesh("disable");
        }

        private void OnWillRenderCanvases()
        {
            if (tmp == null) return;
            if (!tmp.isActiveAndEnabled)
            {
                ClearHiddenMesh("willRenderHidden");
                return;
            }
            if (radiusPixels <= 0f || targetCanvas == null)
            {
                RestoreMesh("willRenderInactive");
                return;
            }
            if (!HudCurveDebug.IsDescendantOf(tmp.transform, targetCanvas.transform))
            {
                RestoreMesh("willRenderDetached");
                return;
            }
            // Only trigger on pivot canvas-x change — ignore pure rotation (flip animations)
            float pivotX = targetCanvas.transform.InverseTransformPoint(tmp.transform.position).x;
            if (!Mathf.Approximately(pivotX, lastPivotCanvasX))
            {
                lastPivotCanvasX = pivotX;
                ForceApply("pivotChanged");
            }
        }

        private void OnTextChanged(UnityEngine.Object obj)
        {
            // Guard: ForceMeshUpdate() inside ForceApply triggers this event; skip to avoid double-apply
            if (obj == tmp && !applying) ForceApply("textChanged");
        }

        public void ForceApply(string reason = "force")
        {
            if (tmp == null) return;
            if (!tmp.isActiveAndEnabled)
            {
                ClearHiddenMesh(reason + "Hidden");
                return;
            }
            if (radiusPixels <= 0f || targetCanvas == null)
            {
                RestoreMesh(reason);
                return;
            }
            if (!HudCurveDebug.IsDescendantOf(tmp.transform, targetCanvas.transform))
            {
                LogDetachedSample(reason);
                RestoreMesh(reason + "Detached");
                return;
            }
            // Update lastPivotCanvasX before ForceMeshUpdate to prevent OnWillRenderCanvases re-triggering
            lastPivotCanvasX = targetCanvas.transform.InverseTransformPoint(tmp.transform.position).x;
            applying = true;
            tmp.ForceMeshUpdate();
            applying = false;
            meshRestored = false;
            hiddenMeshCleared = false;
            CaptureSourceVertices();
            ApplyToMesh(reason);
        }

        private void ApplyToMesh(string reason)
        {
            if (!enabled || radiusPixels <= 0f || targetCanvas == null || tmp == null)
            {
                RestoreMesh(reason);
                return;
            }
            if (!tmp.isActiveAndEnabled)
            {
                ClearHiddenMesh(reason + "Hidden");
                return;
            }
            if (!HudCurveDebug.IsDescendantOf(tmp.transform, targetCanvas.transform))
            {
                LogDetachedSample(reason);
                RestoreMesh(reason + "Detached");
                return;
            }

            // Pivot in canvas space (position only — unaffected by local rotation of TMP)
            Vector3 pivotCanvas = targetCanvas.transform.InverseTransformPoint(tmp.transform.position);
            // Local-to-canvas scale ratio (scale only, rotation ignored)
            float localToCanvasScale = GetLocalToCanvasScale();
            if (Mathf.Approximately(localToCanvasScale, 0f)) return;
            Transform popupRoot = HudCurveDebug.GetPopupNotificationRoot(tmp.transform, targetCanvas.transform);
            var popupEffect = popupRoot != null ? popupRoot.GetComponent<HudPopupCurveTransformEffect>() : null;
            bool usePopupLocal = popupEffect != null && popupEffect.HasVisualRoot;
            bool usePopupArc = popupRoot != null && !usePopupLocal;
            bool useRelativeCurve = !usePopupArc && IsAnimationSensitiveText();
            float anchorDeltaCanvasX = 0f;
            float anchorDeltaCanvasZ = 0f;
            Vector3 popupAnchorCanvas = Vector3.zero;
            float popupAnchorTargetZ = 0f;
            float popupCos = 1f;
            float popupSin = 0f;
            float localToVisualScale = 0f;
            Vector3 popupPivotVisual = Vector3.zero;
            if (usePopupLocal)
            {
                localToVisualScale = GetLocalToAncestorScale(popupEffect.visualRoot);
                if (Mathf.Approximately(localToVisualScale, 0f)) return;
                popupPivotVisual = popupEffect.visualRoot.InverseTransformPoint(tmp.transform.position);
            }
            if (usePopupArc)
            {
                popupAnchorCanvas = targetCanvas.transform.InverseTransformPoint(popupRoot.position);
                float popupAnchorAngle = popupAnchorCanvas.x / radiusPixels;
                float popupTangentAngle = Mathf.Clamp(popupAnchorAngle, -PopupMaxTangentAngle, PopupMaxTangentAngle);
                float popupAnchorTargetX = radiusPixels * Mathf.Sin(popupAnchorAngle);
                popupAnchorTargetZ = -radiusPixels * (1f - Mathf.Cos(popupAnchorAngle));
                anchorDeltaCanvasX = popupAnchorTargetX - popupAnchorCanvas.x;
                popupCos = Mathf.Cos(popupTangentAngle);
                popupSin = Mathf.Sin(popupTangentAngle);
            }
            if (useRelativeCurve)
            {
                Vector3 anchorCanvas = pivotCanvas;
                float anchorAngle = anchorCanvas.x / radiusPixels;
                float anchorTargetX = radiusPixels * Mathf.Sin(anchorAngle);
                float anchorTargetZ = -radiusPixels * (1f - Mathf.Cos(anchorAngle));
                anchorDeltaCanvasX = anchorTargetX - anchorCanvas.x;
                anchorDeltaCanvasZ = anchorTargetZ;
            }

            var textInfo = tmp.textInfo;
            EnsureSourceVertices();
            bool changed = false;
            int totalVertexCount = 0;
            bool hasBeforeBounds = false;
            bool hasAfterBounds = false;
            float beforeMinX = 0f, beforeMaxX = 0f, beforeMinZ = 0f, beforeMaxZ = 0f;
            float afterMinX = 0f, afterMaxX = 0f, afterMinZ = 0f, afterMaxZ = 0f;
            for (int m = 0; m < textInfo.meshInfo.Length; m++)
            {
                var verts = textInfo.meshInfo[m].vertices;
                if (verts == null) continue;
                int count = textInfo.meshInfo[m].vertexCount;
                if (sourceVertices == null || m >= sourceVertices.Length ||
                    sourceVertices[m] == null || sourceVertices[m].Length < count)
                {
                    CaptureSourceVertices();
                    if (sourceVertices == null || m >= sourceVertices.Length ||
                        sourceVertices[m] == null || sourceVertices[m].Length < count)
                    {
                        continue;
                    }
                }
                totalVertexCount += count;
                for (int v = 0; v < count; v++)
                {
                    Vector3 before = sourceVertices[m][v];
                    if (!hasBeforeBounds)
                    {
                        beforeMinX = beforeMaxX = before.x;
                        beforeMinZ = beforeMaxZ = before.z;
                        hasBeforeBounds = true;
                    }
                    else
                    {
                        beforeMinX = Mathf.Min(beforeMinX, before.x);
                        beforeMaxX = Mathf.Max(beforeMaxX, before.x);
                        beforeMinZ = Mathf.Min(beforeMinZ, before.z);
                        beforeMaxZ = Mathf.Max(beforeMaxZ, before.z);
                    }

                    // Virtual canvas x = pivot + vertex local x (scaled), as if TMP were unrotated
                    float virtualX = pivotCanvas.x + before.x * localToCanvasScale;
                    float angle = virtualX / radiusPixels;
                    float targetX = radiusPixels * Mathf.Sin(angle);
                    float targetZ = -radiusPixels * (1f - Mathf.Cos(angle));

                    // Canvas-space deltas -> local-space deltas (divide by stable scale).
                    float deltaCanvasX = targetX - virtualX;
                    float deltaCanvasZ = targetZ;  // straight z is 0

                    if (usePopupLocal)
                    {
                        float cardX = popupPivotVisual.x + before.x * localToVisualScale;
                        float localAngle = cardX / radiusPixels;
                        float localTargetX = radiusPixels * Mathf.Sin(localAngle);
                        float localTargetZ = -radiusPixels * (1f - Mathf.Cos(localAngle));
                        deltaCanvasX = localTargetX - cardX;
                        deltaCanvasZ = localTargetZ;
                    }
                    else if (usePopupArc)
                    {
                        float relativeCanvasX = (pivotCanvas.x - popupAnchorCanvas.x) + before.x * localToCanvasScale;
                        float localAngle = relativeCanvasX / radiusPixels;
                        float localArcX = radiusPixels * Mathf.Sin(localAngle);
                        float localArcZ = -radiusPixels * (1f - Mathf.Cos(localAngle)) * PopupRelativeDepthScale;
                        float rotatedX = popupCos * localArcX + popupSin * localArcZ;
                        float rotatedZ = -popupSin * localArcX + popupCos * localArcZ;
                        deltaCanvasX = anchorDeltaCanvasX + rotatedX - relativeCanvasX;
                        deltaCanvasZ = popupAnchorTargetZ + rotatedZ;
                    }
                    else if (useRelativeCurve)
                    {
                        // Keep the animated object's local pivot stable. Only preserve the
                        // character-relative bend, so flip/swap animations do not orbit around
                        // the cylinder depth offset.
                        deltaCanvasX -= anchorDeltaCanvasX;
                        deltaCanvasZ -= anchorDeltaCanvasZ;
                    }

                    float applyScale = usePopupLocal ? localToVisualScale : localToCanvasScale;
                    verts[v] = before + new Vector3(deltaCanvasX / applyScale, 0f, deltaCanvasZ / applyScale);

                    Vector3 after = verts[v];
                    if (!hasAfterBounds)
                    {
                        afterMinX = afterMaxX = after.x;
                        afterMinZ = afterMaxZ = after.z;
                        hasAfterBounds = true;
                    }
                    else
                    {
                        afterMinX = Mathf.Min(afterMinX, after.x);
                        afterMaxX = Mathf.Max(afterMaxX, after.x);
                        afterMinZ = Mathf.Min(afterMinZ, after.z);
                        afterMaxZ = Mathf.Max(afterMaxZ, after.z);
                    }
                }
                changed = true;
            }
            if (changed) tmp.UpdateVertexData(TMPro.TMP_VertexDataUpdateFlags.Vertices);
            LogDebugSample(reason, pivotCanvas, localToCanvasScale, totalVertexCount,
                beforeMinX, beforeMaxX, beforeMinZ, beforeMaxZ,
                afterMinX, afterMaxX, afterMinZ, afterMaxZ);
        }

        private void LogDetachedSample(string reason)
        {
            if (debugPathLogged || targetCanvas == null) return;
            debugPathLogged = true;
            Mod.logger.LogInfo(
                $"[VRHud/TMP] detached skip reason={reason} name='{tmp.name}' targetCanvas='{targetCanvas.name}' " +
                $"path='{HudCurveDebug.BuildPath(tmp.transform, targetCanvas.transform)}'");
        }

        private void RestoreMesh(string reason)
        {
            if (tmp == null) return;
            if (!tmp.isActiveAndEnabled)
            {
                ClearHiddenMesh(reason + "Hidden");
                return;
            }
            if (meshRestored && reason.StartsWith("willRender"))
                return;
            applying = true;
            tmp.ForceMeshUpdate(true, true);
            applying = false;
            sourceVertices = null;
            lastPivotCanvasX = float.NaN;
            meshRestored = true;
            hiddenMeshCleared = false;
            LogRestoreSample(reason);
        }

        private void ClearHiddenMesh(string reason)
        {
            if (tmp == null) return;
            if (hiddenMeshCleared && reason.StartsWith("willRender"))
                return;
            applying = true;
            tmp.ClearMesh(true);
            if (tmp.canvasRenderer != null)
                tmp.canvasRenderer.Clear();
            applying = false;
            sourceVertices = null;
            lastPivotCanvasX = float.NaN;
            meshRestored = true;
            hiddenMeshCleared = true;
            LogRestoreSample(reason);
        }

        private void LogRestoreSample(string reason)
        {
            if (restoreLogCount >= MaxRestoreLogs || tmp == null) return;
            string text = tmp.text ?? "";
            bool isPowerText = text.Contains("전력") || text.IndexOf("Power", StringComparison.OrdinalIgnoreCase) >= 0;
            bool isPowerPath = targetCanvas != null && HudCurveDebug.IsPowerIndicatorPath(tmp.transform, targetCanvas.transform);
            if (!isPowerText && !isPowerPath) return;

            restoreLogCount++;
            string path = targetCanvas != null
                ? HudCurveDebug.BuildPath(tmp.transform, targetCanvas.transform)
                : HudCurveDebug.BuildPath(tmp.transform, null);
            Mod.logger.LogInfo(
                $"[VRHud/TMPRestore] #{restoreLogCount} reason={reason} name='{tmp.name}' text='{text}' " +
                $"active={tmp.gameObject.activeInHierarchy} path='{path}'");
        }

        private void LogDebugSample(string reason, Vector3 pivotCanvas, float localToCanvasScale, int vertexCount,
            float beforeMinX, float beforeMaxX, float beforeMinZ, float beforeMaxZ,
            float afterMinX, float afterMaxX, float afterMinZ, float afterMaxZ)
        {
            if (debugLogCount >= MaxDebugLogs || vertexCount <= 0 || targetCanvas == null) return;
            bool interestingPath =
                HudCurveDebug.IsStatusIconPath(tmp.transform, targetCanvas.transform) ||
                HudCurveDebug.IsUnderNamedParent(tmp.transform, targetCanvas.transform, "PinnedRecipes") ||
                HudCurveDebug.IsUnderNamedParent(tmp.transform, targetCanvas.transform, "TalkingHead") ||
                HudCurveDebug.IsUnderNamedParent(tmp.transform, targetCanvas.transform, "SubtitleCanvas") ||
                HudCurveDebug.IsPopupNotificationPath(tmp.transform, targetCanvas.transform) ||
                HudCurveDebug.IsCurveTransformPath(tmp.transform, targetCanvas.transform) ||
                HudCurveDebug.IsPowerIndicatorPath(tmp.transform, targetCanvas.transform);
            if (!interestingPath) return;
            debugLogCount++;

            string parentName = tmp.transform.parent != null ? tmp.transform.parent.name : "<none>";
            string text = tmp.text ?? "";
            text = text.Replace("\r", " ").Replace("\n", " ");
            if (text.Length > 32) text = text.Substring(0, 32);

            float facingDot = Vector3.Dot(tmp.transform.forward, targetCanvas.transform.forward);
            string mode = HudCurveDebug.IsPopupNotificationPath(tmp.transform, targetCanvas.transform)
                ? (HudCurveDebug.HasPopupVisualRoot(tmp.transform, targetCanvas.transform) ? "popupLocal" : "popupArc")
                : (IsAnimationSensitiveText() ? "relative" : "vertex");
            Mod.logger.LogInfo(
                $"[VRHud/TMP] #{debugLogCount} reason={reason} name='{tmp.name}' parent='{parentName}' text='{text}' " +
                $"radius={radiusPixels:F2} pivot=({pivotCanvas.x:F2},{pivotCanvas.y:F2},{pivotCanvas.z:F2}) " +
                $"scale={localToCanvasScale:F4} euler=({tmp.transform.localEulerAngles.x:F1},{tmp.transform.localEulerAngles.y:F1},{tmp.transform.localEulerAngles.z:F1}) " +
                $"dot={facingDot:F3} verts={vertexCount} " +
                $"mode={mode} " +
                $"beforeX=({beforeMinX:F2},{beforeMaxX:F2}) beforeZ=({beforeMinZ:F2},{beforeMaxZ:F2}) " +
                $"afterX=({afterMinX:F2},{afterMaxX:F2}) afterZ=({afterMinZ:F2},{afterMaxZ:F2}) " +
                $"span=({afterMaxX - afterMinX:F2},{afterMaxZ - afterMinZ:F2})");

            if (!debugPathLogged && targetCanvas != null &&
                (HudCurveDebug.IsStatusIconPath(tmp.transform, targetCanvas.transform) ||
                 HudCurveDebug.IsPopupNotificationPath(tmp.transform, targetCanvas.transform) ||
                 HudCurveDebug.IsPowerIndicatorPath(tmp.transform, targetCanvas.transform)))
            {
                debugPathLogged = true;
                Mod.logger.LogInfo(
                    $"[VRHud/CurveNode] kind=TMP path='{HudCurveDebug.BuildPath(tmp.transform, targetCanvas.transform)}' " +
                    $"chain='{HudCurveDebug.BuildChainSnapshot(tmp.transform, targetCanvas.transform)}'");
            }
        }

        private void EnsureSourceVertices()
        {
            var textInfo = tmp.textInfo;
            if (sourceVertices == null || sourceVertices.Length != textInfo.meshInfo.Length)
                CaptureSourceVertices();
        }

        private void CaptureSourceVertices()
        {
            if (tmp == null) return;
            var textInfo = tmp.textInfo;
            sourceVertices = new Vector3[textInfo.meshInfo.Length][];
            for (int m = 0; m < textInfo.meshInfo.Length; m++)
            {
                var verts = textInfo.meshInfo[m].vertices;
                int count = textInfo.meshInfo[m].vertexCount;
                if (verts == null || count <= 0) continue;
                sourceVertices[m] = new Vector3[count];
                System.Array.Copy(verts, sourceVertices[m], count);
            }
        }

        private float GetLocalToCanvasScale()
        {
            float scale = 1f;
            Transform current = tmp.transform;
            Transform canvasTransform = targetCanvas.transform;
            while (current != null && current != canvasTransform)
            {
                scale *= current.localScale.x;
                current = current.parent;
            }
            return scale;
        }

        private float GetLocalToAncestorScale(Transform ancestor)
        {
            float scale = 1f;
            Transform current = tmp.transform;
            while (current != null && current != ancestor)
            {
                scale *= current.localScale.x;
                current = current.parent;
            }
            return current == ancestor ? scale : 0f;
        }

        private bool IsAnimationSensitiveText()
        {
            return HudCurveDebug.IsAnimationSensitivePath(tmp.transform, targetCanvas != null ? targetCanvas.transform : null);
        }
    }

    #region Patches

    //Handler for Exosuit entering only
    [HarmonyPatch(typeof(Vehicle), nameof(Vehicle.OnPilotModeBegin))]
    public static class SetHudStaticInVehicles
    {
        public static void Postfix(Vehicle __instance)
        {
            Mod.logger.LogInfo("Vehicle.OnPilotModeBegin");
            // TODO: How to check for SeaTruck?
            if (__instance is Exosuit)
            {
                VRHud.OnEnterVehicle();
            }
        }
    }

    //Handler for Exosuit exiting only
    [HarmonyPatch(typeof(Vehicle), nameof(Vehicle.OnPilotModeEnd))]
    public static class ResetHudStaticInVehicles
    {
        public static void Postfix(Vehicle __instance)
        {
            Mod.logger.LogInfo("Vehicle.OnPilotModeEnd {__instance is Exosuit}");
            if (__instance is Exosuit)
            {
                VRHud.OnExitVehicle();
                //This moves player off of the top of the suit after exiting
                Player.main.transform.position = Player.main.transform.position + SNCameraRoot.main.transform.forward * -2.5f;
                Player.main.transform.localPosition += new Vector3(0.0f, 0.5f, 0.0f);
            }
        }
    }

    //handler for Seatruck only entering the docking bay
    [HarmonyPatch(typeof(Dockable), nameof(Dockable.OnDockingComplete))]
    public static class ResetHudStaticWhenDocked
    {
        public static void Postfix(Dockable __instance)
        {
            Mod.logger.LogInfo("Dockable.OnDockingComplete");
            if(__instance.truckMotor)
            {
                VRHud.OnExitVehicle();      
            }
       }
    }

    //handler for Seatruck only exiting the docking bay
    [HarmonyPatch(typeof(Dockable), nameof(Dockable.OnUndockingComplete))]
    public static class ResetHudStaticWhenUndocked
    {
        public static void Postfix(Dockable __instance)
        {
            Mod.logger.LogInfo("Dockable.OnUndockingComplete");
            if(__instance.truckMotor)
            {
                VRHud.OnEnterVehicle();   
            }   
        }
    }

    //handler for Seatruck only entering
    [HarmonyPatch(typeof(SeaTruckMotor), nameof(SeaTruckMotor.StartPiloting))]
    static class SetHudStaticInSeaTrucker
    {
        public static void Postfix()
        {
            Mod.logger.LogInfo("SeaTruckMotor.StartPiloting");
            VRHud.OnEnterVehicle();
        }
    }

    //handler for Seatruck only exiting
    [HarmonyPatch(typeof(SeaTruckMotor), nameof(SeaTruckMotor.StopPiloting))]
    static class ResetHudStaticInSeaTrucker
    {
        public static void Prefix(SeaTruckMotor __instance, bool waitForDocking = false, bool forceStop = false, bool skipUnsubscribe = false, bool immediate = false, bool forceGetupAnimation = false)
        {
            //Mod.logger.LogInfo($"SeaTruckMotor.StopPiloting waitForDocking = {waitForDocking} forceStop = {forceStop} skipUnsubscribe = {skipUnsubscribe} immediate = {immediate} forceGetupAnimation = {forceGetupAnimation}");
            //Doing this check because SeaTruckMotor.StopPiloting gets called many times at startup for an unknown reason
            if (__instance.piloting) 
            {
                Mod.logger.LogInfo($"SeaTruckMotor.StopPiloting");
                VRHud.OnExitVehicle();
            }
        }
    }


    [HarmonyPatch(typeof(uGUI_PlayerSleep), nameof(uGUI_PlayerSleep.Start))]
    public static class ScaleSleep
    {
        public static void Postfix(uGUI_PlayerSleep __instance)
        {
             __instance.blackOverlay.transform.localScale = new Vector3(10f, 10f, 10f);
        }
    }

    [HarmonyPatch(typeof(uGUI_PlayerDeath), nameof(uGUI_PlayerDeath.Start))]
    public static class ScaleDeath
    {
        public static void Postfix(uGUI_PlayerDeath __instance)
        {
             __instance.blackOverlay.transform.localScale = new Vector3(10f, 10f, 10f);
        }
    }

    [HarmonyPatch(typeof(uGUI_Overlays), nameof(uGUI_Overlays.Awake))]
    public static class ScaleOverlays
    {
        public static void Postfix(uGUI_Overlays __instance)
        {
            __instance.gameObject.transform.localScale = new Vector3(10f, 10f, 10f);
        }
    }

    [HarmonyPatch(typeof(EndCreditsManager), nameof(EndCreditsManager.Update))]
    public static class ScaleEndCredits
    {
        public static void Postfix(EndCreditsManager __instance)
        {
             __instance.gameObject.transform.localScale = new Vector3(0.001f, 0.001f, 0.001f);
        }
    }
/*
    [HarmonyPatch(typeof(uGUI_ExpansionIntro), nameof(uGUI_ExpansionIntro.Start))]
    public static class ScaleuGUI_ExpansionIntro
    {
        public static void Postfix(uGUI_ExpansionIntro __instance)
        {
             __instance.gameObject.transform.localScale = new Vector3(1.4f, 1.4f, 1.4f);
        }
    }
*/

    [HarmonyPatch(typeof(uGUI_Pings), nameof(uGUI_Pings.IsVisibleNow))]
    public static class HidePingsWhenHudOff
    {
        public static void Postfix(ref bool __result)
        {
            __result &= WristHud.isHudOn;
        }
    }

    internal static class PingProjectionHelpers
    {
        private const float HudCanvasBaseScale = 0.00085f;
        private const int DebugFrameInterval = 30;
        public const float HorizontalProjectionScale = 1.85f;
        public const float VerticalProjectionScale = 3.5f;
        private static readonly Dictionary<int, PingDebugState> DebugStates = new Dictionary<int, PingDebugState>();

        public struct ArrowDebug
        {
            public bool arrowEnabled;
            public float angleBefore;
            public float targetAngle;
            public float angle;
            public float worldAngle;
            public float dot;
            public bool wouldFlip;
            public Vector2 direction;
            public Vector3 localEuler;
            public Vector3 localScale;
            public Vector3 lossyScale;
        }

        private struct PingDebugState
        {
            public int lastFrame;
            public float lastAngle;
            public Vector2 lastAdjustedRelative;
        }

        public static float GetHudVerticalReferencePixels()
        {
            float scale = HudCanvasBaseScale * Mathf.Max(0.05f, Settings.HudScale);
            return (0.1f + Settings.HudVerticalOffset + Settings.PingVerticalOffset) / scale;
        }

        public static ArrowDebug ApplyFinalArrowAngle(uGUI_Ping ping, Vector2 adjustedRelative)
        {
            var debug = new ArrowDebug
            {
                arrowEnabled = ping.arrow != null && ping.arrow.enabled,
                angleBefore = -1f,
                targetAngle = -1f,
                angle = -1f,
                worldAngle = -1f,
                dot = 0f,
                wouldFlip = false,
                direction = Vector2.zero,
                localEuler = Vector3.zero,
                localScale = Vector3.one,
                lossyScale = Vector3.one
            };

            if (ping.arrow == null || !ping.arrow.enabled || adjustedRelative.sqrMagnitude <= 0.001f)
                return debug;

            RectTransform arrowRect = ping.arrow.rectTransform;
            debug.angleBefore = arrowRect.localEulerAngles.z;
            debug.targetAngle = Mathf.Atan2(adjustedRelative.y, adjustedRelative.x) * Mathf.Rad2Deg;
            if (debug.targetAngle < 0f)
                debug.targetAngle += 360f;
            ping.SetAngle(debug.targetAngle);
            arrowRect.localRotation = Quaternion.Euler(0f, 0f, debug.targetAngle);
            debug.localEuler = arrowRect.localEulerAngles;
            debug.angle = debug.localEuler.z;
            debug.worldAngle = arrowRect.rotation.eulerAngles.z;
            float radians = debug.targetAngle * Mathf.Deg2Rad;
            debug.direction = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));
            debug.dot = Vector2.Dot(debug.direction, adjustedRelative);
            debug.wouldFlip = debug.dot < 0f;
            debug.localScale = arrowRect.localScale;
            debug.lossyScale = arrowRect.lossyScale;
            return debug;
        }

        public static void LogDebug(uGUI_Ping ping, Vector2 originalAnchored, Vector2 halfSize,
            Vector2 displayCenter, float hudRefPx, Vector2 relative, Vector2 adjustedRelative,
            Vector2 adjusted, float horizontalLimit, float verticalLimit, ArrowDebug arrowDebug)
        {
            if (!Settings.PingDebugLogs)
                return;

            bool atEdge =
                Mathf.Abs(adjustedRelative.x) >= horizontalLimit - 1f ||
                Mathf.Abs(adjustedRelative.y) >= verticalLimit - 1f;
            if (!atEdge && !arrowDebug.arrowEnabled)
                return;

            int id = ping.GetInstanceID();
            int frame = Time.frameCount;
            DebugStates.TryGetValue(id, out PingDebugState state);
            bool changed =
                state.lastFrame == 0 ||
                Mathf.Abs(Mathf.DeltaAngle(state.lastAngle, arrowDebug.angle)) > 15f ||
                Vector2.Distance(state.lastAdjustedRelative, adjustedRelative) > 40f ||
                arrowDebug.wouldFlip;
            if (!changed && frame - state.lastFrame < DebugFrameInterval)
                return;

            DebugStates[id] = new PingDebugState
            {
                lastFrame = frame,
                lastAngle = arrowDebug.angle,
                lastAdjustedRelative = adjustedRelative
            };

            Mod.logger.LogInfo(
                $"[VRHud/Ping] frame={frame} id={id} side={GetSide(adjustedRelative, horizontalLimit, verticalLimit)} " +
                $"label='{SafeText(ping.infoText)}' dist='{SafeText(ping.distanceText)}' suffix='{SafeText(ping.suffixText)}' " +
                $"orig={Fmt(originalAnchored)} half={Fmt(halfSize)} displayCenter={Fmt(displayCenter)} hudRefPx={hudRefPx:F1} " +
                $"rel={Fmt(relative)} adjRel={Fmt(adjustedRelative)} final={Fmt(adjusted)} " +
                $"limits=({horizontalLimit:F1},{verticalLimit:F1}) scale=({HorizontalProjectionScale:F2},{VerticalProjectionScale:F2}) " +
                $"edge=({Settings.PingHorizontalEdgeScale:F2},{Settings.PingVerticalEdgeScale:F2}) pingYOffset={Settings.PingVerticalOffset:F3} " +
                $"arrow={arrowDebug.arrowEnabled} angleBefore={arrowDebug.angleBefore:F1} targetAngle={arrowDebug.targetAngle:F1} " +
                $"angle={arrowDebug.angle:F1} worldAngle={arrowDebug.worldAngle:F1} dir={Fmt(arrowDebug.direction)} dot={arrowDebug.dot:F1} wouldFlip={arrowDebug.wouldFlip} " +
                $"arrowLE={Fmt(arrowDebug.localEuler)} arrowLS={Fmt(arrowDebug.localScale)} arrowLossy={Fmt(arrowDebug.lossyScale)}");
        }

        private static string SafeText(TMP_Text text)
        {
            if (text == null || string.IsNullOrEmpty(text.text))
                return "";
            return text.text.Replace("\r", " ").Replace("\n", " ");
        }

        private static string Fmt(Vector2 value) => $"({value.x:F1},{value.y:F1})";

        private static string Fmt(Vector3 value) => $"({value.x:F2},{value.y:F2},{value.z:F2})";

        private static string GetSide(Vector2 relative, float horizontalLimit, float verticalLimit)
        {
            float nx = horizontalLimit > 0f ? Mathf.Abs(relative.x) / horizontalLimit : 0f;
            float ny = verticalLimit > 0f ? Mathf.Abs(relative.y) / verticalLimit : 0f;
            if (nx < 0.98f && ny < 0.98f)
                return "inside";
            if (nx >= ny)
                return relative.x < 0f ? "left" : "right";
            return relative.y < 0f ? "bottom" : "top";
        }
    }

    [HarmonyPatch(typeof(uGUI_Ping), nameof(uGUI_Ping.SetScale))]
    public static class PingProjectionScaleFixer
    {
        public static void Postfix(uGUI_Ping __instance)
        {
            if (__instance == null ||
                (Mathf.Approximately(PingProjectionHelpers.HorizontalProjectionScale, 1f) &&
                 Mathf.Approximately(PingProjectionHelpers.VerticalProjectionScale, 1f) &&
                 Mathf.Approximately(Settings.PingHorizontalEdgeScale, 1f) &&
                 Mathf.Approximately(Settings.PingVerticalEdgeScale, 1f)))
                return;

            RectTransform rectTransform = __instance.rectTransform;
            if (rectTransform == null || !(rectTransform.parent is RectTransform parentRect))
                return;

            Vector2 originalAnchored = rectTransform.anchoredPosition;
            Rect parent = parentRect.rect;
            Vector2 halfSize = new Vector2(parent.width * 0.5f, parent.height * 0.5f);
            float hudRefPx = PingProjectionHelpers.GetHudVerticalReferencePixels();
            Vector2 displayCenter = halfSize + new Vector2(0f, hudRefPx);
            Vector2 relative = originalAnchored - halfSize;
            Vector2 adjustedRelative = new Vector2(
                relative.x * PingProjectionHelpers.HorizontalProjectionScale,
                relative.y * PingProjectionHelpers.VerticalProjectionScale);
            float horizontalLimit = halfSize.x * Mathf.Max(0.05f, Settings.PingHorizontalEdgeScale);
            float verticalLimit = halfSize.y * Mathf.Max(0.05f, Settings.PingVerticalEdgeScale);
            adjustedRelative.x = Mathf.Clamp(adjustedRelative.x, -horizontalLimit, horizontalLimit);
            adjustedRelative.y = Mathf.Clamp(adjustedRelative.y, -verticalLimit, verticalLimit);
            Vector2 adjusted = displayCenter + adjustedRelative;
            rectTransform.anchoredPosition = adjusted;

            var arrowDebug = PingProjectionHelpers.ApplyFinalArrowAngle(__instance, adjustedRelative);
            PingProjectionHelpers.LogDebug(__instance, originalAnchored, halfSize, displayCenter, hudRefPx,
                relative, adjustedRelative, adjusted, horizontalLimit, verticalLimit, arrowDebug);
        }
    }

    [HarmonyPatch(typeof(uGUI_DepthCompass), nameof(uGUI_DepthCompass.GetDepthInfo))]
    public static class HideDepthCompassWhenHudOff
    {
        public static void Postfix(ref uGUI_DepthCompass.DepthMode __result)
        {
            if (!WristHud.isHudOn)
            {
                __result = uGUI_DepthCompass.DepthMode.None;
            }
        }
    }

    [HarmonyPatch(typeof(uGUI_DepthCompass), nameof(uGUI_DepthCompass.IsCompassEnabled))]
    public static class HideDepthCompassCompassWhenHudOff
    {
        public static void Postfix(ref bool __result)
        {
            if (!WristHud.isHudOn)
            {
                __result = false;
            }
        }
    }

    [HarmonyPatch(typeof(uGUI_PinnedRecipes), nameof(uGUI_PinnedRecipes.GetMode))]
    public static class HidePinnedRecipesWhenHudOff
    {
        public static void Postfix(ref uGUI_PinnedRecipes.Mode __result)
        {
            if (!WristHud.isHudOn)
            {
                __result = uGUI_PinnedRecipes.Mode.Off;
            }
        }
    }

    [HarmonyPatch(typeof(uGUI_PowerIndicator), nameof(uGUI_PowerIndicator.IsPowerEnabled))]
    public static class HidePowerWhenHudOff
    {
        public static void Postfix(ref bool __result)
        {
            __result &= WristHud.isHudOn;
        }
    }

    [HarmonyPatch(typeof(uGUI_SeamothHUD), nameof(uGUI_SeamothHUD.Update))]
    public static class HideSeamothHudWhenHudOff
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var m = new CodeMatcher(instructions);
            m.MatchForward(true, new CodeMatch[] {
                new CodeMatch(OpCodes.Stloc_3),
            }).Advance(1).Insert(new CodeInstruction[] {
                new CodeInstruction(OpCodes.Ldloc_3),
                CodeInstruction.LoadField(typeof(WristHud), nameof(WristHud.isHudOn)),
                new CodeInstruction(OpCodes.And),
                new CodeInstruction(OpCodes.Stloc_3),
            });
            return m.InstructionEnumeration();
        }
    }

    [HarmonyPatch(typeof(uGUI_ExosuitHUD), nameof(uGUI_ExosuitHUD.Update))]
    public static class HideExosuitHudWhenHudOff
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var m = new CodeMatcher(instructions);
            m.MatchForward(true, new CodeMatch[] {
                new CodeMatch(OpCodes.Stloc_3),
            }).Advance(1).Insert(new CodeInstruction[] {
                new CodeInstruction(OpCodes.Ldloc_3),
                CodeInstruction.LoadField(typeof(WristHud), nameof(WristHud.isHudOn)),
                new CodeInstruction(OpCodes.And),
                new CodeInstruction(OpCodes.Stloc_3),
            });
            return m.InstructionEnumeration();
        }
    }

    //Make the pause menu a more comfortable scale
    [HarmonyPatch(typeof(IngameMenu), nameof(IngameMenu.Update))]
    class IngameMenu_Scale_Fixer
    {
        public static void Postfix(IngameMenu __instance)
        {
            __instance.transform.localScale = new Vector3(0.0013f , 0.0013f, 0.0013f);
            //__instance.transform.localPosition = SNCameraRoot.main.transform.forward * 1.5f;
       }
    }
    
    //Make the builder menu a more comfortable scale
    [HarmonyPatch(typeof(uGUI_BuilderMenu), nameof(uGUI_BuilderMenu.Update))]
    class uGUI_BuilderMenu_Scale_Fixer
    {
        public static void Postfix(uGUI_BuilderMenu __instance)
        {
            __instance.transform.localScale = new Vector3(0.0013f , 0.0013f, 0.0013f);
        }
    }
    
    //These next two functions eliminate the "squashed" HUD UI that comes from enabling XRSettings
    //by temporarily turning the setting off during execution
    [HarmonyPatch(typeof(uGUI_CanvasScaler), nameof(uGUI_CanvasScaler.UpdateFrustum))]
    static class uGUI_CanvasScalerFrustum_Fixer
    {
        public static bool Prefix()
        {
            XRSettingsEnabled.isEnabled = false;
            return true;
        }
        public static void Postfix(uGUI_CanvasScaler __instance)
        {
            XRSettingsEnabled.isEnabled = true;
            // Re-apply our custom scale to prevent the game's HUD size slider from hiding the UI
            if (VRHud.screenCanvas != null && __instance.transform == VRHud.screenCanvas)
            {
                VRHud.screenCanvas.localScale = new Vector3(0.00072f, 0.00072f, 0.00072f);
            }
        }
    }

    [HarmonyPatch(typeof(uGUI_SafeAreaScaler), nameof(uGUI_SafeAreaScaler.Update))]
    static class uGUI_SafeAreaScaler_Fixer
    {
        public static bool Prefix()
        {
            XRSettingsEnabled.isEnabled = false;
            return true;
        }
        public static void Postfix()
        {
            XRSettingsEnabled.isEnabled = true;
        }
    }

    [HarmonyPatch(typeof(HandReticle), nameof(HandReticle.LateUpdate))]
    static class HandReticle_IconCanvas_Scale_Fixer
    {
        private static int logCount = 0;
        private const int MaxLogs = 24;

        public static void Postfix(HandReticle __instance)
        {
            if (__instance == null || __instance.iconCanvas == null) return;

            Vector3 scale = __instance.iconCanvas.localScale;
            float uniform = Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y));
            if (uniform <= 0.000001f) uniform = 1f;
            Vector3 fixedScale = new Vector3(uniform, uniform, 1f);
            bool changed = (scale - fixedScale).sqrMagnitude > 0.000001f;
            if (changed)
                __instance.iconCanvas.localScale = fixedScale;

            if (logCount < MaxLogs && (changed || logCount < 4))
            {
                logCount++;
                string handText = __instance.textHand ?? string.Empty;
                string useText = __instance.textUse ?? string.Empty;
                Mod.logger.LogInfo(
                    $"[VRHud/HandReticle] #{logCount} iconCanvas scale=({scale.x:F4},{scale.y:F4},{scale.z:F4})->({fixedScale.x:F4},{fixedScale.y:F4},{fixedScale.z:F4}) " +
                    $"icon={__instance.desiredIconType} handText='{handText}' useText='{useText}' targetDistance={__instance.targetDistance:F3}");
            }
        }
    }

    [HarmonyPatch(typeof(uGUI_ScannerIcon), nameof(uGUI_ScannerIcon.LateUpdate))]
    static class ScannerIcon_UniformScale_Fixer
    {
        private static int logCount = 0;
        private const int MaxLogs = 24;

        public static void Postfix(uGUI_ScannerIcon __instance)
        {
            if (__instance == null || __instance.icon == null) return;

            RectTransform rt = __instance.icon.rectTransform;
            Vector3 scale = rt.localScale;
            float uniform = Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y));
            if (uniform <= 0.000001f) uniform = 1f;
            Vector3 fixedScale = new Vector3(uniform, uniform, 1f);
            bool changed = (scale - fixedScale).sqrMagnitude > 0.000001f;
            if (changed)
                rt.localScale = fixedScale;

            if (logCount < MaxLogs && (changed || __instance.show))
            {
                logCount++;
                Mod.logger.LogInfo(
                    $"[VRHud/ScannerIcon] #{logCount} show={__instance.show} scale=({scale.x:F4},{scale.y:F4},{scale.z:F4})->({fixedScale.x:F4},{fixedScale.y:F4},{fixedScale.z:F4}) " +
                    $"seqT={__instance.sequence.t:F3} active={__instance.sequence.active}");
            }
        }
    }

/*
    [HarmonyPatch(typeof(PlayerMask))]
    [HarmonyPatch("Start")]
    internal static class PlayerMask_Start_Patch
    {
        static bool Prefix(PlayerMask __instance)
        {
            Debug.Log($"[ExtraFov] __instance is {__instance.referenceFov}");
            //__instance.referenceFov += 200.0f;
            return true;
        }
    }
*/
    
    //turn off the uicamera during screenshots
    [HarmonyPatch(typeof(HideForScreenshots), nameof(HideForScreenshots.Hide))]
    public static class HideForScreenshotsFix
    {
        public static void Postfix(HideForScreenshots __instance, HideForScreenshots.HideType hide)
        {
            //Mod.logger.LogInfo($"HideForScreenshots.Hide called {hide}");
            
            //HideForScreenshots.HideType.ViewModel is included for InGameMenu. Dont want to hide UI for that.
            if((hide & HideForScreenshots.HideType.HUD) == HideForScreenshots.HideType.HUD && (hide & HideForScreenshots.HideType.ViewModel) == 0)
            {
                VRCameraRig.instance.uiCamera.enabled = false;
            }
            else if(hide == HideForScreenshots.HideType.None)
            {
                VRCameraRig.instance.uiCamera.enabled = true;
            }  
        }
    }

    #endregion

}
