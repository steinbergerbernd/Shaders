using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Playroom.Cameras;
using Playroom.Helpers;

namespace ShaderSample.Components.Models
{
	/// <summary>
	/// This is a game component that implements IUpdateable.
	/// </summary>
	public class ShaderModel : Microsoft.Xna.Framework.DrawableGameComponent
	{
		#region Fields

		// Name des Models
		public string Name { get; set; }

		// Das Model-Objekt
		public Model DrawingModel { get; set; }

		public Matrix WorldMatrix { get; set; }

		// Liste mit den Texturen
		public SortedList<string, Texture2D> TextureList { get; set; }

		// Standardtextur
		public Texture2D DefaultTexture { get; set; }
		// optionale Normal Map
		public Texture2D NormalMapTexture { get; set; }
		// optionale Distance Map
		public Texture2D DisplacementMapTexture { get; set; }

		// Struktur für das auslesen des VertexBuffer
		public struct PositionNormalTextureTangentBinormalStruct
		{
			public Vector3 Position;
			public Vector3 Normal;
			public Vector2 TextureCoordinate;
			public Vector3 Tangent;
			public Vector3 Binormal;
		}

		// Daten des Vertex- und IndexBuffer
		public PositionNormalTextureTangentBinormalStruct[] VertexData { get; set; }
		public short[] IndexData { get; set; }
		// Bounding Sphere
		public BoundingSphere[] MeshBoundingSphere { get; set; }

		private ICamera camera;

		private const float defaultAmbientLightPower = 0.1f;
		private const float defaultDiffuseLightPower = 0.6f;
		private const float defaultSpecularLightPower = 0.8f;

		#endregion

		#region Setup

		public ShaderModel(Game game)
			: base(game)
		{
			WorldMatrix = Matrix.Identity;
			NormalMapTexture = null;
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
			if (DrawingModel != null)
			{
				// Texturen laden
				TextureList = ModelHelper.GetModelTextures(DrawingModel);

				LoadVertexAndIndexBuffer();
			}

			base.LoadContent();
		}

		// Auslesen des Vertex- und IndexBuffers
		protected void LoadVertexAndIndexBuffer()
		{
			int boundingSphereIndex = 0;
			MeshBoundingSphere = new BoundingSphere[DrawingModel.Meshes.Count];

			// Modelmeshes durchiterieren
			foreach (ModelMesh mesh in DrawingModel.Meshes)
			{
				// Bounding Sphere setzen und an Modelposition transformieren
				MeshBoundingSphere[boundingSphereIndex++] = mesh.BoundingSphere.Transform(WorldMatrix);
				// Meshparts durchiterieren
				foreach (ModelMeshPart meshPart in mesh.MeshParts)
				{
					VertexBuffer vertexBuffer = meshPart.VertexBuffer;
					IndexBuffer indexBuffer = meshPart.IndexBuffer;

					// bisherige Vertices merken
					short oldVertexDataCount = 0;
					if (VertexData != null)
						oldVertexDataCount = (short)VertexData.Length;

					// Die neuen Vertices aus dem MeshPart auslesen
					PositionNormalTextureTangentBinormalStruct[] newVertexData = new PositionNormalTextureTangentBinormalStruct[vertexBuffer.VertexCount];
					vertexBuffer.GetData<PositionNormalTextureTangentBinormalStruct>(newVertexData);

					// Vertices auf Modelposition transformieren
					for (int i = 0; i < newVertexData.Length; ++i)
						newVertexData[i].Position = Vector3.Transform(newVertexData[i].Position, WorldMatrix);

					// Alte und neue Vertices zusammenfügen
					if (VertexData != null)
						VertexData = VertexData.Concat(newVertexData).ToArray();
					else
						VertexData = newVertexData;

					// Indices auslesen
					short[] newIndexData = new short[indexBuffer.IndexCount];
					indexBuffer.GetData<short>(newIndexData);

					// Indices auf richtige Stellen verschieben, wenn bereits Vertices vorhanden sind
					if (oldVertexDataCount > 0)
					{
						for (int i = 0; i < newIndexData.Length; ++i)
							newIndexData[i] += oldVertexDataCount;
					}

					// Die neuen Indices hinzufügen
					if (IndexData != null)
						IndexData = IndexData.Concat(newIndexData).ToArray();
					else
						IndexData = newIndexData;
				}
			}
		}

		#endregion

		/// <summary>
		/// Allows the game component to update itself.
		/// </summary>
		/// <param name="gameTime">Provides a snapshot of timing values.</param>
		public override void Update(GameTime gameTime)
		{
			camera = (ICamera)Game.Services.GetService(typeof(ICamera));

			base.Update(gameTime);
		}

		public override void Draw(GameTime gameTime)
		{
			Effect drawingEffect = (Effect)Game.Services.GetService(typeof(Effect));

			if (drawingEffect.Parameters["Name"].GetValueString() == "Shading")
				DrawShading(drawingEffect);

			base.Draw(gameTime);
		}

		// Zeichnen des Models
		private void DrawShading(Effect effect)
		{
			ShaderSample shaderSample = (ShaderSample)Game;

			RasterizerState rs = new RasterizerState();

			// WireFrame oder Solid
			if (shaderSample.WireFrameMode)
				rs.FillMode = FillMode.WireFrame;
			else
				rs.FillMode = FillMode.Solid;

			GraphicsDevice.RasterizerState = rs;

			EffectParameterCollection parameters = effect.Parameters;

			// Setzen der Lichtparameter
			parameters["LightPosition"].SetValue(shaderSample.Light.Position);
			parameters["LightLookAt"].SetValue(shaderSample.Light.LookAt);
			parameters["SpotLight"].SetValue(shaderSample.Light.SpotLight);
			parameters["CutOffAngle"].SetValue(shaderSample.Light.CutOffAngle);
			parameters["SelfShading"].SetValue(shaderSample.Light.SelfShading);
			parameters["AttenuationDistance"].SetValue(shaderSample.Light.AttenuationDistance);
			parameters["SpecularLightPower"].SetValue(shaderSample.Light.SpecularLightPower);
			parameters["DiffuseLightPower"].SetValue(shaderSample.Light.DiffuseLightPower);

			// Bei Lampen ambientes Licht voll aufdrehen, um leuchtende Lampe zu zeichnen
			if (Name == "Lamp" && ((Lamp)this).On)
				parameters["AmbientLightPower"].SetValue(1.0f);
			else
				parameters["AmbientLightPower"].SetValue(defaultAmbientLightPower);

			// Kameraparameter setzen
			parameters["View"].SetValue(camera.ViewMatrix);
			parameters["Projection"].SetValue(camera.ProjectionMatrix);
			parameters["CameraPosition"].SetValue(camera.Position);

			// Parameter für Normal Mapping setzen
			if (NormalMapTexture != null && shaderSample.UseNormalMap)
			{
				parameters["UseNormalMap"].SetValue(true);
				parameters["NormalMapTexture"].SetValue(NormalMapTexture);
			}
			else
				parameters["UseNormalMap"].SetValue(false);

			// Schatten ein- bzw. ausschalten
			parameters["Shadows"].SetValue(shaderSample.Shadows);
			if (shaderSample.Shadows)
			{
				// Lichtview und -projection übergeben
				parameters["LightView"].SetValue(shaderSample.Light.ViewMatrix);
				parameters["LightProjection"].SetValue(shaderSample.Light.ProjectionMatrix);

				// Die notwendigen Shadow Map Texturen übergeben
				if (shaderSample.Light.Power)
					parameters["ShadowMapLight0Texture"].SetValue(shaderSample.ShadowMap);
			}

			// Zeichnen des Models
			foreach (ModelMesh mesh in DrawingModel.Meshes)
			{
				foreach (ModelMeshPart part in mesh.MeshParts)
				{
					part.Effect = effect;

					part.Effect.CurrentTechnique = part.Effect.Techniques["Phong"];

					// Parameter für Displacement Mapping setzen (für das Room Model werden nur die Wände verwendet)
					if (DisplacementMapTexture != null && shaderSample.UseDisplacementMap && (Name != "Room" || Name == "Room" && mesh.Name == "Walls"))
					{
						effect.Parameters["UseDisplacementMap"].SetValue(true);
						effect.Parameters["DisplacementMapTexture"].SetValue(DisplacementMapTexture);
					}
					else
					{
						effect.Parameters["UseDisplacementMap"].SetValue(false);
						effect.Parameters["DisplacementMapTexture"].SetValue((Texture2D)null);
					}

					// Ist keine Textur vorhanden, wird die Standardtextur verwendet
					Texture2D texture = mesh.Name == "" ? DefaultTexture : TextureList[mesh.Name];

					// Modeltextur übergeben
					effect.Parameters["ModelTexture"].SetValue(texture);

					// World Matrix übergeben
					effect.Parameters["World"].SetValue(mesh.ParentBone.Transform * WorldMatrix);
				}
				mesh.Draw();
			}
		}
	}
}
