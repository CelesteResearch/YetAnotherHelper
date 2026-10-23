using System.Numerics;
using Celeste.Core.Components;
using Celeste.Core.Display;
using Celeste.Core.Mathematics;
using Celeste.Core.Mathematics.Extrema;
using Celeste.Core.Randomness;
using Celeste.Core.Rendering;
using Celeste.Graphics.Content;
using Celeste.Graphics.Content.Atlas;
using Celeste.Graphics.Rendering;
using Celeste.Mods.YetAnotherHelper.Entities.Wind;

namespace Celeste.Mods.YetAnotherHelper.Presentation.Bubbles
{
    internal class BubbleParticle : SourceComponentState
    {
        public Vector2 Position = Vector2.Zero;

        public BubblePushField BubbleField;

        public AtlasEntry Texture;

        private int FramesAlive = 0;

        private int FramesMaxAlive;

        private Vector2 Origin, End;

        private static readonly string[] TextureNames = new string[] { "a", "b" };

        public BubbleParticle(bool active, bool visible) : base(active, visible)
        {
        }

        public override void Added(object entity)
        {
            base.Added(entity);

            BubbleField = (BubblePushField)entity;
            Position = BubbleField.Position;

            Texture = ((ISourceGameGraphicsEnvironment)BubbleField.Engine).GameGraphics.GameAtlas[
                "particles/YetAnotherHelper/bubble_" +
                TextureNames[((ISourceRandomEnvironment)BubbleField.Engine).Random.Next(0, 2)]];

            // Determine bubble spawn point
            switch (BubbleField.Direction)
            {
                case PushDirection.Up:
                    Origin = new Vector2(
                        SourceRandomMath.Range(((ISourceRandomEnvironment)BubbleField.Engine).Random,
                            BubbleField.BottomLeft.X, BubbleField.BottomRight.X), BubbleField.BottomCenter.Y);
                    End = new Vector2(
                        SourceRandomMath.Range(((ISourceRandomEnvironment)BubbleField.Engine).Random,
                            BubbleField.TopLeft.X, BubbleField.TopRight.X), BubbleField.TopCenter.Y);
                    FramesMaxAlive = (int)SourceRandomMath.Range(((ISourceRandomEnvironment)BubbleField.Engine).Random,
                        20, BubbleField.Height / BubbleField.Strength * .5f);
                    break;

                case PushDirection.Down:
                    Origin = new Vector2(
                        SourceRandomMath.Range(((ISourceRandomEnvironment)BubbleField.Engine).Random,
                            BubbleField.TopLeft.X, BubbleField.TopRight.X), BubbleField.TopCenter.Y);
                    End = new Vector2(
                        SourceRandomMath.Range(((ISourceRandomEnvironment)BubbleField.Engine).Random,
                            BubbleField.BottomLeft.X, BubbleField.BottomRight.X), BubbleField.BottomCenter.Y);
                    FramesMaxAlive = (int)SourceRandomMath.Range(((ISourceRandomEnvironment)BubbleField.Engine).Random,
                        20, BubbleField.Height / BubbleField.Strength * .5f);
                    break;

                case PushDirection.Right:
                    Origin = new Vector2(BubbleField.CenterLeft.X,
                        SourceRandomMath.Range(((ISourceRandomEnvironment)BubbleField.Engine).Random,
                            BubbleField.BottomLeft.Y, BubbleField.TopLeft.Y));
                    End = new Vector2(BubbleField.CenterRight.X,
                        SourceRandomMath.Range(((ISourceRandomEnvironment)BubbleField.Engine).Random,
                            BubbleField.BottomRight.Y, BubbleField.TopRight.Y));
                    FramesMaxAlive = (int)SourceRandomMath.Range(((ISourceRandomEnvironment)BubbleField.Engine).Random,
                        20, BubbleField.Width / BubbleField.Strength * .5f);
                    break;

                case PushDirection.Left:
                    Origin = new Vector2(BubbleField.CenterRight.X,
                        SourceRandomMath.Range(((ISourceRandomEnvironment)BubbleField.Engine).Random,
                            BubbleField.BottomRight.Y, BubbleField.TopRight.Y));
                    End = new Vector2(BubbleField.CenterLeft.X,
                        SourceRandomMath.Range(((ISourceRandomEnvironment)BubbleField.Engine).Random,
                            BubbleField.BottomLeft.Y, BubbleField.TopLeft.Y));
                    FramesMaxAlive = (int)SourceRandomMath.Range(((ISourceRandomEnvironment)BubbleField.Engine).Random,
                        20, BubbleField.Width / BubbleField.Strength * .5f);
                    break;
            }

            Position = Origin;
        }

        public override void Update()
        {
            base.Update();

            if (FramesAlive == FramesMaxAlive)
                RemoveSelf();

            FramesAlive++;

            if (BubbleField.Direction == PushDirection.Up || BubbleField.Direction == PushDirection.Down)
            {
                Position.X = ScalarMath.Approach(Position.X, End.X, BubbleField.Strength / 5,
                    ((ISourceExtremaEnvironment)BubbleField.Engine).ExtremaArithmetic);
                Position.Y = ScalarMath.Approach(Position.Y, End.Y, BubbleField.Strength * 2,
                    ((ISourceExtremaEnvironment)BubbleField.Engine).ExtremaArithmetic);
            }
            else
            {
                Position.X = ScalarMath.Approach(Position.X, End.X, BubbleField.Strength * 2,
                    ((ISourceExtremaEnvironment)BubbleField.Engine).ExtremaArithmetic);
                Position.Y = ScalarMath.Approach(Position.Y, End.Y, BubbleField.Strength / 5,
                    ((ISourceExtremaEnvironment)BubbleField.Engine).ExtremaArithmetic);
            }
        }

        public override void Render(ISourceRenderContext context)
        {
            SourceAtlasDrawing.DrawCentered(context, Entity, Texture, Position, PackedColor.White);
        }
    }
}
