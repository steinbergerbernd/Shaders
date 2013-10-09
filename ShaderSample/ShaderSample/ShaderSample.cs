using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Playroom.Cameras;
using ShaderSample.Components;
using ShaderSample.Components.Lights;
using ShaderSample.Components.Models;
using ShaderSample.Components.ParticleSystems;

namespace ShaderSample
{
	/// <summary>
	/// This is the main type for your game
	/// </summary>
	public class ShaderSample : Game
	{
		#region Fields

		// Flags für bestimmte Features
		public bool WireFrameMode { get; set; }
		public bool UseNormalMap { get; set; }
		public bool UseDisplacementMap { get; set; }
		public bool Shadows { get; set; }

		// Speichert die Shadow Maps
		public RenderTarget2D ShadowMap { get; set; }

		// Das Licht
		public Light Light { get; set; }

		// Rendertargets für die Shadow-Map Erstellung
		private RenderTarget2D shadowMapRenderTarget;
		private RenderTarget2D pcfMapRenderTarget;
		private RenderTarget2D blurMapHorizontalRenderTarget;
		private RenderTarget2D blurMapVerticalRenderTarget;

		private GraphicsDeviceManager graphics;

		// Kamera zum Umhergehen
		private FreeCamera freeCamera;

		// Vorigen Tastaturstatus merken
		private KeyboardState previousKeyboardState;

		// Verwendete Models
		private Chair chair;
		private SpaceShip spaceShip;
		private Room room;

		// Partikelsystem für eine Explosion
		private ParticleSystem particleSystem;

		// Der Shader-Effekt
		private Effect shadingEffect;
		// Effekt für das Erstellen der Shadow Maps
		private Effect shadowMapEffect;
		// fertiger BasicEffect
		private BasicEffect basicEffect;

		// Abspeichern der Shadow-Map Texturen auf der HD
		private bool saveMaps;

		// Gewichtungen für die Erstellung der Blur-Map
		private float[] sampleWeightsHorizontal;
		private Vector2[] sampleOffsetsHorizontal;
		private float[] sampleWeightsVertical;
		private Vector2[] sampleOffsetsVertical;

		// Anfangs- und Endpunkt des Strahls
		private VertexPositionColor[] rayPointList;
		private int[] rayIndexList;

		// Farbe des Strahls
		private Color rayColor = Color.Red;

		// Der Strahl
		private Ray ray;

		// Der Kollisionspunkt
		private Vector3 rayCollisionPoint;

		// Die Distanz von der Kamera zum Kollisionspunkt
		private float? rayCollisionDistance;

		// Daten für den eingezeichneten Kollisionspunkt
		private VertexPositionColor[] hitPointVertices;
		private short[] hitPointIndices;
		private Matrix[] hitPointMatrices;
		private Color hitPointColor = Color.Black;
		private const float hitPointLength = 0.0045f;

		// Einstellungsparameter
		private const int shadowMapSize = 2048;
		private const int screenWidth = 1024;
		private const int screenHeight = 768;
		private const int explosionParticleCount = 30;

		#endregion


		#region Setup

		public ShaderSample()
		{
			graphics = new GraphicsDeviceManager(this);
			Content.RootDirectory = "Content";

			freeCamera = new FreeCamera(this);
			spaceShip = new SpaceShip(this);
			chair = new Chair(this);
			room = new Room(this);
			particleSystem = new ParticleSystem(this);

			Components.Add(spaceShip);
			Components.Add(chair);
			Components.Add(freeCamera);
			Components.Add(room);
			Components.Add(particleSystem);

			SetupLights();

			Services.AddService(typeof(ICamera), freeCamera);

			WireFrameMode = false;
			UseNormalMap = false;
			UseDisplacementMap = false;
			Shadows = false;
			saveMaps = false;

			rayPointList = new VertexPositionColor[2];
			rayIndexList = new int[] { 0, 1 };
			SetupHitPoint();

			// Antialiasing aktivieren
			graphics.PreferMultiSampling = true;
		}

		private void SetupLights()
		{
			Vector3 position = new Vector3(0.0f, 14.5f, 0.0f);
			Lamp lamp = new Lamp(this, Matrix.CreateRotationX(MathHelper.ToRadians(-90)) * Matrix.CreateTranslation(position));
			Light = new Light(this, lamp, Light.RotationAxis.X, position);
			Components.Add(lamp);
			Components.Add(Light);
		}

		/// <summary>
		/// Den zu zeichnenden Kollisionspunkt (Würfel) aufbereiten
		/// </summary>
		private void SetupHitPoint()
		{
			hitPointMatrices = new Matrix[6];

			// Matrizen für die Transformation der sechs Quadrate des Würfels
			hitPointMatrices[0] = Matrix.CreateTranslation(0, 0, hitPointLength);
			hitPointMatrices[1] = Matrix.CreateRotationY(MathHelper.ToRadians(-90.0f)) * Matrix.CreateTranslation(-hitPointLength, 0, 0);
			hitPointMatrices[2] = Matrix.CreateRotationY(MathHelper.ToRadians(180.0f)) * Matrix.CreateTranslation(0, 0, -hitPointLength);
			hitPointMatrices[3] = Matrix.CreateRotationY(MathHelper.ToRadians(90.0f)) * Matrix.CreateTranslation(hitPointLength, 0, 0);
			hitPointMatrices[4] = Matrix.CreateRotationX(MathHelper.ToRadians(-90.0f)) * Matrix.CreateTranslation(0, hitPointLength, 0);
			hitPointMatrices[5] = Matrix.CreateRotationX(MathHelper.ToRadians(90.0f)) * Matrix.CreateTranslation(0, -hitPointLength, 0);

			// Punkte für eine Quadratseite
			hitPointVertices = new VertexPositionColor[4];
			hitPointIndices = new short[] { 0, 1, 2, 3 };
			hitPointVertices[0] = new VertexPositionColor(new Vector3(-hitPointLength, -hitPointLength, 0.0f), hitPointColor);
			hitPointVertices[1] = new VertexPositionColor(new Vector3(-hitPointLength, hitPointLength, 0.0f), hitPointColor);
			hitPointVertices[2] = new VertexPositionColor(new Vector3(hitPointLength, -hitPointLength, 0.0f), hitPointColor);
			hitPointVertices[3] = new VertexPositionColor(new Vector3(hitPointLength, hitPointLength, 0.0f), hitPointColor);
		}

		/// <summary>
		/// Allows the game to perform any initialization it needs to before starting to run.
		/// This is where it can query for any required services and load any non-graphic
		/// related content.  Calling base.Initialize will enumerate through any components
		/// and initialize them as well.
		/// </summary>
		protected override void Initialize()
		{
			// Auflösung setzen
			this.graphics.PreferredBackBufferWidth = screenWidth;
			this.graphics.PreferredBackBufferHeight = screenHeight;

			// kein Vollbildmodus
			graphics.IsFullScreen = false;
			graphics.ApplyChanges();

			PresentationParameters pp = GraphicsDevice.PresentationParameters;

			// Set Anitaliasing to 4x
			pp.MultiSampleCount = 4;

			shadowMapRenderTarget = new RenderTarget2D(GraphicsDevice, shadowMapSize, shadowMapSize, false, SurfaceFormat.Single, DepthFormat.Depth24);
			pcfMapRenderTarget = new RenderTarget2D(GraphicsDevice, screenWidth, screenHeight, false, SurfaceFormat.Color, DepthFormat.Depth24);
			blurMapHorizontalRenderTarget = new RenderTarget2D(GraphicsDevice, screenWidth, screenHeight, false, SurfaceFormat.Color, DepthFormat.Depth24);
			blurMapVerticalRenderTarget = new RenderTarget2D(GraphicsDevice, screenWidth, screenHeight, false, SurfaceFormat.Color, DepthFormat.Depth24);
			ShadowMap = blurMapVerticalRenderTarget;

			Vector2 texelSize = new Vector2(1.0f / screenWidth, 1.0f / screenHeight);
			GetGaussionOffsets(true, texelSize, out sampleWeightsHorizontal, out sampleOffsetsHorizontal);
			GetGaussionOffsets(false, texelSize, out sampleWeightsVertical, out sampleOffsetsVertical);

			basicEffect = new BasicEffect(GraphicsDevice);

			base.Initialize();
		}

		/// <summary>
		/// LoadContent will be called once per game and is the place to load
		/// all of your content.
		/// </summary>
		protected override void LoadContent()
		{
			shadingEffect = Content.Load<Effect>("Effects/Shading");
			shadowMapEffect = Content.Load<Effect>("Effects/ShadowMap");
			Services.AddService(typeof(Effect), shadingEffect);
		}

		#endregion


		/// <summary>
		/// Allows the game to run logic such as updating the world,
		/// checking for collisions, gathering input, and playing audio.
		/// </summary>
		/// <param name="gameTime">Provides a snapshot of timing values.</param>
		protected override void Update(GameTime gameTime)
		{
			// Aktuelle Tastatureingabe
			KeyboardState keyboardState = Keyboard.GetState();

			//Programm beenden
			if (previousKeyboardState.IsKeyUp(Keys.Escape) && keyboardState.IsKeyDown(Keys.Escape))
				Exit();

			// Umschalten zwischen den einzelnen Shadow-Maps
			if (previousKeyboardState.IsKeyUp(Keys.D1) && keyboardState.IsKeyDown(Keys.D1))
				ShadowMap = pcfMapRenderTarget;
			else if (previousKeyboardState.IsKeyUp(Keys.D2) && keyboardState.IsKeyDown(Keys.D2))
				ShadowMap = blurMapHorizontalRenderTarget;
			else if (previousKeyboardState.IsKeyUp(Keys.D3) && keyboardState.IsKeyDown(Keys.D3))
				ShadowMap = blurMapVerticalRenderTarget;

			// Self-Shading ein- bzw. ausschalten
			if (previousKeyboardState.IsKeyUp(Keys.F1) && keyboardState.IsKeyDown(Keys.F1))
				Light.SelfShading = !Light.SelfShading;

			// Normal Mapping ein- bzw. ausschalten
			if (previousKeyboardState.IsKeyUp(Keys.F2) && keyboardState.IsKeyDown(Keys.F2))
				UseNormalMap = !UseNormalMap;

			// Displacement Mapping ein- bzw. ausschalten (Normal Mapping muss eingeschalten sein)
			if (previousKeyboardState.IsKeyUp(Keys.F3) && keyboardState.IsKeyDown(Keys.F3))
				UseDisplacementMap = !UseDisplacementMap;

			// Schatten ein- bzw. ausschalten
			if (previousKeyboardState.IsKeyUp(Keys.F4) && keyboardState.IsKeyDown(Keys.F4))
				Shadows = !Shadows;

			// Lichtrotation ein- bzw. ausschalten
			if (previousKeyboardState.IsKeyUp(Keys.F5) && keyboardState.IsKeyDown(Keys.F5))
				Light.Rotate = !Light.Rotate;

			// Shadow Maps auf der HD speichern
			if (previousKeyboardState.IsKeyUp(Keys.F6) && keyboardState.IsKeyDown(Keys.F6))
				saveMaps = true;

			// Spot Light ein- bzw. ausschalten
			if (previousKeyboardState.IsKeyUp(Keys.F7) && keyboardState.IsKeyDown(Keys.F7))
				Light.SpotLight = !Light.SpotLight;

			// Specular Light ein- bzw. ausschalten
			if (previousKeyboardState.IsKeyUp(Keys.F8) && keyboardState.IsKeyDown(Keys.F8))
			{
				if (Light.SpecularLightPower == 0.8f)
					Light.SpecularLightPower = 0.0f;
				else
					Light.SpecularLightPower = 0.8f;
			}

			// Zwischen Wireframe und Solid umschalten
			if (previousKeyboardState.IsKeyUp(Keys.F9) && keyboardState.IsKeyDown(Keys.F9))
				WireFrameMode = !WireFrameMode;

			// Aussenden eines Strahles, Kollisionserkennung und bei Kollision Aktivierung eines Partikelsystems
			if (previousKeyboardState.IsKeyUp(Keys.Enter) && keyboardState.IsKeyDown(Keys.Enter))
				ShootRay();

			previousKeyboardState = keyboardState;

			base.Update(gameTime);
		}


		#region RayCastAndExplosion

		private void ShootRay()
		{
			ICamera camera = (ICamera)Services.GetService(typeof(ICamera));
			// Startpunkt des Strahles ist die Kameraposition plus der Near Plane
			Vector3 startPoint = camera.Position + camera.Direction * camera.NearPlane;
			rayPointList[0] = new VertexPositionColor(startPoint, rayColor);
			// Strahl entlang der Kamerarichtung
			ray = new Ray(startPoint, camera.Direction);

			rayCollisionPoint = Vector3.Zero;
			rayCollisionDistance = null;

			// Kollisionen gegen die Models prüfen
			CheckRayCollisionAgainstModel(spaceShip);
			CheckRayCollisionAgainstModel(chair);

			// Wurde eine Kollision gefunden, so wird nur bis zur Kollision gezeichnet.
			// Ansonsten wird bis zur Far Plane gezeichnet.
			if (rayCollisionDistance != null)
			{
				rayPointList[1] = new VertexPositionColor(ray.Position + ray.Direction * (float)rayCollisionDistance, rayColor);
				// Bei Kollisionspunkt Explosionspartikel erstellen
				for (int i = 0; i < explosionParticleCount; ++i)
					particleSystem.AddParticle(rayCollisionPoint, Vector3.Zero);
			}
			else
				rayPointList[1] = new VertexPositionColor(camera.Position + camera.Direction * camera.FarPlane, rayColor);
		}

		// Prüfen ob eine Kollision mit einem Model vorliegt
		// (Keine Abbruchbedingung damit alle getroffenen Dreiecke gefunden werden)
		private void CheckRayCollisionAgainstModel(ShaderModel model)
		{
			// Test ob der Strahl die BoundingSphere trifft
			if (ray.Intersects(model.MeshBoundingSphere[0]) != null)
			{
				// Durch die Indizes der Model-Dreiecke iterieren
				for (int i = 0; i < model.IndexData.Length; i += 3)
				{
					// Holen der drei Vertices
					Vector3 p1 = model.VertexData[model.IndexData[i]].Position;
					Vector3 p2 = model.VertexData[model.IndexData[i + 1]].Position;
					Vector3 p3 = model.VertexData[model.IndexData[i + 2]].Position;

					// Die vom Dreieck aufgespannte Plane erzeugen
					Plane plane = new Plane(p1, p2, p3);
					// Distanz bis zum Auftreffen des Strahles auf der Plane ermitteln
					// NULL wenn kein Schnittpunkt gefunden wird
					float? distance = ray.Intersects(plane);

					// Wenn ein Schnittpunkt auf der Plane gefunden wurde
					if (distance != null)
					{
						// Den Kollisionspunkt auf der Plane ermitteln
						Vector3 intersectionPoint = ray.Position + ray.Direction * (float)distance;

						// Prüfen ob der Kollisionspunkt im Model-Dreieck liegt
						if (PointIsInsideTriangle(intersectionPoint, p1, p2, p3))
						{
							// Wurde noch kein Kollisionspunkt gefunden oder ist der Kollisionspunkt näher als der bisherige,
							// dann wird dieser gesetzt
							if (rayCollisionPoint == Vector3.Zero ||
								(intersectionPoint - ray.Position).LengthSquared() < (rayCollisionPoint - ray.Position).LengthSquared())
							{
								rayCollisionPoint = intersectionPoint;
								rayCollisionDistance = distance;
							}
						}
					}
				}
			}
		}

		// Prüfen ob der Punkt innerhalb eines Dreiecks liegt
		private bool PointIsInsideTriangle(Vector3 point, Vector3 trianglePoint1, Vector3 trianglePoint2, Vector3 trianglePoint3)
		{
			// Liegt der Punkte innerhalb aller Dreieicksseiten, ist er im Dreieck
			return SameSideTest(point, trianglePoint2, trianglePoint3, trianglePoint1) &&
				SameSideTest(point, trianglePoint1, trianglePoint3, trianglePoint2) &&
				SameSideTest(point, trianglePoint1, trianglePoint2, trianglePoint3);
		}

		// Vergleicht den Punkt mit einer Dreiecksseite, ob dieser auf der Innenseite des Dreiecks liegt
		private bool SameSideTest(Vector3 point, Vector3 trianglePoint1, Vector3 trianglePoint2, Vector3 trianglePoint3)
		{
			// Der Vektor einer Dreiecksseite
			Vector3 triangleVector = trianglePoint2 - trianglePoint1;
			// Normalvektor auf die Dreiecksseite und den Vektor vom Dreieckspunkt zum Vergleichspunkt
			Vector3 normalVector1 = Vector3.Cross(triangleVector, point - trianglePoint1);
			// Normalvektor des Dreiecks
			Vector3 normalVector2 = Vector3.Cross(triangleVector, trianglePoint3 - trianglePoint1);
			// Ist das Dot-Produkt größer oder gleich 0, zeigen die Normalvektoren in die selbe Richtung und der Punkt liegt innerhalb
			return Vector3.Dot(normalVector1, normalVector2) >= 0;
		}

		// Kollisionspunkt zeichnen
		private void DrawHitPoint()
		{
			// BasicEffect verwenden
			ICamera camera = (ICamera)Services.GetService(typeof(ICamera));
			basicEffect.VertexColorEnabled = true;
			basicEffect.Projection = camera.ProjectionMatrix;
			basicEffect.View = camera.ViewMatrix;

			// Durch die sechs Würfelseiten für den Kollisonspunkt iterieren und zeichnen
			for (int i = 0; i < hitPointMatrices.Length; ++i)
			{
				basicEffect.World = hitPointMatrices[i] * Matrix.CreateTranslation(rayCollisionPoint);

				foreach (EffectPass pass in basicEffect.CurrentTechnique.Passes)
				{
					pass.Apply();

					GraphicsDevice.DrawUserIndexedPrimitives<VertexPositionColor>(
						PrimitiveType.TriangleStrip,
						hitPointVertices,
						0,
						hitPointVertices.Length,
						hitPointIndices,
						0,
						hitPointIndices.Length - 2,
						VertexPositionColor.VertexDeclaration);
				}
			}
		}

		// Strahl zeichnen
		private void DrawRay()
		{
			// BasicEffect verwenden
			ICamera camera = (ICamera)Services.GetService(typeof(ICamera));
			basicEffect.VertexColorEnabled = true;
			basicEffect.World = Matrix.Identity;
			basicEffect.Projection = camera.ProjectionMatrix;
			basicEffect.View = camera.ViewMatrix;

			// Strahl zeichnen
			foreach (EffectPass pass in basicEffect.CurrentTechnique.Passes)
			{
				pass.Apply();
				GraphicsDevice.DrawUserIndexedPrimitives<VertexPositionColor>(
					PrimitiveType.LineList,
					rayPointList,
					0,
					2,
					rayIndexList,
					0,
					1);
			}
		}

		#endregion


		/// <summary>
		/// This is called when the game should draw itself.
		/// </summary>
		/// <param name="gameTime">Provides a snapshot of timing values.</param>
		protected override void Draw(GameTime gameTime)
		{
			if (Shadows && Light.Power)
			{
				// Render Target für Shadow Map setzen
				GraphicsDevice.SetRenderTarget(shadowMapRenderTarget);
				GraphicsDevice.Clear(ClearOptions.Target | ClearOptions.DepthBuffer, Color.Black, 1.0f, 0);

				// Models in Shadow Map zeichnen
				DrawShadowMap(shadowMapEffect, spaceShip, Light);
				DrawShadowMap(shadowMapEffect, room, Light);
				DrawShadowMap(shadowMapEffect, chair, Light);

				// Render Target auf die PCFMap setzen
				GraphicsDevice.SetRenderTarget(pcfMapRenderTarget);
				GraphicsDevice.Clear(ClearOptions.Target | ClearOptions.DepthBuffer, Color.Black, 1.0f, 0);

				// Models in Shadow Map zeichnen
				DrawPCFMap(shadowMapEffect, spaceShip, Light, shadowMapRenderTarget);
				DrawPCFMap(shadowMapEffect, room, Light, shadowMapRenderTarget);
				DrawPCFMap(shadowMapEffect, chair, Light, shadowMapRenderTarget);

				Quad quad = new Quad(Vector3.Zero, Vector3.Backward, Vector3.Up, 2, 2);

				// Render Target auf die BlurMapHorizontal setzen
				GraphicsDevice.SetRenderTarget(blurMapHorizontalRenderTarget);
				GraphicsDevice.Clear(ClearOptions.Target | ClearOptions.DepthBuffer, Color.Black, 1.0f, 0);

				DrawBlurMap(shadowMapEffect, quad, pcfMapRenderTarget, sampleWeightsHorizontal, sampleOffsetsHorizontal);

				// Render Target auf die BlurMapHorizontal setzen
				GraphicsDevice.SetRenderTarget(blurMapVerticalRenderTarget);
				GraphicsDevice.Clear(ClearOptions.Target | ClearOptions.DepthBuffer, Color.Black, 1.0f, 0);

				DrawBlurMap(shadowMapEffect, quad, blurMapHorizontalRenderTarget, sampleWeightsVertical, sampleOffsetsVertical);

				// Umschalten auf den Bildschirm als RenderTarget
				GraphicsDevice.SetRenderTarget(null);

				// Shadow Map auf Festplatte speichern
				if (saveMaps)
				{
					saveMaps = false;

					FileStream fs = new FileStream("ShadowMap.png", FileMode.Create);
					shadowMapRenderTarget.SaveAsPng(fs, shadowMapRenderTarget.Width, shadowMapRenderTarget.Height);
					fs.Close();

					fs = new FileStream("PCFMap.png", FileMode.Create);
					pcfMapRenderTarget.SaveAsPng(fs, pcfMapRenderTarget.Width, pcfMapRenderTarget.Height);
					fs.Close();

					fs = new FileStream("BlurMapHorizontal.png", FileMode.Create);
					blurMapHorizontalRenderTarget.SaveAsPng(fs, blurMapHorizontalRenderTarget.Width, blurMapHorizontalRenderTarget.Height);
					fs.Close();

					fs = new FileStream("BlurMapVertical.png", FileMode.Create);
					blurMapVerticalRenderTarget.SaveAsPng(fs, blurMapVerticalRenderTarget.Width, blurMapVerticalRenderTarget.Height);
					fs.Close();
				}
			}

			GraphicsDevice.Clear(ClearOptions.Target | ClearOptions.DepthBuffer, Color.CornflowerBlue, 1.0f, 0);

			// Wenn vorhanden Strahl zeichnen
			if (rayPointList[0] != null && rayPointList[1] != null)
			{
				// Strahl zeichnen
				DrawRay();

				// Kollisionspunkt zeichnen
				if (rayCollisionPoint != Vector3.Zero)
					DrawHitPoint();
			}

			base.Draw(gameTime);
		}


		#region ShadowMapMethods

		// Gewichtung berechnen
		private float GetGaussionDistribution(float x, float y, float rho)
		{
			float g = 1.0f / (float)Math.Sqrt(2.0f * MathHelper.Pi * rho * rho);
			return g * (float)Math.Exp(-(x * x + y * y) / (2 * rho * rho));
		}

		// Setzen der Offsets für das Bluring
		private void GetGaussionOffsets(bool horizontal, Vector2 texelSize, out float[] sampleWeights, out Vector2[] sampleOffsets)
		{
			sampleWeights = new float[15];
			sampleOffsets = new Vector2[15];
			sampleWeights[0] = 1.0f * GetGaussionDistribution(0, 0, 2.0f);
			sampleOffsets[0] = Vector2.Zero;

			for (int i = 1; i < 15; i += 2)
			{
				Vector2 sampleOffset;
				float sampleWeight1, sampleWeight2;

				if (horizontal)
				{
					sampleOffset = new Vector2(i * texelSize.X, 0);
					sampleWeight1 = 2.0f * GetGaussionDistribution(i, 0, 3.0f);
					sampleWeight2 = 2.0f * GetGaussionDistribution(i + 1, 0, 3.0f);
				}
				else
				{
					sampleOffset = new Vector2(0, i * texelSize.Y);
					sampleWeight1 = 2.0f * GetGaussionDistribution(0, i, 3.0f);
					sampleWeight2 = 2.0f * GetGaussionDistribution(0, i + 1, 3.0f);
				}

				sampleOffsets[i] = sampleOffset;
				sampleOffsets[i + 1] = -sampleOffset;
				sampleWeights[i] = sampleWeight1;
				sampleWeights[i + 1] = sampleWeight2;
			}
		}

		// Shadow Map zeichnen
		private void DrawShadowMap(Effect effect, ShaderModel shaderModel, Light light)
		{
			// Lichtmatrizen übergeben
			effect.Parameters["View"].SetValue(light.ViewMatrix);
			effect.Parameters["Projection"].SetValue(light.ProjectionMatrix);

			foreach (ModelMesh mesh in shaderModel.DrawingModel.Meshes)
			{
				foreach (ModelMeshPart part in mesh.MeshParts)
				{
					part.Effect = effect;
					part.Effect.CurrentTechnique = part.Effect.Techniques["ShadowMap"];
					effect.Parameters["World"].SetValue(mesh.ParentBone.Transform * shaderModel.WorldMatrix);
				}
				mesh.Draw();
			}
		}

		// PCF Map zeichnen
		private void DrawPCFMap(Effect effect, ShaderModel shaderModel, Light light, RenderTarget2D shadowMapTexture)
		{
			ICamera camera = (ICamera)Services.GetService(typeof(ICamera));

			// Kameramatrizen übergeben
			effect.Parameters["View"].SetValue(camera.ViewMatrix);
			effect.Parameters["Projection"].SetValue(camera.ProjectionMatrix);

			// Lichtview und -projection übergeben
			effect.Parameters["LightView"].SetValue(light.ViewMatrix);
			effect.Parameters["LightProjection"].SetValue(light.ProjectionMatrix);

			// Shadow Map übergeben
			effect.Parameters["ShadowMapLight0Texture"].SetValue(shadowMapTexture);
			effect.Parameters["ShadowMapSize"].SetValue(shadowMapSize);

			foreach (ModelMesh mesh in shaderModel.DrawingModel.Meshes)
			{
				foreach (ModelMeshPart part in mesh.MeshParts)
				{
					part.Effect = effect;
					part.Effect.CurrentTechnique = part.Effect.Techniques["PCFMap"];

					effect.Parameters["World"].SetValue(mesh.ParentBone.Transform * shaderModel.WorldMatrix);
				}
				mesh.Draw();
			}
		}

		// Blur Map (horizontal oder vertikal) zeichnen
		private void DrawBlurMap(Effect effect, Quad quad, RenderTarget2D textureToBlur, float[] sampleWeights, Vector2[] sampleOffsets)
		{
			effect.CurrentTechnique = effect.Techniques["BlurMap"];

			// Ausgangstextur übergeben
			effect.Parameters["ShadowMapLight0Texture"].SetValue(textureToBlur);

			// Gewichtungen und Offsets für das Bluring übergeben
			effect.Parameters["SampleWeights"].SetValue(sampleWeights);
			effect.Parameters["SampleOffsets"].SetValue(sampleOffsets);

			foreach (EffectPass pass in effect.CurrentTechnique.Passes)
			{
				pass.Apply();

				GraphicsDevice.DrawUserIndexedPrimitives
					 <VertexPositionNormalTexture>(
					 PrimitiveType.TriangleList,
					 quad.Vertices, 0, 4,
					 quad.Indices, 0, 2);
			}
		}

		#endregion
	}
}
