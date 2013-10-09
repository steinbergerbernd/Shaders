using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ShaderSample.Components.Models
{
	/// <summary>
	/// This is a game component that implements IUpdateable.
	/// </summary>
	public class Chair : ShaderModel
	{
		public Chair(Game game)
			: base(game)
		{
			Name = "Chair";
		}

		/// <summary>
		/// Allows the game component to perform any initialization it needs to before starting
		/// to run.  This is where it can query for any required services and load content.
		/// </summary>
		public override void Initialize()
		{
			WorldMatrix = Matrix.CreateRotationX(MathHelper.ToRadians(90)) *
				Matrix.CreateTranslation(4.0f, 2.15f, -5.0f);

			base.Initialize();
		}

		protected override void LoadContent()
		{
			DrawingModel = Game.Content.Load<Model>("Models/chair");

			base.LoadContent();
		}

		/// <summary>
		/// Allows the game component to update itself.
		/// </summary>
		/// <param name="gameTime">Provides a snapshot of timing values.</param>
		public override void Update(GameTime gameTime)
		{
			base.Update(gameTime);
		}
	}
}