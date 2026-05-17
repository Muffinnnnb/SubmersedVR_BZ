using System.Reflection;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Events;
using Yangrc.VolumeCloud;
using UnityEngine.XR;
using rail;

namespace SubmersedVR
{
    public class Settings
    {
        public delegate void BooleanChanged(bool newValue);
        public delegate void FloatChanged(float newValue);
        public delegate void VoidChanged();

        public static bool IsSnapTurningEnabled;
        public static event BooleanChanged IsSnapTurningEnabledChanged;
        public static float SnapTurningAngle = 45.0f;
        public static event FloatChanged SnapTurningAngleChanged;

        public static bool IsExosuitSnapTurningEnabled;
        public static event BooleanChanged IsExosuitSnapTurningEnabledChanged;
        public static float ExosuitSnapTurningAngle = 45.0f;
        public static event FloatChanged ExosuitSnapTurningAngleChanged;
        public static bool IsSnowBikeSnapTurningEnabled;
        public static event BooleanChanged IsSnowBikeSnapTurningEnabledChanged;
        public static float SnowBikeSnapTurningAngle = 45.0f;
        public static event FloatChanged SnowBikeSnapTurningAngleChanged;

        //The game's default ForwardSprintModifier is 2.0f (PlayerMotor.forwardSprintModifier), but this feels slow in VR
        public static float ForwardSprintModifier = 2.25f;

        public static bool IsDebugEnabled;
        public static event BooleanChanged IsDebugChanged;
       
        public static bool InvertYAxis;
        public static event BooleanChanged InvertYAxisChanged;

        //Ambient Occlusion Settings
        public static bool AOEnabled = false;
        public static string AOMethod = "Post Effect";
        public static string AOSampleCount = "Medium";
        public static string AOPerPixelNormals = "Camera";
        public static float AOIntensity = 1.0f;
        public static float AORadius = 2.0f;
        public static float AOPowerExponent = 1.8f;
        public static float AOBias = 0.05f;
        public static float AOThickness = 1.0f;
        public static bool AODownSample = true;
        public static bool AOCacheAware = true; 
        public static bool AOTemporalFilterEnabled = true;
        public static bool AOTemporalFilterDownsampleEnabled = true; 
        public static float AOTemporalFilterBlending = 0.8f;
        public static float AOTemporalFilterResponse = 0.5f;
        
        public static event VoidChanged AmbientOcclusionSettingsChanged;
        //

        public static bool AlwaysShowControllers;
        public static event BooleanChanged AlwaysShowControllersChanged;

        public static string ShowLaserPointer = "Default";
        //public static event BooleanChanged AlwaysShowLaserPointerChanged;

        public static bool PutHandReticleOnLaserPointer;
        public static event BooleanChanged PutHandReticleOnLaserPointerChanged;

        public static bool PutBarsOnWrist;
        public static event BooleanChanged PutBarsOnWristChanged;

        public static bool FullBody;
        public static event BooleanChanged FullBodyChanged;

        public static float SeaTruckZOffset = 0.0f;
        public static float SeaTruckYOffset = 0.0f;
        public static float SnowBikeZOffset = 0.0f;
        public static float SnowBikeYOffset = 0.0f;
        public static float ExosuitZOffset = 0.0f;
        public static float ExosuitYOffset = 0.0f;
        // public static float HudDistance = 1.0f;
        // public static event FloatChanged HudDistanceChanged;
        public static float PlayerScale = 1.0f;

        // 도보 HUD 설정
        public static float HudVerticalOffset = 0.0f;
        public static event FloatChanged HudVerticalOffsetChanged;
        public static float HudScale = 1.0f;
        public static event FloatChanged HudScaleChanged;
        public static float HudDistance = 0.0f;
        public static event FloatChanged HudDistanceChanged;
        public static float PingProjectionScale = 1.85f;
        public static float PingProjectionVerticalScale = 3.5f;
        public static float PingHorizontalEdgeScale = 1.0f;
        public static float PingVerticalEdgeScale = 1.0f;
        public static float PingVerticalOffset = 0.0f;
        public static bool PingDebugLogs = false;

        // 탑승물 HUD 설정
        public static float VehicleHudVerticalOffset = 0.0f;
        public static event FloatChanged VehicleHudVerticalOffsetChanged;
        public static float VehicleHudScale = 1.0f;
        public static event FloatChanged VehicleHudScaleChanged;
        public static float VehicleHudDistance = 0.0f;
        public static event FloatChanged VehicleHudDistanceChanged;

        // 도보 HUD 추가 옵션
        public static bool HudFollowHead = false;
        public static event BooleanChanged HudFollowHeadChanged;
        public static bool HudCurved = false;
        public static event BooleanChanged HudCurvedChanged;
        public static float HudCurveRadius = 2.0f;
        public static event FloatChanged HudCurveRadiusChanged;

        // 탑승물 HUD 커브 옵션
        public static bool VehicleHudCurved = false;
        public static event BooleanChanged VehicleHudCurvedChanged;
        public static float VehicleHudCurveRadius = 2.0f;
        public static event FloatChanged VehicleHudCurveRadiusChanged;

        // 자막 설정
        public static bool SubtitleSyncWithHud = true;
        public static event BooleanChanged SubtitleSyncWithHudChanged;
        public static float SubtitleVerticalOffset = -0.15f;
        public static event FloatChanged SubtitleVerticalOffsetChanged;
        public static float SubtitleScale = 1.0f;
        public static event FloatChanged SubtitleScaleChanged;
        public static float SubtitleDistance = 0.0f;
        public static event FloatChanged SubtitleDistanceChanged;

        public static bool AutoRecenterOnVehicleEnter = false;

        public static bool EnableGameHaptics = true;
        public static bool EnableUIHaptics = true;

        

        public static bool HandBasedTurning = false;
        public static bool LeftHandBasedTurning = false;
        public static bool ArticulatedHands = true;
        public static bool PhysicalDriving = false;
        public static bool PhysicalLockedGrips = false;

        //Keep Seamoth and Cyclops defaults so VRPhysicalPiloting will compile
        public static float SeamothLeftHorizontalCenterAngle = 0.0f;
        public static float SeamothLeftVerticleCenterAngle = 0.0f;
        public static float SeamothLeftDeadZone = 5.0f;
        public static float SeamothLeftSensitivity = 0.0f;

        public static float SeamothRightHorizontalCenterAngle = 0.0f;
        public static float SeamothRightVerticleCenterAngle = 0.0f;
        public static float SeamothRightDeadZone = 5.0f;

        public static float CyclopsLeftHorizontalCenterAngle = 0.0f;
        public static float CyclopsLeftVerticleCenterAngle = 0.0f;
        public static float CyclopsLeftDeadZone = 5.0f;

        public static float CyclopsRightHorizontalCenterAngle = 0.0f;
        public static float CyclopsRightVerticleCenterAngle = 0.0f;
        public static float CyclopsRightDeadZone = 5.0f;

        public static float SeatruckLeftHorizontalCenterAngle = 0.0f;
        public static float SeatruckLeftVerticleCenterAngle = 0.0f;
        public static float SeatruckLeftDeadZone = 5.0f;
        public static float SeatruckLeftSensitivity = 0.0f;

        public static float SeatruckRightHorizontalCenterAngle = 0.0f;
        public static float SeatruckRightVerticleCenterAngle = 0.0f;
        public static float SeatruckRightDeadZone = 5.0f;
        public static bool SeatruckAltLeftGrip = false;

        public static float ExosuitLeftHorizontalCenterAngle = 0.0f;
        public static float ExosuitLeftVerticleCenterAngle = 0.0f;
        public static float ExosuitLeftDeadZone = 5.0f;

        public static float ExosuitRightHorizontalCenterAngle = 0.0f;
        public static float ExosuitRightVerticleCenterAngle = 0.0f;
        public static float ExosuitRightDeadZone = 5.0f;

        public static float SnowbikeLeftHorizontalCenterAngle = 0.0f;
        public static float SnowbikeLeftVerticleCenterAngle = 0.0f;
        public static float SnowbikeLeftDeadZone = 5.0f;

        public static float SnowbikeRightHorizontalCenterAngle = 0.0f;
        public static float SnowbikeRightVerticleCenterAngle = 0.0f;
        public static float SnowbikeRightDeadZone = 5.0f;
        public static bool SnowbikeAltAccelerator = false;
 
        // Saves or loads all public static properties as settings using the given serializer
        internal static void Serialize(GameSettings.ISerializer serializer)
        {
            string ns = nameof(SubmersedVR);
            foreach (var p in typeof(Settings).GetFields(BindingFlags.Static | BindingFlags.Public))
            {
                string name = p.Name;
                var value = p.GetValue(null);
                switch (value)
                {
                    case bool val:
                        p.SetValue(null, serializer.Serialize($"{ns}/{name}", val));
                        break;
                    case int val:
                        p.SetValue(null, serializer.Serialize($"{ns}/{name}", val));
                        break;
                    case float val:
                        p.SetValue(null, serializer.Serialize($"{ns}/{name}", val));
                        break;
                    case string val:
                        p.SetValue(null, serializer.Serialize($"{ns}/{name}", val));
                        break;
                    case Color32 val:
                        p.SetValue(null, serializer.Serialize($"{ns}/{name}", val));
                        break;
                    default:
                        Mod.logger.LogError($"Can't save/load setting {name} with type {value.GetType()}");
                        break;
                }
            }
        }

        internal static void AddToGraphicsOptions(uGUI_OptionsPanel panel)
        {
            int tab = panel.tabs.Count - 1;

            string space = "   ";
            string aoMethodDisplay = AOMethod == "Deferred" ? "지연 렌더링" : AOMethod == "Debug" ? "디버그" : "후처리";
            string aoSampleDisplay = AOSampleCount == "Low" ? "낮음" : AOSampleCount == "High" ? "높음" : AOSampleCount == "Very High" ? "매우 높음" : "중간";
            string aoNormalsDisplay = AOPerPixelNormals == "None" ? "없음" : AOPerPixelNormals;

            panel.AddHeading(tab, "앰비언트 오클루전");
            panel.AddToggleOption(tab, space + "사용", AOEnabled, (value) => { AOEnabled = AmbientOcclusionVR.enabled = value; AmbientOcclusionSettingsChanged(); }, "앰비언트 오클루전을 사용합니다. GPU 부하가 증가합니다.");
            panel.AddChoiceOption<string>(tab, space + "방식", new string[] {"후처리", "지연 렌더링", "디버그"}, aoMethodDisplay, (value) => {
                AOMethod = value == "지연 렌더링" ? "Deferred" : value == "디버그" ? "Debug" : "Post Effect";
                if (AmbientOcclusionSettingsChanged != null) {
                    AmbientOcclusionSettingsChanged();
                }
            });
            panel.AddChoiceOption<string>(tab, space + "샘플 수", new string[] {"낮음", "중간", "높음", "매우 높음"}, aoSampleDisplay, (value) => {
                AOSampleCount = value == "낮음" ? "Low" : value == "높음" ? "High" : value == "매우 높음" ? "Very High" : "Medium";
                if (AmbientOcclusionSettingsChanged != null) {
                    AmbientOcclusionSettingsChanged();
                }
            });
            panel.AddChoiceOption<string>(tab, space + "픽셀별 노멀", new string[] {"없음", "Camera", "GBuffer", "Octa"}, aoNormalsDisplay, (value) => {
                AOPerPixelNormals = value == "없음" ? "None" : value;
                if (AmbientOcclusionSettingsChanged != null) {
                    AmbientOcclusionSettingsChanged();
                }
            });
            panel.AddSliderOption(tab, space + "강도", AOIntensity, 0f, 1.0f, AOIntensity, 0.02f, (value) => { AOIntensity = value; AmbientOcclusionSettingsChanged(); }, SliderLabelMode.Float, "0.00");
            panel.AddSliderOption(tab, space + "반경", AORadius, 0f, 10.0f, AORadius, 0.1f, (value) => { AORadius = value; AmbientOcclusionSettingsChanged(); }, SliderLabelMode.Float, "0.0");
            panel.AddSliderOption(tab, space + "파워 지수", AOPowerExponent, 0f, 16f, AOPowerExponent, 0.1f, (value) => { AOPowerExponent = value; AmbientOcclusionSettingsChanged(); }, SliderLabelMode.Float, "0.0");
            panel.AddSliderOption(tab, space + "바이어스", AOBias, 0f, 0.99f, AOBias, 0.02f, (value) => { AOBias = value; AmbientOcclusionSettingsChanged(); }, SliderLabelMode.Float, "0.00");
            panel.AddSliderOption(tab, space + "두께", AOThickness, 0f, 1.0f, AOThickness, 0.02f, (value) => { AOThickness = value; AmbientOcclusionSettingsChanged(); }, SliderLabelMode.Float, "0.00");
            panel.AddToggleOption(tab, space + "다운샘플", AODownSample, (value) => { AODownSample = value; AmbientOcclusionSettingsChanged(); }, "오클루전과 블러를 절반 해상도로 계산합니다.");
            panel.AddToggleOption(tab, space + "캐시 최적화", AOCacheAware, (value) => { AOCacheAware = value; AmbientOcclusionSettingsChanged(); }, "성능과 품질 균형을 위한 캐시 최적화입니다.");
            panel.AddToggleOption(tab, space + "시간 필터 사용", AOTemporalFilterEnabled, (value) => { AOTemporalFilterEnabled = AmbientOcclusionVR.FilterEnabled = value; AmbientOcclusionSettingsChanged(); }, "시간에 따라 효과를 누적합니다.");
            panel.AddToggleOption(tab, space + "시간 필터 다운샘플", AOTemporalFilterDownsampleEnabled, (value) => { AOTemporalFilterDownsampleEnabled = value; AmbientOcclusionSettingsChanged(); }, "시간 필터를 절반 해상도로 적용합니다.");
            panel.AddSliderOption(tab, space + "시간 필터 블렌딩", AOTemporalFilterBlending, 0f, 1.0f, AOTemporalFilterBlending, 0.02f, (value) => { AOTemporalFilterBlending = value; AmbientOcclusionSettingsChanged(); }, SliderLabelMode.Float, "0.00");
            panel.AddSliderOption(tab, space + "시간 필터 반응", AOTemporalFilterResponse, 0f, 1.0f, AOTemporalFilterResponse, 0.02f, (value) => { AOTemporalFilterResponse = value; AmbientOcclusionSettingsChanged(); }, SliderLabelMode.Float, "0.00");


        }

        internal static void AddMenu(uGUI_OptionsPanel panel)
        {
            int tab = panel.AddTab("Submersed VR");

            string movementModeDisplay = HandBasedTurning ? (LeftHandBasedTurning ? "왼손 기준" : "오른손 기준") : "머리 기준";
            string laserPointerDisplay = ShowLaserPointer == "Always" ? "항상" : ShowLaserPointer == "Never" ? "끄기" : "기본";

            panel.AddHeading(tab, "조작");
            panel.AddChoiceOption<string>(tab, "이동 기준", new string[] {"머리 기준", "오른손 기준", "왼손 기준"}, movementModeDisplay, (value) => {
                HandBasedTurning = value == "오른손 기준" || value == "왼손 기준";
                LeftHandBasedTurning = value == "왼손 기준";
            });
            panel.AddToggleOption(tab, "플레이어 스냅 회전 사용", IsSnapTurningEnabled, (value) => { IsSnapTurningEnabled = value;
                if (IsSnapTurningEnabledChanged != null) {
                    IsSnapTurningEnabledChanged(value);
                }
            });
            panel.AddChoiceOption<float>(tab, "플레이어 스냅 회전 각도(°)", new float[] {22.5f, 30f, 45f, 90f}, SnapTurningAngle, (value) => {
                SnapTurningAngle = value;
                if (SnapTurningAngleChanged != null) {
                    SnapTurningAngleChanged(value);
                }
            });
            panel.AddToggleOption(tab, "프론 슈트 스냅 회전 사용", IsExosuitSnapTurningEnabled, (value) => { IsExosuitSnapTurningEnabled = value;
                if (IsExosuitSnapTurningEnabledChanged != null) {
                    IsExosuitSnapTurningEnabledChanged(value);
                }
            });
            panel.AddChoiceOption<float>(tab, "프론 슈트 스냅 회전 각도(°)", new float[] {22.5f, 30f, 45f, 90f}, ExosuitSnapTurningAngle, (value) => {
                ExosuitSnapTurningAngle = value;
                if (ExosuitSnapTurningAngleChanged != null) {
                    ExosuitSnapTurningAngleChanged(value);
                }
            });
           panel.AddToggleOption(tab, "스노우폭스 스냅 회전 사용", IsSnowBikeSnapTurningEnabled, (value) => { IsSnowBikeSnapTurningEnabled = value;
                if (IsSnowBikeSnapTurningEnabledChanged != null) {
                    IsSnowBikeSnapTurningEnabledChanged(value);
                }
            });
            panel.AddChoiceOption<float>(tab, "스노우폭스 스냅 회전 각도(°)", new float[] {22.5f, 30f, 45f, 90f}, SnowBikeSnapTurningAngle, (value) => {
                SnowBikeSnapTurningAngle = value;
                if (SnowBikeSnapTurningAngleChanged != null) {
                    SnowBikeSnapTurningAngleChanged(value);
                }
            });

            panel.AddSliderOption(tab, "질주 속도 배율", ForwardSprintModifier, 2f, 3f, ForwardSprintModifier, 0.25f, (value) =>
            {
                ForwardSprintModifier = value;
                //If the player is currently in-game, immediately apply the new value to PlayerMotor to update sprint speed in real time
                Sprint.OnForwardSprintModifierChanged();
            }, SliderLabelMode.Float, "0.00");

            panel.AddHeading(tab, "몰입");
            panel.AddToggleOption(tab, "손가락 애니메이션", ArticulatedHands, (value) => { ArticulatedHands = value;  }, "실제 손 움직임에 맞춰 게임 손 애니메이션을 재생합니다.");
            panel.AddToggleOption(tab, "게임 햅틱 사용", EnableGameHaptics, (value) => { EnableGameHaptics = value; }, "월드 오브젝트와 상호작용할 때 컨트롤러 진동을 사용합니다.");
            panel.AddToggleOption(tab, "UI 햅틱 사용", EnableUIHaptics, (value) => { EnableUIHaptics = value; }, "UI와 상호작용할 때 컨트롤러 진동을 사용합니다.");
            panel.AddToggleOption(tab, "생존 상태를 왼손목에 표시", PutBarsOnWrist, (value) => { PutBarsOnWrist = value; PutBarsOnWristChanged(value); });
            panel.AddChoiceOption<string>(tab, "레이저 포인터 표시", new string[] {"항상", "기본", "끄기"}, laserPointerDisplay, (value) => {
                ShowLaserPointer = value == "항상" ? "Always" : value == "끄기" ? "Never" : "Default";
            });
            panel.AddSliderOption(tab, "HUD 상하 위치", HudVerticalOffset, -0.3f, 0.3f, HudVerticalOffset, 0.01f, (value) => {
                HudVerticalOffset = value;
                HudVerticalOffsetChanged?.Invoke(value);
            }, SliderLabelMode.Float, "0.00", "게임 HUD를 VR 헤드셋 기준 위아래로 이동합니다.");
            panel.AddSliderOption(tab, "HUD 크기", HudScale, 0.5f, 2.0f, HudScale, 0.05f, (value) => {
                HudScale = value;
                HudScaleChanged?.Invoke(value);
            }, SliderLabelMode.Float, "0.00", "게임 HUD의 크기를 조절합니다.");
            panel.AddSliderOption(tab, "HUD 거리", HudDistance, -0.5f, 1.0f, HudDistance, 0.05f, (value) => {
                HudDistance = value;
                HudDistanceChanged?.Invoke(value);
            }, SliderLabelMode.Float, "0.00", "게임 HUD를 더 가깝게 또는 멀리 이동합니다.");
            panel.AddSliderOption(tab, "핑 상하 위치 조정", PingVerticalOffset, -0.2f, 0.2f, PingVerticalOffset, 0.005f, (value) => {
                PingVerticalOffset = value;
            }, SliderLabelMode.Float, "0.000", "신호기/핑 전체 위치를 위아래로 미세 조정합니다. 움직임 배율에는 영향을 주지 않습니다.");
            panel.AddSliderOption(tab, "핑 좌우 가장자리 범위", PingHorizontalEdgeScale, 0.5f, 2.0f, PingHorizontalEdgeScale, 0.05f, (value) => {
                PingHorizontalEdgeScale = value;
            }, SliderLabelMode.Float, "0.00", "신호기/핑이 좌우 가장자리에서 막히는 범위입니다. 너무 넓으면 내리고, 너무 빨리 막히면 올리세요.");
            panel.AddSliderOption(tab, "핑 상하 가장자리 범위", PingVerticalEdgeScale, 0.5f, 2.5f, PingVerticalEdgeScale, 0.05f, (value) => {
                PingVerticalEdgeScale = value;
            }, SliderLabelMode.Float, "0.00", "신호기/핑이 상하 가장자리에서 막히는 범위입니다. 너무 빨리 막히면 올리세요.");
            panel.AddChoiceOption<string>(tab, "HUD 고정 방식", new string[] { "몸 기준", "머리 기준" }, HudFollowHead ? "머리 기준" : "몸 기준", (value) => {
                HudFollowHead = value == "머리 기준";
                HudFollowHeadChanged?.Invoke(HudFollowHead);
            });
            panel.AddChoiceOption<string>(tab, "HUD 표시 방식", new string[] { "평면", "커브드" }, HudCurved ? "커브드" : "평면", (value) => {
                HudCurved = value == "커브드";
                HudCurvedChanged?.Invoke(HudCurved);
            });
            panel.AddSliderOption(tab, "HUD 곡률 반경", HudCurveRadius, 0.5f, 5.0f, HudCurveRadius, 0.1f, (value) => {
                HudCurveRadius = value;
                HudCurveRadiusChanged?.Invoke(value);
            }, SliderLabelMode.Float, "0.0", "미터 단위 곡률 반경입니다. 값이 작을수록 더 많이 굽습니다.");

            panel.AddHeading(tab, "자막");
            panel.AddToggleOption(tab, "자막을 도보 HUD와 동기화", SubtitleSyncWithHud, (value) => {
                SubtitleSyncWithHud = value;
                SubtitleSyncWithHudChanged?.Invoke(value);
            }, "자막 캔버스 위치와 크기를 도보 HUD 설정과 연결합니다.");
            panel.AddSliderOption(tab, "자막 상하 위치", SubtitleVerticalOffset, -0.5f, 0.3f, SubtitleVerticalOffset, 0.01f, (value) => {
                SubtitleVerticalOffset = value;
                SubtitleVerticalOffsetChanged?.Invoke(value);
            }, SliderLabelMode.Float, "0.00", "도보 HUD와 동기화하지 않을 때 자막을 위아래로 이동합니다.");
            panel.AddSliderOption(tab, "자막 크기", SubtitleScale, 0.5f, 2.0f, SubtitleScale, 0.05f, (value) => {
                SubtitleScale = value;
                SubtitleScaleChanged?.Invoke(value);
            }, SliderLabelMode.Float, "0.00", "도보 HUD와 동기화하지 않을 때 자막 캔버스 크기를 조절합니다.");
            panel.AddSliderOption(tab, "자막 거리", SubtitleDistance, -0.5f, 1.0f, SubtitleDistance, 0.05f, (value) => {
                SubtitleDistance = value;
                SubtitleDistanceChanged?.Invoke(value);
            }, SliderLabelMode.Float, "0.00", "도보 HUD와 동기화하지 않을 때 자막 캔버스 깊이를 조절합니다.");

            panel.AddHeading(tab, "실험 기능");
            panel.AddToggleOption(tab, "전신 표시", FullBody, (value) => { FullBody = value; FullBodyChanged(value); }, "손과 발만 보지 않고 전신을 표시합니다.");
            panel.AddSliderOption(tab, "몸 크기", PlayerScale, 0.8f, 1.2f, PlayerScale, 0.01f, (value) => { PlayerScale = value; }, SliderLabelMode.Float, "0.00");
            panel.AddToggleOption(tab, "손 아이콘을 레이저 끝에 표시", PutHandReticleOnLaserPointer, (value) => { PutHandReticleOnLaserPointer = value; PutHandReticleOnLaserPointerChanged(value); });
            panel.AddToggleOption(tab, "시모스/카메라 Y축 반전", InvertYAxis, (value) => { InvertYAxis = value; InvertYAxisChanged(value); }, "시모스와 카메라 조작의 Y축을 반전합니다.");

            panel.AddHeading(tab, "고급 VR 설정(멀미 위험)");
            panel.AddToggleOption(tab, "잠수 중 상하 회전 사용", !VROptions.disableInputPitch, (value) => { VROptions.disableInputPitch = !value; }, "잠수 중 오른쪽 스틱으로 위아래를 바라보게 합니다. 어지러울 수 있어 기본적으로 끄는 것을 권장합니다.");
            panel.AddToggleOption(tab, "데스크톱 컷신 사용", VROptions.enableCinematics, (value) => { VROptions.enableCinematics = value; }, "게임 기본 컷신을 사용합니다. 머리가 강제로 움직일 수 있어 멀미를 유발할 수 있습니다.");
            panel.AddToggleOption(tab, "인트로 건너뛰기", VROptions.skipIntro, (value) => { VROptions.skipIntro = value; }, "새 게임 시작 시 인트로를 건너뜁니다.");

            panel.AddHeading(tab, "디버그");
            panel.AddToggleOption(tab, "디버그 오버레이", IsDebugEnabled, (value) => { IsDebugEnabled = value; IsDebugChanged(value); }, "디버그 오버레이와 로그를 사용합니다.");
            panel.AddToggleOption(tab, "핑 디버그 로그", PingDebugLogs, (value) => { PingDebugLogs = value; }, "신호기/핑 화살표 진단 로그를 출력합니다. 문제 재현할 때만 켜세요.");
            panel.AddToggleOption(tab, "컨트롤러 항상 표시", AlwaysShowControllers, (value) => { AlwaysShowControllers = value; AlwaysShowControllersChanged(value); }, "컨트롤러를 항상 표시합니다.");
            //panel.AddToggleOption(tab, "Always show laserpointer", AlwaysShowLaserPointer, (value) => { AlwaysShowLaserPointer = value; AlwaysShowLaserPointerChanged(value); }, "Show the laserpointer at all times.");

            tab = panel.AddTab("탑승물 VR");
            panel.AddHeading(tab, "탑승물 HUD");
            panel.AddSliderOption(tab, "상하 위치", VehicleHudVerticalOffset, -0.3f, 0.3f, VehicleHudVerticalOffset, 0.01f, (value) => {
                VehicleHudVerticalOffset = value;
                VehicleHudVerticalOffsetChanged?.Invoke(value);
            }, SliderLabelMode.Float, "0.00", "탑승물 HUD를 VR 헤드셋 기준 위아래로 이동합니다.");
            panel.AddSliderOption(tab, "크기", VehicleHudScale, 0.5f, 2.0f, VehicleHudScale, 0.05f, (value) => {
                VehicleHudScale = value;
                VehicleHudScaleChanged?.Invoke(value);
            }, SliderLabelMode.Float, "0.00", "탑승물 HUD의 크기를 조절합니다.");
            panel.AddSliderOption(tab, "거리", VehicleHudDistance, -0.5f, 1.0f, VehicleHudDistance, 0.05f, (value) => {
                VehicleHudDistance = value;
                VehicleHudDistanceChanged?.Invoke(value);
            }, SliderLabelMode.Float, "0.00", "탑승물 HUD를 더 가깝게 또는 멀리 이동합니다.");
            panel.AddChoiceOption<string>(tab, "표시 방식", new string[] { "평면", "커브드" }, VehicleHudCurved ? "커브드" : "평면", (value) => {
                VehicleHudCurved = value == "커브드";
                VehicleHudCurvedChanged?.Invoke(VehicleHudCurved);
            });
            panel.AddSliderOption(tab, "곡률 반경", VehicleHudCurveRadius, 0.5f, 5.0f, VehicleHudCurveRadius, 0.1f, (value) => {
                VehicleHudCurveRadius = value;
                VehicleHudCurveRadiusChanged?.Invoke(value);
            }, SliderLabelMode.Float, "0.0", "미터 단위 곡률 반경입니다. 값이 작을수록 더 많이 굽습니다.");
            panel.AddHeading(tab, "편의");
            panel.AddToggleOption(tab, "탑승/컷신 진입 시 자동 리센터", AutoRecenterOnVehicleEnter, (value) => {
                AutoRecenterOnVehicleEnter = value;
            }, "탑승물이나 컷신에 들어갈 때 VR 추적을 자동으로 리센터합니다.");
            panel.AddSliderOption(tab, "시트럭 조종석 앞뒤 위치", SeaTruckZOffset, -0.4f, 0.4f, SeaTruckZOffset, 0.01f, (value) => { SeaTruckZOffset = value; }, SliderLabelMode.Float, "0.00");
            panel.AddSliderOption(tab, "시트럭 조종석 높이", SeaTruckYOffset, -0.4f, 0.4f, SeaTruckYOffset, 0.01f, (value) => { SeaTruckYOffset = value; }, SliderLabelMode.Float, "0.00");
            panel.AddSliderOption(tab, "프론 슈트 앞뒤 위치", ExosuitZOffset, -0.4f, 0.4f, ExosuitZOffset, 0.01f, (value) => { ExosuitZOffset = value; }, SliderLabelMode.Float, "0.00");
            panel.AddSliderOption(tab, "프론 슈트 높이", ExosuitYOffset, -0.4f, 0.4f, ExosuitYOffset, 0.01f, (value) => { ExosuitYOffset = value; }, SliderLabelMode.Float, "0.00");
            panel.AddSliderOption(tab, "스노우폭스 앞뒤 위치", SnowBikeZOffset, -0.4f, 0.4f, SnowBikeZOffset, 0.01f, (value) => { SnowBikeZOffset = value; }, SliderLabelMode.Float, "0.00");
            panel.AddSliderOption(tab, "스노우폭스 높이", SnowBikeYOffset, -0.2f, 0.4f, SnowBikeYOffset, 0.01f, (value) => { SnowBikeYOffset = value; }, SliderLabelMode.Float, "0.00");
        

            panel.AddHeading(tab, "탑승물 조작");
            panel.AddToggleOption(tab, "물리 운전", PhysicalDriving, (value) => { PhysicalDriving = value;  }, "탑승물 조작부를 잡아서 조향합니다.");
            panel.AddToggleOption(tab, "조향 손 고정", PhysicalLockedGrips, (value) => { PhysicalLockedGrips = value;  }, "조작부를 잡으면 손이 고정되어 계속 잡고 있을 필요가 없습니다. 다시 잡으면 해제됩니다.");
            
            panel.AddHeading(tab, "시트럭 왼손");
            panel.AddSliderOption(tab, "중심 (좌/우)", SeatruckLeftHorizontalCenterAngle, -10f, 10f, SeatruckLeftHorizontalCenterAngle, 1f, (value) => { SeatruckLeftHorizontalCenterAngle = value; }, SliderLabelMode.Float, "0");
            panel.AddSliderOption(tab, "중심 (상/하)", SeatruckLeftVerticleCenterAngle, -10f, 10f, SeatruckLeftVerticleCenterAngle, 1f, (value) => { SeatruckLeftVerticleCenterAngle = value; }, SliderLabelMode.Float, "0");
            //Call dead zone "Sensitivity" for users
            panel.AddSliderOption(tab, "감도", SeatruckLeftDeadZone, 1f, 10f, SeatruckLeftDeadZone, 1f, (value) => { SeatruckLeftDeadZone = value; }, SliderLabelMode.Float, "0", "값이 높을수록 더 빠르게 회전합니다.");
            //panel.AddSliderOption(tab, "Sensitivity", SeamothLeftSensitivity, 0f, 100f, SeamothLeftSensitivity, 1f, (value) => { SeamothLeftSensitivity = value; }, SliderLabelMode.Float, "0");
            panel.AddToggleOption(tab, "세로 그립 사용", SeatruckAltLeftGrip, (value) => { SeatruckAltLeftGrip = value;  }, "가로 그립 대신 세로 손잡이를 사용합니다.");

            panel.AddHeading(tab, "시트럭 오른손");
            panel.AddSliderOption(tab, "중심 (좌/우)", SeatruckRightHorizontalCenterAngle, -10f, 10f, SeatruckRightHorizontalCenterAngle, 1f, (value) => { SeatruckRightHorizontalCenterAngle = value; }, SliderLabelMode.Float, "0");
            panel.AddSliderOption(tab, "중심 (상/하)", SeatruckRightVerticleCenterAngle, -10f, 10f, SeatruckRightVerticleCenterAngle, 1f, (value) => { SeatruckRightVerticleCenterAngle = value; }, SliderLabelMode.Float, "0");
            //Call dead zone "Sensitivity" for users
            panel.AddSliderOption(tab, "감도",SeatruckRightDeadZone, 1f, 10f, SeatruckRightDeadZone, 1f, (value) => { SeatruckRightDeadZone = value; }, SliderLabelMode.Float, "0", "값이 높을수록 더 빠르게 회전합니다.");
            //panel.AddSliderOption(tab, "Sensitivity", SeamothRightSensitivity, 0f, 100f, SeamothRightSensitivity, 1f, (value) => { SeamothRightSensitivity = value; }, SliderLabelMode.Float, "0");

            panel.AddHeading(tab, "프론 슈트 왼손");
            panel.AddSliderOption(tab, "중심 (좌/우)", ExosuitLeftHorizontalCenterAngle, -10f, 10f, ExosuitLeftHorizontalCenterAngle, 1f, (value) => { ExosuitLeftHorizontalCenterAngle = value; }, SliderLabelMode.Float, "0");
            panel.AddSliderOption(tab, "중심 (상/하)", ExosuitLeftVerticleCenterAngle, -10f, 10f, ExosuitLeftVerticleCenterAngle, 1f, (value) => { ExosuitLeftVerticleCenterAngle = value; }, SliderLabelMode.Float, "0");
            //Call dead zone "Sensitivity" for users
            panel.AddSliderOption(tab, "감도", ExosuitLeftDeadZone, 1f, 10f, ExosuitLeftDeadZone, 1f, (value) => { ExosuitLeftDeadZone = value; }, SliderLabelMode.Float, "0", "값이 높을수록 더 빠르게 회전합니다.");
            //panel.AddSliderOption(tab, "Sensitivity", SeamothLeftSensitivity, 0f, 100f, SeamothLeftSensitivity, 1f, (value) => { SeamothLeftSensitivity = value; }, SliderLabelMode.Float, "0");

            panel.AddHeading(tab, "프론 슈트 오른손");
            panel.AddSliderOption(tab, "중심 (좌/우)", ExosuitRightHorizontalCenterAngle, -10f, 10f, ExosuitRightHorizontalCenterAngle, 1f, (value) => { ExosuitRightHorizontalCenterAngle = value; }, SliderLabelMode.Float, "0");
            panel.AddSliderOption(tab, "중심 (상/하)", ExosuitRightVerticleCenterAngle, -10f, 10f, ExosuitRightVerticleCenterAngle, 1f, (value) => { ExosuitRightVerticleCenterAngle = value; }, SliderLabelMode.Float, "0");
            //Call dead zone "Sensitivity" for users
            panel.AddSliderOption(tab, "감도", ExosuitRightDeadZone, 1f, 10f, ExosuitRightDeadZone, 1f, (value) => { ExosuitRightDeadZone = value; }, SliderLabelMode.Float, "0", "값이 높을수록 더 빠르게 회전합니다.");
            //panel.AddSliderOption(tab, "Sensitivity", SeamothRightSensitivity, 0f, 100f, SeamothRightSensitivity, 1f, (value) => { SeamothRightSensitivity = value; }, SliderLabelMode.Float, "0");

            panel.AddHeading(tab, "스노우폭스 왼손");
            //panel.AddSliderOption(tab, "Center (Left/Right)", SnowbikeLeftHorizontalCenterAngle, -10f, 10f, SnowbikeLeftHorizontalCenterAngle, 1f, (value) => { SnowbikeLeftHorizontalCenterAngle = value; }, SliderLabelMode.Float, "0");
            //panel.AddSliderOption(tab, "Center (Up/Down)", SnowbikeLeftVerticleCenterAngle, -10f, 10f, SnowbikeLeftVerticleCenterAngle, 1f, (value) => { SnowbikeLeftVerticleCenterAngle = value; }, SliderLabelMode.Float, "0");
            //Call dead zone "Sensitivity" for users
            panel.AddSliderOption(tab, "감도", SnowbikeLeftDeadZone, 1f, 10f, SnowbikeLeftDeadZone, 1f, (value) => { SnowbikeLeftDeadZone = value; }, SliderLabelMode.Float, "0", "값이 높을수록 더 빠르게 회전합니다.");
            //panel.AddSliderOption(tab, "Sensitivity", SeamothLeftSensitivity, 0f, 100f, SeamothLeftSensitivity, 1f, (value) => { SeamothLeftSensitivity = value; }, SliderLabelMode.Float, "0");

            panel.AddHeading(tab, "스노우폭스 오른손");
            //panel.AddSliderOption(tab, "Center (Left/Right)", SnowbikeRightHorizontalCenterAngle, -10f, 10f, SnowbikeRightHorizontalCenterAngle, 1f, (value) => { SnowbikeRightHorizontalCenterAngle = value; }, SliderLabelMode.Float, "0");
            panel.AddSliderOption(tab, "중심 (가속)", SnowbikeRightVerticleCenterAngle, -10f, 10f, SnowbikeRightVerticleCenterAngle, 1f, (value) => { SnowbikeRightVerticleCenterAngle = value; }, SliderLabelMode.Float, "0");
            //Call dead zone "Sensitivity" for users
            panel.AddSliderOption(tab, "감도", SnowbikeRightDeadZone, 1f, 10f, SnowbikeRightDeadZone, 1f, (value) => { SnowbikeRightDeadZone = value; }, SliderLabelMode.Float, "0", "값이 높을수록 더 빠르게 회전합니다.");
            //panel.AddSliderOption(tab, "Sensitivity", SeamothRightSensitivity, 0f, 100f, SeamothRightSensitivity, 1f, (value) => { SeamothRightSensitivity = value; }, SliderLabelMode.Float, "0");
            panel.AddToggleOption(tab, "가속 반전", SnowbikeAltAccelerator, (value) => { SnowbikeAltAccelerator = value;  }, "앞으로 비틀어 가속합니다.");

        }
    }

    #region Patches

    // This enables the mod to save and load settings, by serializing our settings from the class above.
    [HarmonyPatch(typeof(GameSettings), nameof(GameSettings.SerializeSettings))]
    static class SerializeModSettings
    {
        public static void Postfix(GameSettings.ISerializer serializer)
        {
            Settings.Serialize(serializer);
        }
    }

    [HarmonyPatch(typeof(uGUI_OptionsPanel), nameof(uGUI_OptionsPanel.Update))]
    static class AlwaysEnableBackButton
    {
        public static void Postfix(uGUI_OptionsPanel __instance)
        {
            __instance.UpdateButtonState(__instance.backButton, true);
        }
    }

    // Save the advanced VR Settings
    [HarmonyPatch(typeof(GameSettings), nameof(GameSettings.SerializeVRSettings))]
    static class SerializeAdvancedVRSettings
    {
        public static void Postfix(GameSettings.ISerializer serializer)
        {
            VROptions.enableCinematics = serializer.Serialize($"VR/{nameof(VROptions.enableCinematics)}", VROptions.enableCinematics);
            VROptions.disableInputPitch = serializer.Serialize($"VR/{nameof(VROptions.disableInputPitch)}", VROptions.disableInputPitch);
            VROptions.skipIntro = serializer.Serialize($"VR/{nameof(VROptions.skipIntro)}", VROptions.skipIntro);
        }
    }

    // This hooks into the tab creation to create the options menu.
    [HarmonyPatch(typeof(uGUI_OptionsPanel), nameof(uGUI_OptionsPanel.AddTabs))]
    static class CreateOptionsTab
    {
        public static void Postfix(uGUI_OptionsPanel __instance)
        {
            Settings.AddMenu(__instance);
        }
    }

    // The next two function add back in the ability to toggle fullscreen on the flatscreen display.
    [HarmonyPatch(typeof(uGUI_OptionsPanel), nameof(uGUI_OptionsPanel.AddGeneralTab))]
    static class ReAddFullscreenOption
    {
        public static void Postfix(uGUI_OptionsPanel __instance)
        {
			__instance.AddToggleOption(__instance.tabs.Count - 1, "전체 화면", Screen.fullScreen, new UnityAction<bool>(__instance.OnFullscreenChanged), null);
        }
    }

    [HarmonyPatch(typeof(uGUI_OptionsPanel), nameof(uGUI_OptionsPanel.AddGraphicsTab))]
    static class UpdateGraphicsOptions
    {
        //Get rid of the default Ambient Occlusion Option
        public static bool Prefix(uGUI_OptionsPanel __instance)
        {
            int tabIndex = __instance.AddTab("그래픽");
            __instance.AddSliderOption(tabIndex, "감마", GammaCorrection.gamma, 0.1f, 2.8f, 1f, 0.01f, delegate(float value)
            {
                GammaCorrection.gamma = value;
            }, SliderLabelMode.Float, "0.00", null);
            int qualityPresetIndex = __instance.GetQualityPresetIndex();
            __instance.qualityPresetOption = __instance.AddChoiceOption(tabIndex, "프리셋", uGUI_OptionsPanel.presetOptions, qualityPresetIndex, new UnityAction<int>(__instance.OnQualityPresetChanged), null);
            __instance.ApplyQualityPreset(qualityPresetIndex);
            __instance.AddHeading(tabIndex, "고급");
            if (uGUI_MainMenu.main)
            {
                int currentIndex;
                string[] detailOptions = uGUI_OptionsPanel.GetDetailOptions(out currentIndex);
                __instance.detailOption = __instance.AddChoiceOption(tabIndex, "세부 묘사", detailOptions, currentIndex, new UnityAction<int>(__instance.OnDetailChanged), null);
            }
            __instance.waterQualityOption = __instance.AddChoiceOption<WaterSurface.Quality>(tabIndex, "물 품질", WaterSurface.GetQualityOptions(), WaterSurface.GetQuality(), new UnityAction<WaterSurface.Quality>(__instance.OnWaterQualityChanged), null);
            __instance.skyboxQualityOption = __instance.AddChoiceOption(tabIndex, "하늘 품질", uGUI_OptionsPanel.skyboxQualityOptions, VolumeCloudRenderer.GetQuality(), new UnityAction<int>(__instance.OnASkyboxqualityChanged), null);
            int currentIndex2;
            string[] antiAliasingOptions = uGUI_OptionsPanel.GetAntiAliasingOptions(out currentIndex2);
            __instance.aaModeOption = __instance.AddChoiceOption(tabIndex, "안티앨리어싱", antiAliasingOptions, currentIndex2, new UnityAction<int>(__instance.OnAAmodeChanged), null);
            __instance.aaQualityOption = __instance.AddChoiceOption(tabIndex, "안티앨리어싱 품질", uGUI_OptionsPanel.postFXQualityNames, UwePostProcessingManager.GetAaQuality(), new UnityAction<int>(__instance.OnAAqualityChanged), null);
            __instance.bloomOption = __instance.AddToggleOption(tabIndex, "블룸", UwePostProcessingManager.GetBloomEnabled(), new UnityAction<bool>(__instance.OnBloomChanged), null);
            if (!XRSettings.enabled)
            {
                __instance.lensDirtOption = __instance.AddToggleOption(tabIndex, "렌즈 먼지", UwePostProcessingManager.GetBloomLensDirtEnabled(), new UnityAction<bool>(__instance.OnBloomLensDirtChanged), null);
                if (!GraphicsUtil.IsOpenGL())
                {
                    __instance.dofOption = __instance.AddToggleOption(tabIndex, "피사계 심도", UwePostProcessingManager.GetDofEnabled(), new UnityAction<bool>(__instance.OnDofChanged), null);
                }
                __instance.motionBlurQualityOption = __instance.AddChoiceOption(tabIndex, "모션 블러 품질", uGUI_OptionsPanel.postFXQualityNames, UwePostProcessingManager.GetMotionBlurQuality(), new UnityAction<int>(__instance.OnMotionBlurQualityChanged), null);
            }
            //__instance.aoQualityOption = __instance.AddChoiceOption(tabIndex, "AmbientOcclusion", uGUI_OptionsPanel.postFXQualityNames, UwePostProcessingManager.GetAoQuality(), new UnityAction<int>(this.OnAOqualityChanged), null);
            if (!XRSettings.enabled)
            {
                __instance.ssrQualityOption = __instance.AddChoiceOption(tabIndex, "스크린 공간 반사", uGUI_OptionsPanel.postFXQualityNames, UwePostProcessingManager.GetSsrQuality(), new UnityAction<int>(__instance.OnSSRqualityChanged), null);
                __instance.ditheringOption = __instance.AddToggleOption(tabIndex, "디더링", UwePostProcessingManager.GetDitheringEnabled(), new UnityAction<bool>(__instance.OnDitheringChanged), null);
            }
            __instance.weatherQualityOption = __instance.AddChoiceOption(tabIndex, "날씨 품질", uGUI_OptionsPanel.weatherQualityOptions, VFXWeatherManager.GetQuality(), new UnityAction<int>(__instance.OnAWeatherQualityChanged), null);       
        
            return false;
        }
        
        //Add in our own Ambient Occlusion Option
        public static void Postfix(uGUI_OptionsPanel __instance)
        {
			Settings.AddToGraphicsOptions(__instance);
        }
    }

    [HarmonyPatch(typeof(uGUI_OptionsPanel), nameof(uGUI_OptionsPanel.OnScreenChanged))]
    static class OnScreenChangedFixer
    {
        public static bool Prefix(uGUI_OptionsPanel __instance)
        {
            if (__instance.AreDisplayOptionsEnabled() && __instance.resolutionOption)
            {
                __instance.resolutionOption.value = uGUI_OptionsPanel.GetCurrentResolutionIndex(__instance.resolutions);
                __instance.toApply.Remove(uGUI_OptionsPanel.Change.Resolution);
            }
            if(__instance.hFovSlider != null)
            {
                __instance.OnVFovChanged(MiscSettings.fieldOfView);  
            } 

            return false;    
        }
    }


    // GameOptions.GetVRAnimationMode returns true whenever we want to play the simplified VR Animations instead of the desktop ones
    [HarmonyPatch(typeof(VRGameOptions), nameof(VRGameOptions.GetVrAnimationMode))]
    class EnableCinematicsIfSet
    {
        static bool Prefix(ref bool __result)
        {
            __result = false;//!VROptions.enableCinematics;
            return false;
        }
    }

    //XRSettings sets most of the graphics settings correctly for comfort but it still allows Ambient Occlusion to be set
    //AO appears to only be updating in one eye at the moment so I am disabling it here until we can either
    //default it to off in new installations or fix the issue with only one eye rendering
    [HarmonyPatch(typeof(UwePostProcessingManager), nameof(UwePostProcessingManager.ApplySettingsToProfile))]
    public static class FixGraphicsForVR
    {
        public static void Postfix(UwePostProcessingManager __instance)
        {
            //Mod.logger.LogInfo($"UwePostProcessingManager ApplySettingsToProfile called");
            __instance.SetAO(0);
            //__instance.SetDof(0);
            //__instance.SetSSR(0);
            //__instance.SetMotionBlur(0);
             MiscSettings.cameraBobbing = VROptions.enableCinematics; 

             //AmbientOcclusionVR.enabled = Settings.UseAmbientOcclusion;
             //AmbientOcclusionVR.FilterEnabled = Settings.AmbientOcclusionTemporalFilterEnabled;

        }
    }


    #endregion
}
