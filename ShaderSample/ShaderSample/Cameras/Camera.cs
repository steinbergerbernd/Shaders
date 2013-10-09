using Microsoft.Xna.Framework;

namespace Playroom.Cameras
{
	// Basisklasse für alle Kameras
	public abstract class Camera : GameComponent, ICamera
	{
		public float FarPlane { get; set; }
		public float NearPlane { get; set; }

		public Matrix ViewMatrix { get; set; }
		public Matrix ProjectionMatrix { get; set; }

		public Vector3 Position { get; set; }
		public Vector3 Direction { get; protected set; }

		protected Vector3 lookAt;
		protected Vector3 upVector;
		protected Vector3 rightVector;

		private const float defaultNearPlane = 0.1f;
		private const float defaultFarPlane = 100.0f;

		// Konstruktor
		public Camera(Game game) : base(game)
		{
			ProjectionMatrix = Matrix.Identity;
			ViewMatrix = Matrix.Identity;

			Position = Vector3.Zero;
			Direction = Vector3.Zero;
			lookAt = Vector3.Zero;
			upVector = Vector3.Zero;
			rightVector = Vector3.Zero;

			FarPlane = defaultFarPlane;
			NearPlane = defaultNearPlane;
		}

		// Positioniert die Kamera und berechnet die neue View-Matrix
		public void PositionCamera(Vector3 position, Vector3 lookAt, Vector3 upVector)
		{
			this.Position = position;
			this.lookAt = lookAt;
			this.upVector = upVector;

			ViewMatrix = Matrix.CreateLookAt(
				Position,
				lookAt,
				upVector);
		}
	}
}
