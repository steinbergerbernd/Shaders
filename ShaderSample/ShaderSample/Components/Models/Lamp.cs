using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;


namespace ShaderSample.Components.Models
{
	/// <summary>
	/// This is a game component that implements IUpdateable.
	/// </summary>
	public class Lamp : ShaderModel
	{
		// Flag ob Lampe ein- oder ausgeschaltet ist
		public bool On { get; set; }

		public Lamp(Game game, Matrix worldMatrix)
			: base(game)
		{
			Name = "Lamp";
			On = true;
			WorldMatrix = worldMatrix;
		}

		/// <summary>
		/// Allows the game component to perform any initialization it needs to before starting
		/// to run.  This is where it can query for any required services and load content.
		/// </summary>
		public override void Initialize()
		{
			base.Initialize();
		}

		protected override void LoadContent()
		{
			DrawingModel = Game.Content.Load<Model>("Models/spotlight");

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
