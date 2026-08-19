using System.Collections.Generic;
using GyroCue.Input;
using GyroCue.UI;
using UnityEngine;

namespace GyroCue.Core
{
    public static class ShotLifecycleMath
    {
        public static Vector2 CalculateImpulse(ShotCommand shotCommand, float maxCueBallImpulse)
        {
            var direction = shotCommand.AimDirection.sqrMagnitude > 0.0001f
                ? shotCommand.AimDirection.normalized
                : Vector2.right;
            return direction * (Mathf.Clamp01(shotCommand.NormalizedPower) * Mathf.Max(0f, maxCueBallImpulse));
        }
    }

    /// <summary>
    /// Requires every active ball to stay below the configured linear and angular
    /// thresholds for a continuous debounce window before declaring the table settled.
    /// </summary>
    public sealed class ShotSettleDetector
    {
        private readonly float stopSpeedThreshold;
        private readonly float angularStopThresholdDegreesPerSecond;
        private readonly float settleDebounceSeconds;

        public ShotSettleDetector(
            float stopSpeedThreshold,
            float angularStopThresholdDegreesPerSecond,
            float settleDebounceSeconds)
        {
            this.stopSpeedThreshold = Mathf.Max(0f, stopSpeedThreshold);
            this.angularStopThresholdDegreesPerSecond = Mathf.Max(0f, angularStopThresholdDegreesPerSecond);
            this.settleDebounceSeconds = Mathf.Max(0f, settleDebounceSeconds);
        }

        public float SettledDurationSeconds { get; private set; }

        public void Reset()
        {
            SettledDurationSeconds = 0f;
        }

        public bool Advance(IReadOnlyList<Rigidbody2D> ballBodies, float deltaTimeSeconds)
        {
            if (AnyBodyMoving(ballBodies))
            {
                SettledDurationSeconds = 0f;
                return false;
            }

            SettledDurationSeconds += Mathf.Max(0f, deltaTimeSeconds);
            return SettledDurationSeconds >= settleDebounceSeconds;
        }

        public void ClampBodiesToRest(IReadOnlyList<Rigidbody2D> ballBodies)
        {
            if (ballBodies == null)
            {
                return;
            }

            for (var i = 0; i < ballBodies.Count; i++)
            {
                var body = ballBodies[i];
                if (!IsActiveSimulationBody(body))
                {
                    continue;
                }

                if (body.velocity.sqrMagnitude <= stopSpeedThreshold * stopSpeedThreshold)
                {
                    body.velocity = Vector2.zero;
                }

                if (Mathf.Abs(body.angularVelocity) <= angularStopThresholdDegreesPerSecond)
                {
                    body.angularVelocity = 0f;
                }
            }
        }

        private bool AnyBodyMoving(IReadOnlyList<Rigidbody2D> ballBodies)
        {
            if (ballBodies == null)
            {
                return false;
            }

            var stopSpeedSquared = stopSpeedThreshold * stopSpeedThreshold;
            for (var i = 0; i < ballBodies.Count; i++)
            {
                var body = ballBodies[i];
                if (!IsActiveSimulationBody(body))
                {
                    continue;
                }

                if (body.velocity.sqrMagnitude > stopSpeedSquared ||
                    Mathf.Abs(body.angularVelocity) > angularStopThresholdDegreesPerSecond)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsActiveSimulationBody(Rigidbody2D body)
        {
            return body != null && body.gameObject.activeInHierarchy && body.simulated;
        }
    }

    public readonly struct ShotRuleContext
    {
        public ShotRuleContext(bool cueBallScratched, int objectBallsPocketed, int shootingPlayerIndex)
        {
            CueBallScratched = cueBallScratched;
            ObjectBallsPocketed = Mathf.Max(0, objectBallsPocketed);
            ShootingPlayerIndex = Mathf.Max(0, shootingPlayerIndex);
        }

        public bool CueBallScratched { get; }

        public int ObjectBallsPocketed { get; }

        public int ShootingPlayerIndex { get; }
    }

    /// <summary>
    /// Boundary between shot orchestration and game-specific rules. A complete
    /// 8-ball evaluator can replace the fallback implementation without coupling
    /// rules to touch, remote cue, or physics polling code.
    /// </summary>
    public interface IShotRuleResolver
    {
        TurnResolutionResult ResolveShot(ShotRuleContext context);
    }

    public sealed class ScratchOnlyShotRuleResolver : IShotRuleResolver
    {
        public TurnResolutionResult ResolveShot(ShotRuleContext context)
        {
            return TurnResolutionResult.Continue(
                keepTurn: context.ObjectBallsPocketed > 0 && !context.CueBallScratched,
                committedFoul: context.CueBallScratched);
        }
    }

    /// <summary>
    /// Connects input, cue-ball impulse, table settling, pocket tracking, rules,
    /// HUD, and cue-ball-in-hand into one runtime shot lifecycle.
    /// </summary>
    public sealed class ShotLifecycleController : MonoBehaviour
    {
        [Header("Input and Presentation")]
        [SerializeField]
        private CueInputCoordinator cueInputCoordinator;

        [SerializeField]
        private TouchAimSwipeController touchAimSwipeController;

        [SerializeField]
        private CuePreviewVisualizer cuePreviewVisualizer;

        [SerializeField]
        private MinimalHudPresenter hudPresenter;

        [Header("Table")]
        [SerializeField]
        private Rigidbody2D cueBallBody;

        [SerializeField]
        private Rigidbody2D[] ballBodies = new Rigidbody2D[0];

        [SerializeField]
        private PocketTableController pocketTableController;

        [SerializeField]
        private CueBallPlacementController cueBallPlacementController;

        [SerializeField]
        private PoolPhysicsTuningProfile physicsProfile;

        [Header("Shot Tuning")]
        [SerializeField, Min(0f)]
        private float maxCueBallImpulse = 8f;

        [SerializeField, Min(0f)]
        private float settleDebounceSeconds = 0.25f;

        [SerializeField, Min(0f)]
        private float angularStopThresholdDegreesPerSecond = 3f;

        [Header("Rules (optional MonoBehaviour implementing IShotRuleResolver)")]
        [SerializeField]
        private MonoBehaviour ruleResolverBehaviour;

        private readonly TurnStateMachine turnState = new TurnStateMachine();
        private readonly IShotRuleResolver fallbackRuleResolver = new ScratchOnlyShotRuleResolver();
        private IShotRuleResolver ruleResolverOverride;
        private ShotSettleDetector settleDetector;

        public TurnStateMachine TurnState => turnState;

        public Vector2 LastAppliedImpulse { get; private set; }

        public bool IsTableMoving { get; private set; }

        private IShotRuleResolver RuleResolver =>
            ruleResolverOverride ?? ruleResolverBehaviour as IShotRuleResolver ?? fallbackRuleResolver;

        /// <summary>
        /// Binds the table this controller drives. Used by runtime scene construction
        /// and by edit-mode tests through <see cref="ConfigureForTests"/>.
        /// </summary>
        public void ConfigureTable(
            Rigidbody2D configuredCueBallBody,
            Rigidbody2D[] configuredBallBodies,
            PoolPhysicsTuningProfile configuredPhysicsProfile,
            PocketTableController configuredPocketTableController,
            IShotRuleResolver configuredRuleResolver,
            float maxCueBallImpulse,
            float settleDebounceSeconds,
            float angularStopThresholdDegreesPerSecond)
        {
            cueBallBody = configuredCueBallBody;
            ballBodies = configuredBallBodies ?? new Rigidbody2D[0];
            physicsProfile = configuredPhysicsProfile;
            pocketTableController = configuredPocketTableController;
            ruleResolverOverride = configuredRuleResolver;
            this.maxCueBallImpulse = Mathf.Max(0f, maxCueBallImpulse);
            this.settleDebounceSeconds = Mathf.Max(0f, settleDebounceSeconds);
            this.angularStopThresholdDegreesPerSecond = Mathf.Max(0f, angularStopThresholdDegreesPerSecond);
            RebuildSettleDetector();
        }

        public void ConfigureForTests(
            Rigidbody2D configuredCueBallBody,
            Rigidbody2D[] configuredBallBodies,
            PoolPhysicsTuningProfile configuredPhysicsProfile,
            PocketTableController configuredPocketTableController,
            IShotRuleResolver configuredRuleResolver,
            float maxCueBallImpulse,
            float settleDebounceSeconds,
            float angularStopThresholdDegreesPerSecond)
        {
            ConfigureTable(
                configuredCueBallBody,
                configuredBallBodies,
                configuredPhysicsProfile,
                configuredPocketTableController,
                configuredRuleResolver,
                maxCueBallImpulse,
                settleDebounceSeconds,
                angularStopThresholdDegreesPerSecond);
        }

        public bool TryStartShot(ShotCommand shotCommand)
        {
            ResolveReferences();
            if (cueBallBody == null || !turnState.TryBeginShot())
            {
                return false;
            }

            pocketTableController?.BeginTurnPocketTracking();
            EnsureSettleDetector().Reset();
            LastAppliedImpulse = ShotLifecycleMath.CalculateImpulse(shotCommand, maxCueBallImpulse);
            IsTableMoving = true;
            ApplyLifecycleLocks();

            cueBallBody.gameObject.SetActive(true);
            cueBallBody.simulated = true;
            cueBallBody.AddForce(LastAppliedImpulse, ForceMode2D.Impulse);
            return true;
        }

        /// <summary>
        /// Deterministic tick entry point used by Update and EditMode tests.
        /// Returns true only on the tick that completes turn resolution.
        /// </summary>
        public bool TickShotLifecycle(float deltaTimeSeconds)
        {
            if (turnState.Phase != TurnLifecyclePhase.ShotSimulation)
            {
                return false;
            }

            var detector = EnsureSettleDetector();
            if (!detector.Advance(ballBodies, deltaTimeSeconds))
            {
                return false;
            }

            detector.ClampBodiesToRest(ballBodies);
            IsTableMoving = false;

            if (!turnState.TryMarkSimulationComplete())
            {
                ApplyLifecycleLocks();
                return false;
            }

            var shootingPlayerIndex = turnState.CurrentPlayerIndex;
            var context = new ShotRuleContext(
                pocketTableController != null && pocketTableController.ScratchOccurredThisTurn,
                pocketTableController != null ? pocketTableController.ObjectBallsPocketedThisTurn : 0,
                shootingPlayerIndex);
            var resolution = RuleResolver.ResolveShot(context);

            if (!turnState.TryResolveTurn(resolution))
            {
                ApplyLifecycleLocks();
                return false;
            }

            hudPresenter?.ApplyTurnResolution(
                resolution.KeepTurn,
                resolution.CommittedFoul,
                resolution.WonMatch,
                resolution.LostMatch,
                turnState.RequiresCueBallPlacement,
                turnState.CurrentPlayerIndex,
                turnState.TurnNumber);

            if (turnState.RequiresCueBallPlacement)
            {
                cueBallPlacementController?.BeginCueBallInHand();
            }

            ApplyLifecycleLocks();
            return true;
        }

        private void Awake()
        {
            ResolveReferences();
            RebuildSettleDetector();
            hudPresenter?.ResetForNewMatch(playerOneStarts: true);
            ApplyLifecycleLocks();
        }

        private void OnEnable()
        {
            ResolveReferences();

            if (cueInputCoordinator != null)
            {
                cueInputCoordinator.ShotReleased += HandleShotReleased;
            }

            if (cueBallPlacementController != null)
            {
                cueBallPlacementController.CueBallPlaced += HandleCueBallPlaced;
            }
        }

        private void OnDisable()
        {
            if (cueInputCoordinator != null)
            {
                cueInputCoordinator.ShotReleased -= HandleShotReleased;
            }

            if (cueBallPlacementController != null)
            {
                cueBallPlacementController.CueBallPlaced -= HandleCueBallPlaced;
            }
        }

        private void Update()
        {
            TickShotLifecycle(Time.deltaTime);
        }

        private void HandleShotReleased(ShotCommand shotCommand)
        {
            TryStartShot(shotCommand);
        }

        private void HandleCueBallPlaced(Vector2 _)
        {
            if (!turnState.TryCompleteCueBallPlacement())
            {
                return;
            }

            hudPresenter?.SetTurnState(
                turnState.CurrentPlayerIndex,
                turnState.TurnNumber,
                turnState.RequiresCueBallPlacement);
            ApplyLifecycleLocks();
        }

        private ShotSettleDetector EnsureSettleDetector()
        {
            if (settleDetector == null)
            {
                RebuildSettleDetector();
            }

            return settleDetector;
        }

        private void RebuildSettleDetector()
        {
            var stopSpeed = physicsProfile != null ? physicsProfile.StopSpeedThreshold : 0.06f;
            settleDetector = new ShotSettleDetector(
                stopSpeed,
                angularStopThresholdDegreesPerSecond,
                settleDebounceSeconds);
        }

        private void ApplyLifecycleLocks()
        {
            var shouldLockInput = turnState.InputLocked;
            cueInputCoordinator?.SetGameplayInputLocked(shouldLockInput);
            touchAimSwipeController?.SetBallsMoving(shouldLockInput);
            cuePreviewVisualizer?.SetBallsMoving(shouldLockInput);
        }

        private void ResolveReferences()
        {
            if (cueInputCoordinator == null)
            {
                cueInputCoordinator = GetComponent<CueInputCoordinator>();
            }

            if (touchAimSwipeController == null)
            {
                touchAimSwipeController = GetComponent<TouchAimSwipeController>();
            }

            if (cuePreviewVisualizer == null)
            {
                cuePreviewVisualizer = GetComponent<CuePreviewVisualizer>();
            }

            if (hudPresenter == null)
            {
                hudPresenter = GetComponent<MinimalHudPresenter>();
            }

            if (pocketTableController == null)
            {
                pocketTableController = GetComponent<PocketTableController>();
            }

            if (cueBallPlacementController == null)
            {
                cueBallPlacementController = GetComponent<CueBallPlacementController>();
            }

            if (cueBallBody == null && pocketTableController != null)
            {
                cueBallBody = pocketTableController.CueBallBody;
            }

            if ((ballBodies == null || ballBodies.Length == 0) && cueBallBody != null)
            {
                ballBodies = new[] { cueBallBody };
            }
        }
    }
}
