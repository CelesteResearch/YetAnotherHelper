using System;
using System.Collections.Generic;
using System.Numerics;
using Celeste.Core.Components;
using Celeste.Core.Mathematics;
using Celeste.Core.Mathematics.Extrema;
using Celeste.Core.Mathematics.Easing;
using Celeste.Core.Randomness;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Rendering;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Content.Maps.Entities;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Game.Execution.Levels;
using Celeste.Mods.YetAnotherHelper.Presentation.Bubbles;
using Celeste.Physics.Collision;
using Celeste.Physics.Collision.Shapes;
using Celeste.Scene.Entities;
using Celeste.Scene.Tracking;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Player.Runtime;
using WindMover = Celeste.Simulation.Gameplay.Environment.Wind.WindMoverState;

namespace Celeste.Mods.YetAnotherHelper.Entities.Wind
{
    [RuntimeTypeAlias("Celeste.Mod.YetAnotherHelper.Entities.BubblePushField")]
    [CustomEntity("YetAnotherHelper/BubbleField = Create")]
    public class BubblePushField : SceneEntityState
    {
        public enum ActivationMode
        {
            Always,
            OnlyWhenFlagActive,
            OnlyWhenFlagInactive
        }

        public float Strength;

        private Dictionary<WindMover, float> WindMovers = new Dictionary<WindMover, float>();

        private Dictionary<WindMover, float> QueuedMovers = new Dictionary<WindMover, float>();

        private int FramesSinceSpawn = 0;

        private int SpawnFrame = 30;

        public PushDirection Direction;

        private ActivationMode activationMode;

        private string flag;

        private bool liftOffOfGround;

        private bool makeWind;

        public readonly ISourceEngineContext Engine;

        public static BubblePushField Create(OriginalLevelSceneRoot level, OriginalLevelRoomData room, Vector2 offset,
            OriginalLevelEntityData data) => new(level.EngineContext, data, offset);

        public BubblePushField(ISourceEngineContext engine, EntityData data, Vector2 offset) : this(
            engine,
            data.Position + offset,
            data.Width,
            data.Height,
            data.Float("strength", 1f),
            data.Attr("direction", "right"),
            data.Enum("activationMode", ActivationMode.Always),
            data.Attr("flag", "bubble_push_field"),
            data.Bool("liftOffOfGround", false),
            data.Bool("makeWind", true)
        )
        {
        }

        public BubblePushField(ISourceEngineContext engine, Vector2 position, int width, int height, float strength,
            string direction, ActivationMode activationMode, string flag, bool liftOff, bool makeWind) : base(
            ((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration)
        {
            Engine = engine;
            Position = position;
            Strength = strength;
            this.activationMode = activationMode;
            this.flag = flag;
            this.liftOffOfGround = liftOff;
            this.makeWind = makeWind;

            Enum.TryParse(direction, out Direction);

            Collider = new HitboxColliderGeometry(width, height);
        }

        public override void Added(object scene)
        {
            base.Added(scene);
        }

        public override void Removed(object scene)
        {
            base.Removed(scene);
        }

        public override void Render(ISourceRenderContext context)
        {
            base.Render(context);
        }

        public override void Update()
        {
            base.Update();

            var session = ((OriginalLevelSceneRoot)Scene).Session;
            if ((activationMode == ActivationMode.OnlyWhenFlagActive && !session.GetFlag(flag))
                || (activationMode == ActivationMode.OnlyWhenFlagInactive && session.GetFlag(flag)))
            {
                // the bubble push field is currently turned off by a session flag.
                Collidable = false;
                return;
            }

            Collidable = true;

            FramesSinceSpawn++;
            if (Strength > 0)
            {
                if (FramesSinceSpawn == SpawnFrame)
                {
                    FramesSinceSpawn = 0;
                    SpawnFrame = ((ISourceRandomEnvironment)Engine).Random.Next(2, 10);
                    Add(new BubbleParticle(true, true));
                }

                if (this.makeWind)
                {
                    foreach (WindMover mover in ((ISourceScene)Scene).SourceScene.Components.GetTrackedComponents(
                                 "Celeste.WindMover"))
                    {
                        if (SourceEntityCollision.Check((ISourceEntityCollider)mover.Entity, this))
                        {
                            if (WindMovers.ContainsKey(mover))
                                WindMovers[mover] = ScalarMath.Approach(WindMovers[mover], Strength,
                                    Engine.DeltaTime / .6f, ((ISourceExtremaEnvironment)Engine).ExtremaArithmetic);
                            else
                                WindMovers.Add(mover, 0f);
                        }
                        else
                        {
                            if (WindMovers.ContainsKey(mover))
                            {
                                WindMovers[mover] = ScalarMath.Approach(WindMovers[mover], 0f, Engine.DeltaTime / 0.3f,
                                    ((ISourceExtremaEnvironment)Engine).ExtremaArithmetic);
                                if (WindMovers[mover] == 0f)
                                    WindMovers.Remove(mover);
                            }
                        }
                    }

                    QueuedMovers.Clear();
                    foreach (WindMover mover in WindMovers.Keys)
                    {
                        float windSpeed = Strength * 2f *
                                          ((ISourceEasingEnvironment)Engine).EasingArithmetic.CubeInOut(
                                              WindMovers[mover]);

                        if (mover.Entity != null && ((SceneEntityState)mover.Entity).Scene != null)
                        {
                            Vector2 move = Vector2.Zero;
                            switch (Direction)
                            {
                                case PushDirection.Up:
                                    move = new Vector2(0, -windSpeed);
                                    break;

                                case PushDirection.Down:
                                    move = new Vector2(0, windSpeed);
                                    break;

                                case PushDirection.Left:
                                    move = new Vector2(-windSpeed, 0);
                                    break;

                                case PushDirection.Right:
                                    move = new Vector2(windSpeed, 0);
                                    break;
                            }

                            if (mover.Entity.GetType() == typeof(RoomPlayerSession))
                            {
                                if (!MovePlayer((RoomPlayerSession)mover.Entity, move))
                                {
                                    QueuedMovers.Add(mover, 0f);
                                }
                            }
                            else
                            {
                                mover.Move(move);
                            }
                        }
                    }

                    if (QueuedMovers.Count > 0)
                    {
                        foreach (KeyValuePair<WindMover, float> pair in QueuedMovers)
                        {
                            WindMovers[pair.Key] = pair.Value;
                        }
                    }
                }
            }
        }

        private bool MovePlayer(RoomPlayerSession player, Vector2 move)
        {
            Vector2 windDirection = player.Appearance.WindDirection;

            if (player.OnGround() && (player.Data.Motion.Ducking))
            {
                player.Appearance.WindDirection = windDirection;
                return false;
            }

            if (!player.JustRespawned && player.Data.NoWindTimer <= 0f &&
                PlayerFrameMotion.InControl(player.Machine.State) && player.Machine.State != 4 &&
                player.Machine.State != 2 && player.Machine.State != 10)
            {
                if (move.X != 0f)
                {
                    player.Appearance.WindTimeout = 0.2f;
                    windDirection.X = (float)Math.Sign(move.X);
                    if (liftOffOfGround ||
                        (!SourceTrackedEntityCollision.CheckAt(((ISourceScene)Scene).SourceScene.Entities,
                             "Celeste.Solid", player,
                             player.Position + Vector2.UnitX * (float)(-(float)Math.Sign(move.X)) * 3f) &&
                         player.Machine.State != 1))
                    {
                        if (move.X < 0f)
                        {
                            move.X = Math.Max(move.X,
                                (float)(((OriginalLevelSceneRoot)Scene).Bounds.Left -
                                        (player.ExactPosition.X + player.Collider.Left)));
                        }
                        else
                        {
                            move.X = Math.Min(move.X,
                                (float)(((OriginalLevelSceneRoot)Scene).Bounds.Right -
                                        (player.ExactPosition.X + player.Collider.Right)));
                        }

                        player.MoveH(move.X);
                    }
                }

                if (move.Y != 0f)
                {
                    if (player.OnGround())
                    {
                        if (liftOffOfGround)
                        {
                            player.MoveV(move.Y);
                        }

                        player.Appearance.WindDirection = windDirection;
                        return true;
                    }

                    player.Appearance.WindTimeout = 0.2f;
                    windDirection.Y = (float)Math.Sign(move.Y);

                    if (player.Bottom > (float)((OriginalLevelSceneRoot)Scene).Bounds.Top)
                    {
                        if (player.Machine.State == 1)
                        {
                            if (move.Y <= 0f || player.Data.ClimbNoMoveTimer > 0f)
                            {
                                player.Appearance.WindDirection = windDirection;
                                return false;
                            }

                            move.Y *= 0.4f;
                        }

                        player.MoveV(move.Y);
                    }
                }
            }

            player.Appearance.WindDirection = windDirection;
            return true;
        }
    }
}
