using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ShaderSample.Components.Models
{
	public class SpaceShip : ShaderModel
	{
		public SpaceShip(Game game)
			: base(game)
		{
			Name = "SpaceShip";
		}

		/// <summary>
		/// Allows the game component to perform any initialization it needs to before starting
		/// to run.  This is where it can query for any required services and load content.
		/// </summary>
		public override void Initialize()
		{
			WorldMatrix = Matrix.CreateScale(0.008f) * Matrix.CreateTranslation(0, 5.0f, 0);

			base.Initialize();
		}

		protected override void LoadContent()
		{
			DrawingModel = Game.Content.Load<Model>("Models/spaceShip");
			NormalMapTexture = Game.Content.Load<Texture2D>("Textures/normalMap");
			DisplacementMapTexture = Game.Content.Load<Texture2D>("Textures/displacementMap");

			base.LoadContent();
		}

		public override void Update(GameTime gameTime)
		{
			base.Update(gameTime);
		}
	}
}
