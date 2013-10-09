using Microsoft.Xna.Framework;

namespace Playroom.Cameras
{
	// Interface für alle Kameras
	public interface ICamera
	{
		Matrix ViewMatrix { get; }
		Matrix ProjectionMatrix { get; }
		Vector3 Position { get; }
		Vector3 Direction { get; }
		float NearPlane { get; }
		float FarPlane { get; }
	}
}
