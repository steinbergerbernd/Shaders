using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Playroom.Cameras
{
	// Flugkamera
	class AutoCamera : Camera
	{
		// Weg- und Blickpunkte des Kamerafluges
		private List<Vector3> wayPoints;
		private List<Vector3> lookAtPoints;

		// Vergangene Zeit
		private float timeDifference;

		// Flugbahnparameter
		private const float frequency = 0.3f;
		private const float tension = 0.0f;
		private const float continuity = 0.0f;
		private const float bias = 0.0f;

		// Index des Start- und Endpunktes
		private int indexSource;
		private int indexTarget;

		// Die Tangenten des Start- und Endpunktes
		private Vector3 sourceTangent;
		private Vector3 targetTangent;

		// Roatation in Quaternionen
		private Quaternion sourceRotation;
		private Quaternion targetRotation;

		public AutoCamera(Game game)
			: base(game)
		{
			wayPoints = new List<Vector3>();
			lookAtPoints = new List<Vector3>();
			timeDifference = 0;
		}

		// Initialisierung der Flugbahn
		public override void Initialize()
		{
			// Ausgangsposition
			Position = new Vector3(0, 8, 4);
			lookAt = new Vector3(0, 8, 0);
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

			// Wegpunkte anlegen
			wayPoints.Add(new Vector3(0, 8, 4));
			wayPoints.Add(new Vector3(-6, 8, 0));
			wayPoints.Add(new Vector3(0, 10, -6));
			wayPoints.Add(new Vector3(6, 8, 0));
			wayPoints.Add(new Vector3(9, 9, 0));
			wayPoints.Add(new Vector3(0, 12, 0));
			wayPoints.Add(new Vector3(0, 12, 4));
			wayPoints.Add(new Vector3(-4, 12, 0));
			wayPoints.Add(new Vector3(-4, 2, 0));
			wayPoints.Add(new Vector3(0, 4, 0));

			// Blickpunkte anlegen
			lookAtPoints.Add(new Vector3(0, 8, 0));
			lookAtPoints.Add(new Vector3(0, 12, 0));
			lookAtPoints.Add(new Vector3(5, 0, 0));
			lookAtPoints.Add(new Vector3(12, 6, 0));
			lookAtPoints.Add(new Vector3(0, 8, 10));
			lookAtPoints.Add(new Vector3(-0.1f, 8, 0));
			lookAtPoints.Add(new Vector3(0, 8, -4));
			lookAtPoints.Add(new Vector3(-8, 7, 0));
			lookAtPoints.Add(new Vector3(8, 2, -8));
			lookAtPoints.Add(new Vector3(0, 6, -2));

			indexSource = 0;
			indexTarget = 1;

			// Tangenten berechnen
			sourceTangent = CalculateOutgoingTangent(indexSource);
			targetTangent = CalculateIncomingTangent(indexTarget);

			// Rotationen berechnen
			sourceRotation = Quaternion.CreateFromRotationMatrix(Matrix.CreateWorld(Vector3.Zero, Vector3.Normalize(lookAtPoints[indexSource] - wayPoints[indexSource]), Vector3.Up));
			targetRotation = Quaternion.CreateFromRotationMatrix(Matrix.CreateWorld(Vector3.Zero, Vector3.Normalize(lookAtPoints[indexTarget] - wayPoints[indexTarget]), Vector3.Up));

			base.Initialize();
		}

		// Eingehende Tangente eines Punktes berechnen
		private Vector3 CalculateIncomingTangent(int index)
		{
			// vorherigen und nächsten Index ermitteln
			int previousIndex = index - 1;
			if (previousIndex == -1)
				previousIndex = wayPoints.Count - 1;
			int nextIndex = index + 1;
			if (nextIndex == wayPoints.Count)
				nextIndex = 0;

			// Tangente berechnen
			return (((1 - tension) * (1 - continuity) * (1 + bias)) / 2) * (wayPoints[index] - wayPoints[previousIndex]) +
				(((1 - tension) * (1 + continuity) * (1 - bias)) / 2) * (wayPoints[nextIndex] - wayPoints[index]);
		}

		// Ausgehende Tangente eines Punktes berechnen
		private Vector3 CalculateOutgoingTangent(int index)
		{
			// vorherigen und nächsten Index ermitteln
			int previousIndex = index - 1;
			if (previousIndex == -1)
				previousIndex = wayPoints.Count - 1;
			int nextIndex = index + 1;
			if (nextIndex == wayPoints.Count)
				nextIndex = 0;

			// Tangente berechnen
			return (((1 - tension) * (1 + continuity) * (1 + bias)) / 2) * (wayPoints[index] - wayPoints[previousIndex]) +
				(((1 - tension) * (1 - continuity) * (1 - bias)) / 2) * (wayPoints[nextIndex] - wayPoints[index]);
		}

		public override void Update(GameTime gameTime)
		{
			// vergangene Zeit aufsummieren
			timeDifference += (float)gameTime.ElapsedGameTime.TotalMilliseconds / 1000.0f;

			// Fortschritt der Kurve berechnen
			float age = timeDifference * frequency;

			// Nie über das Kurvenende hinausschießen
			if (age > 1.0f)
				age = 1.0f;

			// Mit Hilfe der Hermiteinterpolation die akutelle Position ermitteln
			Position = Vector3.Hermite(wayPoints[indexSource], sourceTangent, wayPoints[indexTarget], targetTangent, age);

			// Die Rotation interpolieren
			Quaternion interpolatedRotation = Quaternion.Slerp(sourceRotation, targetRotation, age);
			
			// Vektoren berechnen
			upVector = Vector3.Normalize(Vector3.Transform(Vector3.Up, interpolatedRotation));
			Direction = Vector3.Normalize(Vector3.Transform(Vector3.Forward, interpolatedRotation));

			rightVector = Vector3.Cross(Direction, upVector);
			upVector = Vector3.Cross(rightVector, Direction);

			lookAt = Position + Direction;

			ViewMatrix = Matrix.CreateLookAt(
				Position,
				lookAt,
				upVector
				);

			// Wenn das Ende der Kurve erreicht ist
			if (age == 1.0f)
			{
				// Nächster Weg- und Blickpunkt
				++indexSource;
				if (indexSource == wayPoints.Count)
					indexSource = 0;
				++indexTarget;
				if (indexTarget == wayPoints.Count)
					indexTarget = 0;
				timeDifference = 0;

				// Beide Tangenten berechnen
				sourceTangent = CalculateOutgoingTangent(indexSource);
				targetTangent = CalculateIncomingTangent(indexTarget);

				// Rotationen berechnen
				sourceRotation = Quaternion.CreateFromRotationMatrix(Matrix.CreateWorld(Vector3.Zero, Vector3.Normalize(lookAtPoints[indexSource] - wayPoints[indexSource]), Vector3.Up));
				targetRotation = Quaternion.CreateFromRotationMatrix(Matrix.CreateWorld(Vector3.Zero, Vector3.Normalize(lookAtPoints[indexTarget] - wayPoints[indexTarget]), Vector3.Up));
			}

			base.Update(gameTime);
		}
	}
}
