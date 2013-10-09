using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace Playroom.Cameras
{
	/// <summary>
	/// This is a game component that implements IUpdateable.
	/// </summary>
	public class FreeCamera : Camera
	{
		// Geschwindigkeit bei automatischer Rotation
		private const float rotationSpeed = 0.3f;
		// Geschwindigkeit für die manuelle Beweg2ung
		private const float moveSpeed = 3.0f;

		private MouseState originalMouseState;

		// Konstruktor
		public FreeCamera(Game game)
			: base(game)
		{
		}

		//Initialisieren der Anfangswerte
		public override void Initialize()
		{
			Position = new Vector3(0, 5, 0);
			lookAt = new Vector3(0, 5, -1);
			upVector = Vector3.Up;
			rightVector = Vector3.Right;
			Direction = Vector3.Forward;

			ProjectionMatrix = Matrix.CreatePerspectiveFieldOfView(
				MathHelper.ToRadians(45.0f),
				Game.GraphicsDevice.Viewport.AspectRatio,
				NearPlane,
				FarPlane);

			ViewMatrix = Matrix.CreateLookAt(
				Position,
				lookAt,
				upVector
				);

			//Setzen der Mauszeigerposition auf die Mitte des Schirmes
			Mouse.SetPosition(Game.GraphicsDevice.Viewport.Width / 2, Game.GraphicsDevice.Viewport.Height / 2);
			originalMouseState = Mouse.GetState();

			base.Initialize();
		}


		// Verarbeitung der Mausbewegungen und der Tastaturbefehle
		private void ProcessInput(float timeDifference)
		{
			bool updateViewMatrix = false;
			Vector3 moveVector = Vector3.Zero;
			MouseState mouseState = Mouse.GetState();
			KeyboardState keyboardState = Keyboard.GetState();

			// Wenn die Maus bewegt wurde
			if (mouseState != originalMouseState)
			{
				//Prüfen der neuen Mausposition
				float xDifference = mouseState.X - originalMouseState.X;
				float yDifference = mouseState.Y - originalMouseState.Y;

				//Rotation nach Mausbewegung und vergangener Zeit setzen
				float xRotation = rotationSpeed * -yDifference * timeDifference;
				float yRotation = rotationSpeed * -xDifference * timeDifference;

				//Maus wieder in die Mitte setzen
				Mouse.SetPosition(
					Game.GraphicsDevice.Viewport.Width / 2,
					Game.GraphicsDevice.Viewport.Height / 2);

				Matrix rotationMatrixX = Matrix.Identity;
				Matrix rotationMatrixY = Matrix.Identity;
				Matrix rotationMatrix;

				// Die Rotationsmatrizen der Mausbewegung berechnen
				rotationMatrixX *= Matrix.CreateFromAxisAngle(rightVector, xRotation);
				rotationMatrixY *= Matrix.CreateFromAxisAngle(Vector3.Up, yRotation);
				rotationMatrix = rotationMatrixX * rotationMatrixY;

				// Den neuen Up-Vektor durch Rotation berechnen
				Vector3 newUpVector = Vector3.Normalize(Vector3.Transform(upVector, rotationMatrix));

				// Der Up-Vektor darf niemals ins negative (nach unten) zeigen
				if (newUpVector.Y <= 0.0f)
					rotationMatrix = rotationMatrixY;

				upVector = newUpVector;
				// Forward-Vektor durch Rotation berechnen
				Direction = Vector3.Normalize(Vector3.Transform(Direction, rotationMatrix));
				// Right-Vektor durch Rotation berechnen
				rightVector = Vector3.Normalize(Vector3.Transform(rightVector, rotationMatrix));

				// Right- und Up-Vektor über Normalvektoren neu berechnen, um Ungenauigkeiten auszugleichen
				rightVector = Vector3.Cross(Direction, newUpVector);
				upVector = Vector3.Cross(rightVector, Direction);

				lookAt = Position + Direction;

				updateViewMatrix = true;
			}

			// Tastatureingaben auswerten
			if (keyboardState.IsKeyDown(Keys.W))
				moveVector += new Vector3(0, 0, 1);
			if (keyboardState.IsKeyDown(Keys.S))
				moveVector += new Vector3(0, 0, -1);
			if (keyboardState.IsKeyDown(Keys.D))
				moveVector += new Vector3(1, 0, 0);
			if (keyboardState.IsKeyDown(Keys.A))
				moveVector += new Vector3(-1, 0, 0);

			// Bei Bewegungen Kamera neu positionieren
			if (moveVector != Vector3.Zero)
			{
				Position += timeDifference * moveSpeed * (moveVector.X * rightVector);
				Position += timeDifference * moveSpeed * (moveVector.Z * Direction);
				lookAt = Position + Direction;

				updateViewMatrix = true;
			}

			// Werte aktualisieren
			if(updateViewMatrix)
				ViewMatrix = Matrix.CreateLookAt(
				Position,
				lookAt,
				upVector);
		}

		public override void Update(GameTime gameTime)
		{
			//Eingabeverarbeitung mit vergangener Zeit
			float timeDifference = (float)gameTime.ElapsedGameTime.TotalMilliseconds / 1000.0f;
			ProcessInput(timeDifference);

			base.Update(gameTime);
		}
	}
}