using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Graphics.PackedVector;

namespace ShaderSample.Components.ParticleSystems
{
	// Struktur für einen Vertex des Partikelsystems
	public struct ParticleVertex
	{
		// Die Felder
		public Short2 Corner { get; set; }
		public Vector3 Position { get; set; }
		public Vector3 Velocity { get; set; }
		public Color Random { get; set; }
		public float CreationTime { get; set; }

		// Die Vertex Declaration für den Vertex Buffer
		public static readonly VertexDeclaration VertexDeclaration = new VertexDeclaration
		(
			new VertexElement(0, VertexElementFormat.Short2, VertexElementUsage.Position, 0),
			new VertexElement(4, VertexElementFormat.Vector3, VertexElementUsage.Position, 1),
			new VertexElement(16, VertexElementFormat.Vector3, VertexElementUsage.Normal, 0),
			new VertexElement(28, VertexElementFormat.Color, VertexElementUsage.Color, 0),
			new VertexElement(32, VertexElementFormat.Single, VertexElementUsage.TextureCoordinate, 0)
		);

		// Größe in Bytes der Vertexstruktur
		public const int SizeInBytes = 36;
	}
}