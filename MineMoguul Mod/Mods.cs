using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace MineMoguul_Mod
{
    internal static class Mods
    {
        public static PlayerController Player;
        private static EconomyManager economyManager;

        public static bool fastMove;
        public static bool customGravity;
        public static bool bunnyHop;
        public static bool airControl;
        public static bool momentum;
        public static bool flyMode;
        public static bool noClip;
        public static bool infiniteJump;
        public static bool superJump;
        public static bool speedLines;
        public static bool timeScale;
        public static bool esp;
        public static bool autoCollect;
        public static bool xrayVision;
        public static bool autoMinerESP;
        public static bool autoMinerTracers;
        public static bool infiniteMoney;
        public static bool unlockAllShopItems;
        public static bool freeShopping;
        public static bool sellMultiplierEnabled;
        public static bool autoSellEnabled;
        public static bool moneyFreeze;
        public static bool passiveIncome;
        public static bool smartAutoSell;
        public static bool selectiveFreeShopping;
        public static bool infiniteGrabRange;
        public static bool customFOV;
        public static bool noBobbing;
        private static List<Collider> ignoredColliders = new List<Collider>();
        private static bool collisionDisabled = false;
        private static readonly List<Renderer> xrayedRenderers = new List<Renderer>();
        private static bool xrayApplied;

        public static float sellMultiplier = 2f;
        public static float moneyAmount = 1000000f;
        public static float freePriceThreshold = 50000f;
        public static float walkSpeed = 8f;
        public static float sprintSpeed = 12f;
        public static float gravity = -20f;
        public static float jumpMultiplier = 1.4f;
        public static float airControlStrength = 2.5f;
        public static float momentumStrength = 1.2f;
        public static float flySpeed = 10f;
        public static float superJumpHeight = 10f;
        public static float timeScaleMultiplier = 1f;
        public static float espRange = 100f;
        public static float grabRangeMultiplier = 10f;
        public static float customFOVValue = 120f;
        private static int lastTickets = -1;

        private static Vector3 storedVelocity;
        private static CharacterController originalController;
        private static bool wasFlying = false;
        private static float originalSlopeLimit;
        private static float originalStepOffset;
        private static LayerMask interactLayerMaskBackup;
        private static int originalCullingMask = -1;
        private static GameObject speedLinesEffect;
        private static float originalFOV = -1f;
        private static float originalCameraBob = -1f;
        private static float originalViewmodelBob = -1f;
        private static float originalJumpHeight = 2f;
        public static float moneyPerMinute;

        public static List<AutoMiner> autoMiners = new List<AutoMiner>();
        private static Dictionary<AutoMiner, GameObject> espObjects = new Dictionary<AutoMiner, GameObject>();
        private static Dictionary<AutoMiner, LineRenderer> tracerLines = new Dictionary<AutoMiner, LineRenderer>();
        private static Material espMaterial;
        private static Material tracerMaterial;
        private static Color activeColor = new Color(0f, 1f, 0f, 0.5f);
        private static Color inactiveColor = new Color(1f, 0f, 0f, 0.5f);
        private static Color tracerColor = new Color(1f, 0.5f, 0f, 0.8f);

        public static void Init()
        {
            if (Player == null)
            {
                Player = UnityEngine.Object.FindFirstObjectByType<PlayerController>();
                if (Player != null)
                {
                    originalController = Player.CharacterController;
                    originalSlopeLimit = Player.StandingSlopeLimit;
                    originalStepOffset = Player.CharacterController.stepOffset;
                    interactLayerMaskBackup = Player.InteractLayerMask;
                    originalJumpHeight = Player.JumpHeight;

                    var settings = Singleton<SettingsManager>.Instance;
                    if (settings != null)
                    {
                        originalFOV = settings.DesiredFOV;
                    }
                }
            }

            if (economyManager == null)
            {
                economyManager = UnityEngine.Object.FindFirstObjectByType<EconomyManager>();
            }

            CreateSpeedLinesEffect();
            CreateESPMaterials();
            RefreshAutoMiners();
        }

        private static void CreateESPMaterials()
        {
            try
            {
                espMaterial = new Material(Shader.Find("Unlit/Color"))
                {
                    color = new Color(1f, 0f, 0f, 0.5f)
                };

                tracerMaterial = new Material(Shader.Find("Unlit/Color"))
                {
                    color = tracerColor
                };

                espMaterial.SetInt("_ZWrite", 0);
                espMaterial.SetInt("_ZTest", (int)UnityEngine.Rendering.CompareFunction.LessEqual);
                tracerMaterial.SetInt("_ZWrite", 0);
                tracerMaterial.SetInt("_ZTest", (int)UnityEngine.Rendering.CompareFunction.LessEqual);
            }
            catch
            {
                Debug.LogError("Failed to create ESP materials.");
            }
        }

        public static void RefreshAutoMiners()
        {
            autoMiners.Clear();
            var foundMiners = UnityEngine.Object.FindObjectsByType<AutoMiner>(FindObjectsSortMode.None);
            if (foundMiners != null && foundMiners.Length > 0)
            {
                autoMiners.AddRange(foundMiners);
            }
        }

        public static void Tick()
        {
            if (Player == null) return;
            ApplySpeed();
            ApplyGravity();
            HandleBunnyHop();
            HandleAirControl();
            HandleMomentum();
            HandleFlyMode();
            HandleNoClip();
            HandleInfiniteJump();
            HandleSuperJump();
            HandleSpeedLines();
            HandleTimeScale();
            HandleXrayVision();
            HandleNoBobbing();
            HandleCustomFOV();
            HandleAutoCollect();
            HandleAutoMinerESP();
            HandleMoneyMods();
            HandleShopMods();
            HandleInfiniteGrabRange();
        }


        private static void ApplySpeed()
        {
            if (!fastMove) return;
            Player.WalkSpeed = walkSpeed;
            Player.SprintSpeed = sprintSpeed;
        }

        private static void ApplyGravity()
        {
            if (!customGravity) return;
            Player.Gravity = gravity;
        }

        private static void HandleBunnyHop()
        {
            if (!bunnyHop) return;
            var input = Player.GetInputActions();
            if (input.Player.Jump.triggered && Player.SelectedWalkSpeed > 0f)
            {
                Player.JumpHeight *= jumpMultiplier;
            }
        }

        private static void HandleAirControl()
        {
            if (!airControl || flyMode || noClip) return;
            Vector2 move = Player.MoveInput;
            if (move.sqrMagnitude < 0.01f) return;
            Vector3 wishDir = Player.transform.right * move.x + Player.transform.forward * move.y;
            storedVelocity += wishDir * airControlStrength * Time.deltaTime;
        }

        private static void HandleMomentum()
        {
            if (!momentum || flyMode || noClip) return;
            Vector2 move = Player.MoveInput;
            if (move.sqrMagnitude < 0.01f) return;
            Vector3 wishDir = Player.transform.forward * move.y + Player.transform.right * move.x;
            storedVelocity = Vector3.Lerp(storedVelocity, wishDir * momentumStrength, Time.deltaTime * 2f);
        }

        private static void HandleFlyMode()
        {
            if (!flyMode)
            {
                if (wasFlying)
                {
                    wasFlying = false;
                    if (Player.CharacterController != null)
                    {
                        Player.CharacterController.enabled = true;
                        Player.CharacterController.detectCollisions = true;
                    }
                    Player.Gravity = customGravity ? gravity : -9.81f;
                    ClearVelocity();
                }
                return;
            }

            if (noClip) return;

            wasFlying = true;

            if (Player.CharacterController != null)
            {
                Player.CharacterController.enabled = false;
                Player.CharacterController.detectCollisions = false;
            }

            Player.Gravity = 0f;
            ClearVelocity();

            var input = Player.GetInputActions();
            Vector2 move = input.Player.Move.ReadValue<Vector2>();
            Vector3 moveDir = Vector3.zero;

            if (Mathf.Abs(move.x) > 0.1f) moveDir += Player.transform.right * move.x;
            if (Mathf.Abs(move.y) > 0.1f) moveDir += Player.transform.forward * move.y;

            if (input.Player.Jump.IsPressed()) moveDir.y = 1f;
            if (input.Player.Duck.IsPressed()) moveDir.y = -1f;

            if (moveDir.sqrMagnitude > 0.1f)
            {
                float currentSpeed = flySpeed;
                if (input.Player.Sprint.IsPressed()) currentSpeed *= 1.5f;

                Player.transform.position += moveDir.normalized * currentSpeed * Time.deltaTime;
            }
        }

        private static bool noclipActive = false;
        private static float savedGravity;
        private static float savedSlopeLimit;
        private static float savedStepOffset;

        private static void DisableErrorPopups()
        {
            var popups = UnityEngine.Object.FindObjectsOfType<ErrorMessagePopup>(true);
            foreach (var popup in popups)
            {
                if (popup != null && popup.gameObject.activeSelf)
                {
                    popup.gameObject.SetActive(false);
                }
            }

            if (Singleton<DebugManager>.Instance != null)
            {
                Singleton<DebugManager>.Instance.DontShowErrorAgainThisSession = true;
            }
        }

        private static void HandleNoClip()
        {
            if (Player == null || Player.CharacterController == null)  return;

            if (noClip && !noclipActive)
            {
                noclipActive = true;
                savedGravity = Player.Gravity;
                savedSlopeLimit = Player.StandingSlopeLimit;
                savedStepOffset = Player.CharacterController.stepOffset;
                Player.CharacterController.enabled = false;
                Player.CharacterController.detectCollisions = false;
                Player.Gravity = 0f;
                Player.StandingSlopeLimit = 90f;
                ClearVelocity();
                DisableErrorPopups();
            }

            if (!noClip && noclipActive)
            {
                noclipActive = false;
                Player.CharacterController.enabled = true;
                Player.CharacterController.detectCollisions = true;
                Player.Gravity = savedGravity;
                Player.StandingSlopeLimit = savedSlopeLimit;
                Player.CharacterController.stepOffset = savedStepOffset;
                ClearVelocity();
                return;
            }

            if (!noclipActive) return;

            DisableErrorPopups();
            var input = Player.GetInputActions();
            if (input == null) return;

            Vector2 move = input.Player.Move.ReadValue<Vector2>();
            Vector3 moveDir = Vector3.zero;
            if (Mathf.Abs(move.x) > 0.05f)  moveDir += Player.transform.right * move.x;
            if (Mathf.Abs(move.y) > 0.05f) moveDir += Player.transform.forward * move.y;
            if (input.Player.Jump.IsPressed()) moveDir += Vector3.up;
            if (input.Player.Duck.IsPressed()) moveDir += Vector3.down;
            if (moveDir.sqrMagnitude < 0.01f) return;
            float speed = walkSpeed * 3f;
            if (input.Player.Sprint.IsPressed()) speed *= 1.5f;
            Player.transform.position += moveDir.normalized * speed * Time.deltaTime;
        }

        private static void ClearVelocity()
        {
            var velocityField = typeof(PlayerController).GetField("_velocity", BindingFlags.NonPublic | BindingFlags.Instance);
            if (velocityField != null)
            {
                velocityField.SetValue(Player, Vector3.zero);
            }
            storedVelocity = Vector3.zero;
        }

        private static void HandleInfiniteJump()
        {
            if (!infiniteJump) return;
            var input = Player.GetInputActions();
            if (input.Player.Jump.triggered)
            {
                var velocityField = typeof(PlayerController).GetField("_velocity", BindingFlags.NonPublic | BindingFlags.Instance);
                if (velocityField != null)
                {
                    Vector3 currentVelocity = (Vector3)velocityField.GetValue(Player);
                    currentVelocity.y = Mathf.Sqrt(Player.JumpHeight * -2f * Player.Gravity);
                    velocityField.SetValue(Player, currentVelocity);
                }
            }
        }

        private static void HandleSuperJump()
        {
            if (!superJump) return;
            Player.JumpHeight = superJumpHeight;
        }

        private static void CreateSpeedLinesEffect()
        {
            if (speedLinesEffect != null) return;
            speedLinesEffect = new GameObject("SpeedLines");
            var ps = speedLinesEffect.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.loop = true;
            main.startSpeed = -20f;
            main.startLifetime = 0.5f;
            main.startSize = 0.1f;
            main.maxParticles = 100;
            var emission = ps.emission;
            emission.rateOverTime = 0f;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 5f;
            shape.radius = 0.1f;
            speedLinesEffect.SetActive(false);
        }

        private static void HandleSpeedLines()
        {
            if (speedLinesEffect == null) return;

            bool shouldShow = speedLines && Player.SelectedWalkSpeed > walkSpeed * 0.8f;

            if (shouldShow && !speedLinesEffect.activeSelf)
            {
                speedLinesEffect.transform.position = Player.transform.position + Player.transform.forward * 0.5f;
                speedLinesEffect.transform.rotation = Player.transform.rotation;
                speedLinesEffect.transform.SetParent(Player.PlayerCamera.transform);
                speedLinesEffect.SetActive(true);
                var ps = speedLinesEffect.GetComponent<ParticleSystem>();
                var emission = ps.emission;
                emission.rateOverTime = Mathf.Lerp(0, 50, (Player.SelectedWalkSpeed - walkSpeed * 0.8f) / (walkSpeed * 0.2f));
                ps.Play();
            }
            else if (!shouldShow && speedLinesEffect.activeSelf)
            {
                speedLinesEffect.SetActive(false);
            }
        }

        private static void HandleTimeScale()
        {
            if (!timeScale) return;
            Time.timeScale = timeScaleMultiplier;
            Time.fixedDeltaTime = 0.02f * timeScaleMultiplier;
        }

        private static void HandleAutoCollect()
        {
            if (!autoCollect) return;

            if (Time.frameCount % 10 != 0) return;

            float range = 5f;
            Collider[] colliders = Physics.OverlapSphere(Player.transform.position, range);

            foreach (var collider in colliders)
            {
                if (collider.CompareTag("Grabbable") || collider.GetComponent<OrePiece>() != null)
                {
                    if (Vector3.Distance(Player.transform.position, collider.transform.position) < 2f)
                    {
                        collider.transform.position = Vector3.MoveTowards(
                            collider.transform.position,
                            Player.transform.position,
                            Time.deltaTime * 10f
                        );
                    }
                }
            }
        }

        private static void HandleXrayVision()
        {
            if (!xrayVision)
            {
                if (!xrayApplied) return;

                foreach (var r in xrayedRenderers)
                {
                    if (r != null)
                        r.forceRenderingOff = false;
                }

                xrayedRenderers.Clear();
                xrayApplied = false;
                return;
            }

            if (xrayApplied) return;

            foreach (var r in UnityEngine.Object.FindObjectsOfType<Renderer>())
            {
                if (r == null) continue;

                // Filters — tweak these for MineMogul
                string n = r.gameObject.name.ToLower();
                if (
                    n.Contains("wall") ||
                    n.Contains("rock") ||
                    n.Contains("terrain") ||
                    n.Contains("ground")
                )
                {
                    r.forceRenderingOff = true;
                    xrayedRenderers.Add(r);
                }
            }

            xrayApplied = true;
        }


        private static void HandleAutoMinerESP()
        {
            if (!autoMinerESP && !autoMinerTracers)
            {
                CleanupESP();
                return;
            }

            if (Time.frameCount % 120 == 0)
            {
                RefreshAutoMiners();
            }

            foreach (var miner in autoMiners.ToList())
            {
                if (miner == null) continue;

                float distance = Vector3.Distance(Player.transform.position, miner.transform.position);
                if (distance > espRange || !miner.gameObject.activeInHierarchy)
                {
                    RemoveESPObject(miner);
                    RemoveTracerLine(miner);
                    continue;
                }

                if (autoMinerESP) UpdateESPObject(miner);
                else RemoveESPObject(miner);

                if (autoMinerTracers) UpdateTracerLine(miner);
                else RemoveTracerLine(miner);
            }

            CleanupNullReferences();
        }

        private static void UpdateESPObject(AutoMiner miner)
        {
            if (!espObjects.TryGetValue(miner, out GameObject espObj) || espObj == null)
            {
                CreateESPObject(miner);
                return;
            }
            espObj.transform.position = miner.transform.position;
            espObj.transform.rotation = Quaternion.identity;
            Vector3 size = Vector3.one * 2f;
            Renderer minerRenderer = miner.GetComponentInChildren<Renderer>();
            if (minerRenderer != null) size = minerRenderer.bounds.size;
            espObj.transform.localScale = Vector3.Min(size, Vector3.one * 5f);
            Renderer espRenderer = espObj.GetComponent<Renderer>();
            if (espRenderer == null || espRenderer.sharedMaterial == null) return;
            Color color;
            if (miner.ResourceDefinition == null)
            {
                color = new Color(1f, 1f, 0f, 0.6f);
            }
            else if (!miner.Enabled)
            {
                color = new Color(1f, 0f, 0f, 0.6f);
            }
            else
            {
                color = new Color(0f, 1f, 0f, 0.6f);
            }
            float pulse = Mathf.Sin(Time.time * 3f) * 0.25f + 0.75f;
            color.a *= pulse;
            espRenderer.sharedMaterial.color = color;
        }

        private static void CreateESPObject(AutoMiner miner)
        {
            GameObject espObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            espObj.name = $"ESP_{miner.GetInstanceID()}";
            Collider col = espObj.GetComponent<Collider>();
            if (col != null) UnityEngine.Object.Destroy(col);

            Renderer renderer = espObj.GetComponent<Renderer>();
            if (renderer != null && espMaterial != null)
            {
                renderer.sharedMaterial = espMaterial;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }

            espObjects[miner] = espObj;
        }

        private static void UpdateTracerLine(AutoMiner miner)
        {
            if (!tracerLines.TryGetValue(miner, out LineRenderer line) || line == null)
            {
                CreateTracerLine(miner);
                return;
            }

            Vector3 startPos = Player.transform.position + Vector3.up * 1.5f;
            Vector3 endPos = miner.transform.position + Vector3.up * 1f;
            line.SetPosition(0, startPos);
            line.SetPosition(1, endPos);

            float distance = Vector3.Distance(startPos, endPos);
            float width = Mathf.Lerp(0.08f, 0.02f, Mathf.Clamp01(distance / espRange));
            line.startWidth = width;
            line.endWidth = width * 0.3f;

            float pulse = Mathf.Sin(Time.time * 2f) * 0.4f + 0.6f;
            Color lineColor = miner.Enabled ? new Color(0f, 1f, 0f, 0.7f * pulse) : new Color(1f, 0.5f, 0f, 0.7f * pulse);
            line.material.color = lineColor;
        }

        private static void CreateTracerLine(AutoMiner miner)
        {
            try
            {
                GameObject lineObj = new GameObject($"Tracer_{miner.GetInstanceID()}");
                LineRenderer line = lineObj.AddComponent<LineRenderer>();

                Shader unlitShader = Shader.Find("Unlit/Color");
                if (unlitShader == null) return;

                line.material = new Material(unlitShader);
                line.startWidth = 0.05f;
                line.endWidth = 0.02f;
                line.positionCount = 2;
                line.useWorldSpace = true;
                line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                line.receiveShadows = false;
                tracerLines[miner] = line;
            }
            catch { }
        }

        private static void RemoveESPObject(AutoMiner miner)
        {
            if (espObjects.TryGetValue(miner, out GameObject espObj) && espObj != null)
            {
                UnityEngine.Object.Destroy(espObj);
            }
            espObjects.Remove(miner);
        }

        private static void RemoveTracerLine(AutoMiner miner)
        {
            if (tracerLines.TryGetValue(miner, out LineRenderer line) && line != null)
            {
                UnityEngine.Object.Destroy(line.gameObject);
            }
            tracerLines.Remove(miner);
        }

        private static void CleanupESP()
        {
            foreach (var kvp in espObjects.ToList())
            {
                if (kvp.Value != null) UnityEngine.Object.Destroy(kvp.Value);
            }
            espObjects.Clear();

            foreach (var kvp in tracerLines.ToList())
            {
                if (kvp.Value != null) UnityEngine.Object.Destroy(kvp.Value.gameObject);
            }
            tracerLines.Clear();
        }

        private static void CleanupNullReferences()
        {
            var espNulls = espObjects.Keys.Where(k => k == null || espObjects[k] == null).ToList();
            foreach (var key in espNulls)
            {
                if (espObjects.TryGetValue(key, out GameObject obj) && obj != null) UnityEngine.Object.Destroy(obj);
                espObjects.Remove(key);
            }

            var tracerNulls = tracerLines.Keys.Where(k => k == null || tracerLines[k] == null).ToList();
            foreach (var key in tracerNulls)
            {
                if (tracerLines.TryGetValue(key, out LineRenderer line) && line != null) UnityEngine.Object.Destroy(line.gameObject);
                tracerLines.Remove(key);
            }
        }

        private static void HandleInfiniteGrabRange()
        {
            if (!infiniteGrabRange) return;
            var interactRangeField = typeof(PlayerController).GetField("_interactRange", BindingFlags.NonPublic | BindingFlags.Instance);
            if (interactRangeField != null)
            {
                interactRangeField.SetValue(Player, 2f * grabRangeMultiplier);
            }
        }

        private static void HandleMoneyMods()
        {
            if (economyManager == null) return;
            if (infiniteMoney) economyManager.SetMoney(moneyAmount);
        }

        private static void HandleShopMods()
        {
            if (economyManager == null) return;
            if (unlockAllShopItems) economyManager.UnlockAllShopItems();
            if (freeShopping) SetMoneyToMax();
        }

        private static void SetMoneyToMax()
        {
            try
            {
                var moneyField = typeof(EconomyManager).GetField("_money", BindingFlags.NonPublic | BindingFlags.Instance);
                if (moneyField != null)
                {
                    moneyField.SetValue(economyManager, float.MaxValue * 0.1f);
                }
                else
                {
                    var setMoneyMethod = typeof(EconomyManager).GetMethod("SetMoney", BindingFlags.Public | BindingFlags.Instance);
                    if (setMoneyMethod != null)
                    {
                        setMoneyMethod.Invoke(economyManager, new object[] { 999999999f });
                    }
                }
            }
            catch { }
        }

        private static void HandleNoBobbing()
        {
            var settings = Singleton<SettingsManager>.Instance;
            if (settings == null) return;

            if (noBobbing)
            {
                if (originalCameraBob < 0f)
                {
                    originalCameraBob = settings.CameraBobScale;
                    originalViewmodelBob = settings.ViewmodelBobScale;
                }

                settings.CameraBobScale = 0f;
                settings.ViewmodelBobScale = 0f;
            }
            else if (originalCameraBob >= 0f)
            {
                settings.CameraBobScale = originalCameraBob;
                settings.ViewmodelBobScale = originalViewmodelBob;
                originalCameraBob = -1f;
                originalViewmodelBob = -1f;
            }
        }

        private static void HandleCustomFOV()
        {
            if (Player == null) return;
            var settings = Singleton<SettingsManager>.Instance;
            if (settings == null) return;

            if (customFOV)
            {
                if (originalFOV < 0f) originalFOV = settings.DesiredFOV;
                settings.DesiredFOV = customFOVValue;
            }
            else if (originalFOV >= 0f)
            {
                settings.DesiredFOV = originalFOV;
                originalFOV = -1f;
            }
        }

        public static float GetMoney()
        {
            if (economyManager != null)
            {
                FieldInfo moneyField = typeof(EconomyManager).GetField("_money", BindingFlags.NonPublic | BindingFlags.Instance);
                if (moneyField != null)
                {
                    return (float)moneyField.GetValue(economyManager);
                }
            }
            return 0f;
        }

        public static void PrintAutoMinerInfo()
        {
            RefreshAutoMiners();
            int activeCount = 0;
            int inactiveCount = 0;

            foreach (var miner in autoMiners)
            {
                if (miner == null) continue;

                if (miner.Enabled)
                    activeCount++;
                else
                    inactiveCount++;
            }

            Debug.Log($"AutoMiners: {autoMiners.Count} total");
            Debug.Log($"  Active: {activeCount}");
            Debug.Log($"  Inactive: {inactiveCount}");
            Debug.Log($"  ESP Objects: {espObjects.Count}");
            Debug.Log($"  Tracer Lines: {tracerLines.Count}");
        }

        public static void AddMoney(float amount)
        {
            if (economyManager != null)
            {
                economyManager.AddMoney(amount);
            }
        }

        public static void SetMoney(float amount)
        {
            if (economyManager != null)
            {
                economyManager.SetMoney(amount);
            }
        }

        public static void AddMoneyGUI()
        {
            AddMoney(moneyAmount);
        }

        public static void SetMoneyGUI()
        {
            SetMoney(moneyAmount);
        }

        public static void ResetMoney()
        {
            if (economyManager != null)
            {
                economyManager.SetMoney(0f);
            }
        }

        public static void ToggleAutoMinerESP()
        {
            autoMinerESP = !autoMinerESP;
            if (!autoMinerESP) CleanupESP();
            else RefreshAutoMiners();
        }

        public static void ToggleAutoMinerTracers()
        {
            autoMinerTracers = !autoMinerTracers;
            if (!autoMinerTracers)
            {
                foreach (var kvp in tracerLines)
                {
                    if (kvp.Value != null) kvp.Value.enabled = false;
                }
            }
        }

        public static void TeleportForward(float distance = 6f)
        {
            if (Player == null) return;
            Player.TeleportPlayer(Player.transform.position + Player.transform.forward * distance);
        }

        public static void TeleportToCursor()
        {
            if (Player == null || Camera.main == null) return;
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, 1000f))
            {
                Player.TeleportPlayer(hit.point + Vector3.up * 2f);
            }
        }

        public static void TeleportUp(float height = 50f)
        {
            if (Player == null) return;
            Player.TeleportPlayer(Player.transform.position + Vector3.up * height);
        }

        public static void InfiniteResearchTickets()
        {
            var rm = ResearchManager.Instance;
            if (rm == null) return;
            if (rm.ResearchTickets < 9999) rm.SetResearchTickets(9999);
        }

        public static void UnlockAllResearch()
        {
            var rm = ResearchManager.Instance;
            if (rm == null) return;

            foreach (var research in Singleton<SavingLoadingManager>.Instance.AllResearchItemDefinitions)
            {
                if (!rm.IsResearchItemCompleted(research))
                {
                    rm.CompletedResearchItems.Add(research.GetSavableObjectID());
                    research.OnResearched();
                }
            }
        }

        public static void FreeResearch(ResearchItemDefinition item)
        {
            var rm = ResearchManager.Instance;
            if (rm == null || item == null) return;

            if (rm.IsResearchItemCompleted(item)) return;

            rm.CompletedResearchItems.Add(item.GetSavableObjectID());
            item.OnResearched();
        }

        public static void ResetAllResearch()
        {
            var rm = ResearchManager.Instance;
            if (rm == null) return;

            rm.CompletedResearchItems.Clear();
            rm.SetResearchTickets(0);
        }

        public static void ForceUnlockLockedResearch()
        {
            var rm = ResearchManager.Instance;
            if (rm == null) return;

            foreach (var research in Singleton<SavingLoadingManager>.Instance.AllResearchItemDefinitions)
            {
                if (!rm.IsResearchItemCompleted(research))
                {
                    rm.CompletedResearchItems.Add(research.GetSavableObjectID());
                    research.OnResearched();
                }
            }
        }

        public static void AutoResearch()
        {
            var rm = ResearchManager.Instance;
            if (rm == null) return;

            var allResearch = Singleton<SavingLoadingManager>.Instance.AllResearchItemDefinitions;

            foreach (var research in allResearch)
            {
                if (!rm.IsResearchItemCompleted(research) &&
                    research.CanAfford() &&
                    !research.IsLocked())
                {
                    rm.ResearchItem(research);
                }
            }
        }


        public static void TicketMultiplier(int multiplier = 5)
        {
            var rm = ResearchManager.Instance;
            if (rm == null) return;

            if (lastTickets == -1)
                lastTickets = rm.ResearchTickets;

            if (rm.ResearchTickets > lastTickets)
            {
                int gained = rm.ResearchTickets - lastTickets;
                rm.AddResearchTickets(gained * (multiplier - 1));
            }

            lastTickets = rm.ResearchTickets;
        }


        public static void ResetOverrides()
        {
            if (Player == null) return;

            Player.WalkSpeed = 4f;
            Player.SprintSpeed = 6f;
            Player.Gravity = -9.81f;
            Player.JumpHeight = originalJumpHeight;
            storedVelocity = Vector3.zero;

            if (wasFlying)
            {
                Player.CharacterController.enabled = true;
                Player.CharacterController.detectCollisions = true;
                wasFlying = false;
            }

            if (Player.CharacterController != null)
            {
                Player.CharacterController.enabled = true;
                Player.CharacterController.detectCollisions = true;
                Player.StandingSlopeLimit = originalSlopeLimit;
                Player.CharacterController.stepOffset = originalStepOffset;
            }

            Time.timeScale = 1f;
            Time.fixedDeltaTime = 0.02f;

            if (Player.PlayerCamera != null)
            {
                Player.PlayerCamera.cullingMask = interactLayerMaskBackup;
            }

            if (speedLinesEffect != null) speedLinesEffect.SetActive(false);

            CleanupESP();
            autoMinerESP = false;
            autoMinerTracers = false;
            infiniteMoney = false;
            unlockAllShopItems = false;
            freeShopping = false;
            sellMultiplierEnabled = false;
            autoSellEnabled = false;

            var settings = Singleton<SettingsManager>.Instance;
            if (settings != null && originalFOV >= 0f)
            {
                settings.DesiredFOV = originalFOV;
            }

            var interactRangeField = typeof(PlayerController).GetField("_interactRange", BindingFlags.NonPublic | BindingFlags.Instance);
            if (interactRangeField != null)
            {
                interactRangeField.SetValue(Player, 2f);
            }
        }
    }
}