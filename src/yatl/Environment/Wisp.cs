using OpenTK;

namespace yatl.Environment
{
    sealed class Wisp : Unit
    {
        private readonly ControlScheme controls;

        public Wisp(GameState game, Vector2 position)
            : base(game, position, Settings.Game.Wisp.FrictionCoefficient)
        {
            this.controls = new ControlScheme();
        }

        public override void Update(GameUpdateEventArgs e)
        {
            var acceleration = new Vector2(
                this.controls.Right.AnalogAmount - this.controls.Left.AnalogAmount,
                this.controls.Up.AnalogAmount - this.controls.Down.AnalogAmount
                );
            var a = acceleration.Length;
            if (a > 0)
                acceleration /= a;

            this.velocity += acceleration * Settings.Game.Wisp.Acceleration * e.ElapsedTimeF;

            base.Update(e);
        }
    }
}