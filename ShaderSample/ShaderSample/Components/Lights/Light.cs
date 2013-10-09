using Microsoft.Xna.Framework;
using ShaderSample.Components.Models;

namespace ShaderSample.Components.Lights
{
	/// <summary>
	/// This is a game component that implements IUpdateable.
	/// </summary>
	public class Light : Microsoft.Xna.Framework.GameComponent
	{
		#region Fields

		// Enumeration für die möglichen Rotationsachsen
		public enum RotationAxis { X, Y, Z, None };

		public Matrix ViewMatrix { get; set; }
		public Matrix ProjectionMatrix { get; set; }

		public Vector3 Position { get; set; }
		public Vector3 LookAt { get; set; }

		// Lichtstärke
		public float DiffuseLightPower { get; set; }
		public float SpecularLightPower { get; set; }

		public bool Rotate { get; set; }

		// Flag ob das Licht ein- oder ausgeschaltet ist
		private bool power;
		public bool Power
		{
			get { return power; }
			set { power = value; lamp.On = value; }
		}

		// Flag ob Self-Shading verwendet wird oder nicht
		public bool SelfShading { get; set; }

		// Spot-Light an- bzw. ausschalten
		public bool SpotLight { get; set; }
		// Der Öffnungswinkel des Spot-Lights
		public float CutOffAngle { get; set; }
		// Die Reichweite des Lichtes
		public float AttenuationDistance { get; set; }

		// zugehöriges Lampen-Model
		private Lamp lamp;

		private Vector3 upVector;

		// Rotationsachse
		private RotationAxis rotationAxis;

		// Blickrichtung
		private float direction;

		// Settings
		private const float nearPlane = 1.0f;
		private const float farPlane = 100.0f;

		private const float defaultDiffuseLightPower = 0.6f;
		private const float defaultSpecualarLightPower = 0.8f;

		private const float defaultCutOffAngle = 0.15f;
		private const float defaultLightAttenuationDistance = 50.0f;

		private const float moveSpeed = 0.2f;

		#endregion

		#region Setup

		public Light(Game game, Lamp lamp, RotationAxis rotationAxis, Vector3 position)
			: base(game)
		{
			this.lamp = lamp;
			this.rotationAxis = rotationAxis;
			Power = true;
			DiffuseLightPower = defaultDiffuseLightPower;
			SpecularLightPower = defaultSpecualarLightPower;
			Position = position;
			LookAt = new Vector3(0.01f, 0.0f, 0.0f);
			CutOffAngle = defaultCutOffAngle;
			SpotLight = false;
			SelfShading = true;
			upVector = Vector3.Up;
			Rotate = false;
			AttenuationDistance = defaultLightAttenuationDistance;
			direction = 1.0f;
		}

		/// <summary>
		/// Allows the game component to perform any initialization it needs to before starting
		/// to run.  This is where it can query for any required services and load content.
		/// </summary>
		public override void Initialize()
		{
			ViewMatrix = Matrix.CreateLookAt(Position, LookAt, upVector);
			ProjectionMatrix = Matrix.CreatePerspectiveFieldOfView(
				MathHelper.PiOver2, 1.0f, nearPlane, farPlane);

			base.Initialize();
		}

		#endregion


		/// <summary>
		/// Allows the game component to update itself.
		/// </summary>
		/// <param name="gameTime">Provides a snapshot of timing values.</param>
		public override void Update(GameTime gameTime)
		{
			// Wenn rotiert werden soll
			if (Rotate)
			{
				float timeDifference = (float)gameTime.ElapsedGameTime.TotalMilliseconds / 1000.0f;
				float rotation = 0.0f;

				// Rotation ermitteln
				rotation = moveSpeed * timeDifference * direction;

				if (rotation != 0.0f)
				{
					Matrix rotationMatrix = Matrix.Identity;

					// Rotationsmatrix je nach Achse berechnen
					switch (rotationAxis)
					{
						case RotationAxis.X:
							rotationMatrix = Matrix.CreateRotationX(rotation);
							break;
						case RotationAxis.Y:
							rotationMatrix = Matrix.CreateRotationY(rotation);
							break;
						case RotationAxis.Z:
							rotationMatrix = Matrix.CreateRotationZ(rotation);
							break;
					}

					// Zugehöriges Lampen-Model mitrotieren
					lamp.WorldMatrix *= rotationMatrix;
					Position = Vector3.Transform(Position, rotationMatrix);
					upVector = Vector3.Transform(upVector, rotationMatrix);
					ViewMatrix = Matrix.CreateLookAt(Position, LookAt, upVector);
					// Ist das Licht zu weit unten, dreht es wieder um
					if (Position.Y < 1.0f)
						direction *= -1.0f;
				}
			}

			base.Update(gameTime);
		}
	}
}
