using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Graphics.PackedVector;
using Playroom.Cameras;

namespace ShaderSample.Components.ParticleSystems
{
	/// <summary>
	/// Ein Partikelsystem für eine Explosion
	/// </summary>
	public class ParticleSystem : DrawableGameComponent
	{
		#region Fields

		// Liste aller Partiekl
		private List<ParticleVertex> particleList;

		// Die Ecken eines quadratischen Partikels
		private Short2[] corners;

		// Aktuelle Zeit für das Partikelsystem
		private float currentTime;

		// Vertex- und Index Buffer mit den Partikelvertices
		private VertexBuffer vertexBuffer;
		private IndexBuffer indexBuffer;

		// Der HLSL Shader für das Zeichnen des Partikelsystems
		private Effect particleEffect;
		
		// Textur für die Partikel
		private Texture2D particleTexture;

		// Settings
		// maximale Anzahl an Partikeln
		private const int maxParticles = 300;

		// Geschwindigkeit der Partikel horizontal bzw. vertikal
		private const float minHorizontalVelocity = 0;
		private const float maxHorizontalVelocity = 1;
		private const float minVerticalVelocity = -1;
		private const float maxVerticalVelocity = 1;

		// Lebensdauer eines Partikels
		private const float maxParticleLifeTime = 2.0f;

		// Farben zur Ausdünnung der Texturfarbe
		private Color minColor = Color.DarkGray;
		private Color maxColor = Color.Gray;

		// Rotation der Partikel
		private const float minRotationSpeed = -1;
		private const float maxRotationSpeed = 1;

		// Anfangs- und Zielgröße der Partikel
		private const float minStartSize = 1.0f;
		private const float maxStartSize = 1.0f;
		private const float minEndSize = 4.0f;
		private const float maxEndSize = 6.0f;

		// Random-Generator
		private static Random random = new Random();

		#endregion

		#region Setup

		public ParticleSystem(Game game)
			: base(game)
		{
			particleList = new List<ParticleVertex>();

			corners = new Short2[4];

			currentTime = 0;
		}

		protected override void LoadContent()
		{
			// HLSL Shader laden
			particleEffect = Game.Content.Load<Effect>("Effects/ParticleEffect");
			// Textur laden
			particleTexture = Game.Content.Load<Texture2D>("Textures/explosion");

			// statische Einstellungsparameter für den Shader setzen
			EffectParameterCollection parameters = particleEffect.Parameters;

			parameters["MaxParticleLifeTime"].SetValue(maxParticleLifeTime);
			parameters["MinColor"].SetValue(minColor.ToVector4());
			parameters["MaxColor"].SetValue(maxColor.ToVector4());

			parameters["RotationSpeed"].SetValue(new Vector2(minRotationSpeed, maxRotationSpeed));

			parameters["StartSize"].SetValue(new Vector2(minStartSize, maxStartSize));

			parameters["EndSize"].SetValue(
				 new Vector2(minEndSize, maxEndSize));

			parameters["ParticleTexture"].SetValue(particleTexture);

			base.LoadContent();
		}

		/// <summary>
		/// Allows the game component to perform any initialization it needs to before starting
		/// to run.  This is where it can query for any required services and load content.
		/// </summary>
		public override void Initialize()
		{
			base.Initialize();

			// Die vier Ecken eines quadratischen Partikels
			corners[0] = new Short2(-1, -1);
			corners[1] = new Short2(1, -1);
			corners[2] = new Short2(1, 1);
			corners[3] = new Short2(-1, 1);

			// Erstellen des Vertex Buffers
			vertexBuffer = new VertexBuffer(GraphicsDevice, ParticleVertex.VertexDeclaration,
				maxParticles * 4, BufferUsage.WriteOnly);

			// Erstellen der Indizes
			ushort[] indices = new ushort[maxParticles * 6];

			for (int i = 0; i < maxParticles; i++)
			{
				indices[i * 6 + 0] = (ushort)(i * 4 + 0);
				indices[i * 6 + 1] = (ushort)(i * 4 + 1);
				indices[i * 6 + 2] = (ushort)(i * 4 + 2);

				indices[i * 6 + 3] = (ushort)(i * 4 + 0);
				indices[i * 6 + 4] = (ushort)(i * 4 + 2);
				indices[i * 6 + 5] = (ushort)(i * 4 + 3);
			}

			// Einlesen der Indizes in den Index Buffer
			indexBuffer = new IndexBuffer(GraphicsDevice, typeof(ushort), indices.Length, BufferUsage.WriteOnly);
			indexBuffer.SetData(indices);
		}

		#endregion

		/// <summary>
		/// Allows the game component to update itself.
		/// </summary>
		/// <param name="gameTime">Provides a snapshot of timing values.</param>
		public override void Update(GameTime gameTime)
		{
			// aktuelle Zeit des Partikelsystems
			currentTime += (float)gameTime.ElapsedGameTime.TotalSeconds;

			// "alte" Partikel löschen (Lebensdauer überschritten)
			for (int i = 0; i < particleList.Count; ++i)
				if (currentTime - particleList[i].CreationTime > maxParticleLifeTime)
					particleList.RemoveAt(i--);

			// Wenn keine Partikel mehr da sind, kann die Zeit zurückgesetzt werden
			if (particleList.Count == 0)
				currentTime = 0;

			base.Update(gameTime);
		}

		public override void Draw(GameTime gameTime)
		{
			GraphicsDevice device = GraphicsDevice;

			// Nur Zeichnen wenn zumindes ein ganzer Partikel in der Liste ist
			if (particleList.Count > 3)
			{
				EffectParameterCollection parameters = particleEffect.Parameters;
				ICamera camera = (ICamera)Game.Services.GetService(typeof(ICamera));

				// Shader Parameter setzen
				parameters["View"].SetValue(camera.ViewMatrix);
				parameters["Projection"].SetValue(camera.ProjectionMatrix);
				// ViewportScale für die Anpassung an der Partikelgröße an das Seitenverhältnis
				parameters["CurrentTime"].SetValue(currentTime);

				// Für durchseinende Explosionen
				device.BlendState = BlendState.Additive;
				// Der Tiefenbuffer wird nicht mehr verändert
				device.DepthStencilState = DepthStencilState.DepthRead;
				device.RasterizerState = RasterizerState.CullNone;

				// Vertices hochladen
				vertexBuffer.SetData(particleList.ToArray());
				// VertexBuffer und Indexbuffer zuweisen
				device.SetVertexBuffer(vertexBuffer);
				device.Indices = indexBuffer;

				// Partikelsystem zeichnen
				foreach (EffectPass pass in particleEffect.CurrentTechnique.Passes)
				{
					pass.Apply();

					device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, particleList.Count, 0, particleList.Count / 2);
				}

				// Standardwerte wieder setzen
				device.DepthStencilState = DepthStencilState.Default;
				device.BlendState = BlendState.Opaque;
				device.RasterizerState = RasterizerState.CullCounterClockwise;
			}

			base.Draw(gameTime);
		}

		// Hinzufügen eines Partikels zum System
		public void AddParticle(Vector3 position, Vector3 velocity)
		{
			// Nur wenn noch Paltz in der Liste ist
			if (maxParticles * 4 <= particleList.Count)
				return;

			// Geschwindigkeit für horizontale Ausdehnung zufällig ermitteln
			float horizontalVelocity = MathHelper.Lerp(minHorizontalVelocity,
																	 maxHorizontalVelocity,
																	 (float)random.NextDouble());

			// zufällige Ausrichtung in x- und z-Richtung bestimmen
			double horizontalAngle = random.NextDouble() * MathHelper.TwoPi;

			// Geschwindigkeitsvektor anpassen
			velocity.X += horizontalVelocity * (float)Math.Cos(horizontalAngle);
			velocity.Z += horizontalVelocity * (float)Math.Sin(horizontalAngle);

			// Geschwindigkeit für horizontale Ausdehnung zufällig ermitteln
			velocity.Y += MathHelper.Lerp(minVerticalVelocity,
													maxVerticalVelocity,
													(float)random.NextDouble());

			// Vier Zufallszahlen ermitteln, um im Shader den Partikeln unterschiedliche Größen, Rotationen und Farben zu verpassen
			Color randomValues = new Color((byte)random.Next(255),
													 (byte)random.Next(255),
													 (byte)random.Next(255),
													 (byte)random.Next(255));

			// Die Vertices des Partikels erstellen
			for (int i = 0; i < 4; i++)
			{
				ParticleVertex particle = new ParticleVertex();
				particle.Corner = corners[i];
				particle.Position = position;
				particle.Velocity = velocity;
				particle.Random = randomValues;
				particle.CreationTime = currentTime;
				particleList.Add(particle);
			}
		}
	}
}
