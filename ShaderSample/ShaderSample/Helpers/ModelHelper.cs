using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;

namespace Playroom.Helpers
{
	public static class ModelHelper
	{
		// Texturen der Models extrahieren
		public static SortedList<string, Texture2D> GetModelTextures(Model model)
		{
			var textureList = new SortedList<string, Texture2D>();

			foreach (ModelMesh mesh in model.Meshes)
			{
				foreach (BasicEffect effect in mesh.Effects)
				{
					string textureName = mesh.Name;

					// Ist der Name der Textur in der Liste bereits vorhanden, wird eine Guid erzeugt
					if (textureName == "" || textureList.ContainsKey(textureName))
						textureName = Guid.NewGuid().ToString();
					// Textur in Liste einfügen
					textureList.Add(textureName, effect.Texture);
				}
			}

			return textureList;
		}
	}
}
