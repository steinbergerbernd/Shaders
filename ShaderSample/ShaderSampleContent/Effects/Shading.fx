String Name = "Shading";

// Transformationsmatrizen
float4x4 World;
float4x4 View;
float4x4 Projection;

// Lichtmatrizen
float4x4 LightView;
float4x4 LightProjection;

// Korrekturwert für die Ungenauigkeit bei den Schatten
float correctionValue = 0.001;

// Anzahl der Berechnungsschritte für das Displacement Mapping
float displacementSteps = 10;
// Skalierung der Höhe
float heightRatio = 0.01f;
// Skalierung der Textur
float textureRatio = 4.0f;

bool UseNormalMap = false;
bool UseDisplacementMap = false;

// Sichtvektor von der Kamera aus
float3 CameraPosition;

// Lichtposition
float3 LightPosition;
float3 LightLookAt;
bool SpotLight = false;
float CutOffAngle;
float AttenuationDistance;

// Self Shading an- und ausgeschalten
bool SelfShading = true;
// Schatten
bool Shadows = false;

// Farbe und Leuchtkraft des ambienten Lichts
float4 AmbientLightColor = float4(1, 1, 1, 1);
float AmbientLightPower = 0.1;

// Farbe und Leuchtkraft des diffusen Lichts
float4 DiffuseLightColor = float4(1, 1, 1, 1);
float DiffuseLightPower = 0.5;

// Farbe und Leuchtkraft des spekulären Lichts
float4 SpecularLightColor = float4(1, 1, 1, 1);
float SpecularLightPower = 0.8;

// Glanzwert für die Ausprägung des Glanzpunktes
float Shininess = 50;

// Textur für die Normal Map
Texture NormalMapTexture;

// Sampler für die Textur der Normal Map
sampler2D NormalMapTextureSampler = sampler_state
{
	Texture = (NormalMapTexture);
	MagFilter = Linear;
	MinFilter = Linear;
	MipFilter = Linear;
	AddressU = Wrap;
	AddressV = Wrap;
};

// Textur für die Distance Map
Texture DisplacementMapTexture;

// Sampler für die Textur der Distance Map
sampler2D DisplacementMapTextureSampler = sampler_state
{
	Texture = (DisplacementMapTexture);
	MagFilter = Linear;
	MinFilter = Linear;
	MipFilter = Linear;
	AddressU = Wrap;
	AddressV = Wrap;
};

// Textur für das Model
Texture ModelTexture;

// Sampler für die Textur des Models
sampler2D ModelTextureSampler = sampler_state
{
	Texture = (ModelTexture);
	MagFilter = Linear;
	MinFilter = Linear;
	MipFilter = Linear;
	AddressU = Wrap;
	AddressV = Wrap;
};

// Textur für die ShadowMap von Licht 0
Texture ShadowMapLight0Texture;

// Sampler für die Textur der ShadowMap von Licht 0
sampler2D ShadowMapLight0Sampler = sampler_state
{
	Texture = (ShadowMapLight0Texture);
	MagFilter = Point;
	MinFilter = Point;
	MipFilter = Point;
	AddressU = Clamp;
	AddressV = Clamp;
};

/*******************************************SHARED FUNCTIONS******************************************/

// Berechnung des diffusen Lichtes
float4 CalculateDiffuseLight(float3 lightDirection, float3 normal)
{
	// zurückgeworfene Intensität des diffusen Lichtes
	float diffuseLightAttenuation = saturate(dot(lightDirection, normal));
	
	// Berechnung der diffusen Komponente
	return saturate(DiffuseLightColor * DiffuseLightPower * diffuseLightAttenuation);
}

// Berechnung des spekulären Lichtes
float4 CalculateSpecularLight(float3 lightDirection, float3 normal, float3 view)
{
	// Berechnung des Reflexionsvektors
	float3 reflectionVector = normalize(2 * dot(lightDirection, normal) * normal - lightDirection);
	
	// zurückgeworfene Intensität des spekulären Lichtes
	float specularLightAttenuation = saturate(dot(reflectionVector, view));

	// Berechnung der spekulären Komponente
	return SpecularLightPower * SpecularLightColor * pow(specularLightAttenuation, Shininess);
}

// Linear die Schrittweiten überprüfen um eine Annäherung zu erreichen
float3 LinearSearch(sampler2D displacementMap, float3 offset, float3 samplingCoord) 
{ 
	float4 texel;
	for(int i=0;i<10;i++)
	{
		texel = tex2D(displacementMap, samplingCoord.xy);
		if(samplingCoord.z>texel.r)
			samplingCoord += offset;
	}
	return samplingCoord;
}

// Binäre Suche - Halbierung der Schrittweite um genaueren Wert zu erhalten
float3 BinarySearch(sampler2D displacementMap, float3 offset, float3 samplingCoord)  
{ 
	float4 texel;
	for(int i=0;i<10;i++)
	{
		offset *= 0.5f;
		texel = tex2D(displacementMap, samplingCoord.xy);
		if(samplingCoord.z>texel.r)
			samplingCoord -= offset;
		else
			samplingCoord += offset;
	}
	return samplingCoord;
}

/***********************************************PHONG***********************************************/

// Ein- und Ausgabestrukturen für Phong
struct VertexShaderInputPhong
{
	float4 Position : POSITION0;
	float3 Normal : NORMAL0;
	float3 Tangent : TANGENT0;
	float3 Binormal : BINORMAL0;
	float2 TextureCoordinate : TEXCOORD0;
};

struct VertexShaderOutputPhong
{
	float4 Position : POSITION0;
	float3 Normal : TEXCOORD0;
	float2 TextureCoordinate : TEXCOORD1;
	float3 View : TEXCOORD2;
	float4 WorldPosition : TEXCOORD3;
	float3 Tangent : TEXCOORD4;
	float3 Binormal : TEXCOORD5;
	float4 PositionLightView : TEXCOORD6;
	float4 ScreenPosition : TEXCOORD7;
};

// Vertex Shader für Phong
VertexShaderOutputPhong VertexShaderFunctionPhong(VertexShaderInputPhong input)
{
	VertexShaderOutputPhong output;

	// Transformation der Position
	float4 worldPosition = mul(input.Position, World);
	float4 viewPosition = mul(worldPosition, View);
	output.Position = mul(viewPosition, Projection);
	output.ScreenPosition = output.Position;
	output.WorldPosition = worldPosition;
	
	// Verschiebung der Normalen
	output.Normal = normalize(mul(input.Normal, World));
	output.Tangent = normalize(mul(input.Tangent, World));
	output.Binormal = normalize(mul(input.Binormal, World));

	// Position aus Sicht des Lichtes
	output.PositionLightView = mul(mul(worldPosition, LightView), LightProjection);

	// Weitergabe der Texturkoordinaten
	output.TextureCoordinate = input.TextureCoordinate;

	// View Vector berechnen
	output.View = CameraPosition - worldPosition;

	return output;
}

// Pixel Shader für Phong
float4 PixelShaderFunctionPhong(VertexShaderOutputPhong input) : COLOR0
{
	float3 normal;
	// Vektor zur Kamera
	float3 viewDirection = normalize(input.View);
	// Vektor zum Licht
	float3 lightVec = LightPosition - input.WorldPosition;
	// normalisierter Richtungsvektor zum Licht
	float3 lightDirection = normalize(lightVec);
	float4 diffuseLight = float4(0,0,0,0);
	float4 specularLight = float4(0,0,0,0);
	float shadow = 1.0f;

	// Entweder kein Spot Light oder wenn doch, dann muss die Szene innerhalb des Öffnungswinkels liegen
	if(!SpotLight || dot(normalize(-lightDirection), normalize(LightLookAt - LightPosition)) > cos(CutOffAngle))
	{
		// Wenn Normal Mapping aktiviert ist
		if(UseNormalMap)
		{
			float3x3 tangentToWorldSpace;
			tangentToWorldSpace[0] = normalize(input.Tangent);
			tangentToWorldSpace[1] = normalize(input.Binormal);
			tangentToWorldSpace[2] = normalize(input.Normal);

			// Wenn Displacement Mapping aktiviert ist
			if(UseDisplacementMap)
			{
				float3x3 worldToTangentSpace = transpose(tangentToWorldSpace);
				// Kamerasicht in den Tangen Space transponieren
				float3 viewTS = mul(input.View, worldToTangentSpace);

				// z-Wert zwischen 0 und 1
				float3 scaledView = viewTS / viewTS.z;
				// Höhe skalieren
				scaledView.xy *= heightRatio;

				// Texturkoordinate berechnen
				float3 sampleCoord = scaledView + float3(input.TextureCoordinate.xy * textureRatio,0);
				// Schrittweiten berechnen
				float3 offset = -scaledView / displacementSteps;

				// Lineare Suche in der Textur für Annäherung
				sampleCoord = LinearSearch(DisplacementMapTextureSampler, offset, sampleCoord);
				// Binäre Suche in der Textur für genauere Bestimmung des Texturenwertes
				sampleCoord = BinarySearch(DisplacementMapTextureSampler, -offset, sampleCoord);

				// Normale auslesen
				normal = normalize((tex2D(NormalMapTextureSampler, sampleCoord.xy)) * 2 - 1);
				normal = normalize(mul(normal, tangentToWorldSpace));
			}
			// Ansonsten gewöhnliches Normal Mapping
			else
			{
				// Normale aus der Normal Map auslesen und den Wert in das Intervall [-1,1] umrechnen
				normal = normalize((tex2D(NormalMapTextureSampler, input.TextureCoordinate * textureRatio)) * 2 - 1);
				// Normale aus dem Tangent Space in den World Space umrechnen
				normal = normalize(mul(normal, tangentToWorldSpace));
			}
		}
		else
			normal = normalize(input.Normal);
		
		// Wenn Schatten aktiviert sind
		if(Shadows)
		{
			// Texturkoordinaten aus dem Screen Space
			float2 texCoordinates = (input.ScreenPosition.xy / input.ScreenPosition.w) * 0.5f + 0.5f;
			// y-Koordinate umdrehen
			texCoordinates.y = 1.0f - texCoordinates.y;
			// Schattenwert auslesen
			shadow = tex2D(ShadowMapLight0Sampler, texCoordinates);
		}
		
		// Berechnung der diffusen Komponente
		diffuseLight = CalculateDiffuseLight(lightDirection, normal);

		// Berechnung der spekulären Komponente
		specularLight = CalculateSpecularLight(lightDirection, normal, viewDirection);
	}

	// Self Shadowing berechnen
	float selfShadow = 1.0f;
	if(SelfShading)
		selfShadow = saturate(4 * diffuseLight);

	// Lichtabschwächung berechnen
	float attenuation = 1.0f - length(lightVec) / AttenuationDistance;

	// Farbwert berechnen
	float4 color = saturate((AmbientLightColor * AmbientLightPower + shadow * attenuation * selfShadow * (diffuseLight + specularLight)) * tex2D(ModelTextureSampler, input.TextureCoordinate));

	// Gesamtfarbe für den Pixel ausgeben
	return color;
}


/***********************************************TECHNIKEN***********************************************/

// Technik Phong
technique Phong
{
	pass Pass0
	{
		VertexShader = compile vs_3_0 VertexShaderFunctionPhong();
		PixelShader = compile ps_3_0 PixelShaderFunctionPhong();
	}
}