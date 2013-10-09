String Name = "ShadowMap";

// Matrizen
float4x4 World;
float4x4 View;
float4x4 Projection;

// Lichtmatrizen
float4x4 LightView;
float4x4 LightProjection;

// Weights und Offsets für das Bluring
float SampleWeights[15];
float2 SampleOffsets[15];

// Korrekturwert für die Ungenauigkeit bei den Schatten
float correctionValue = 0.001;

// Größe der Shadow Map
float ShadowMapSize = 2048.0f;

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

/****************************************************************/
/************************ShadowMap Effect************************/
/****************************************************************/

// Ein- und Ausgabestrukturen für die ShadowMap
struct VertexShaderShadowMapInput
{
	float4 Position : POSITION0;
};

struct VertexShaderShadowMapOutput
{
	float4 Position	: POSITION0;
	float4 OriginalPosition : TEXCOORD0;
};

VertexShaderShadowMapOutput VertexShaderShadowMap(VertexShaderShadowMapInput input)
{
	VertexShaderShadowMapOutput output;
	
	// Position aus Sicht des Lichtes
	float4 worldPosition = mul(input.Position, World);
	float4 viewPosition = mul(worldPosition, View);
	output.Position = mul(viewPosition, Projection);
	
	output.OriginalPosition = output.Position;
	
	return output;
}

float4 PixelShaderShadowMap(VertexShaderShadowMapOutput input) : COLOR0
{
	// Tiefenwert aus Sicht des Lichtes
	float shadowValue = input.OriginalPosition.z/input.OriginalPosition.w;
	return float4(shadowValue, shadowValue, shadowValue, 1.0f);
}


/***************************************************************/
/*************************PCFMap Effect*************************/
/***************************************************************/

// Prüfung mit Hilfe der Shadow Map ob Position im Schatten liegt
bool IsInShadow(float depthValueByLight, float4 positionLightView, float2 textureCoordinate, sampler2D textureSampler)
{
	// Wert einlesen
	float depthValueShadowMap = tex2D(textureSampler, textureCoordinate).r;

	// Prüfen ob im Schatten (nur gerichtetes Licht nach vorne)
	return depthValueShadowMap < depthValueByLight
			&& positionLightView.x >= -1 && positionLightView.x <= 1
			&& positionLightView.y >= -1 && positionLightView.y <= 1
			&& positionLightView.z >= -1 && positionLightView.z <= 1;
}

// Ein- und Ausgabestrukturen für die ShadowMap
struct VertexShaderPCFMapInput
{
	float4 Position : POSITION0;
};

struct VertexShaderPCFMapOutput
{
	float4 Position	: POSITION0;
	float4 PositionLightView : TEXCOORD0;
};

VertexShaderPCFMapOutput VertexShaderPCFMap(VertexShaderPCFMapInput input)
{
	VertexShaderPCFMapOutput output;
	
	// Position aus Kamerasicht
	float4 worldPosition = mul(input.Position, World);
	float4 viewPosition = mul(worldPosition, View);
	output.Position = mul(viewPosition, Projection);
	
	// Position aus Sicht des Lichtes
	output.PositionLightView = mul(mul(worldPosition, LightView), LightProjection);
	
	return output;
}

float4 PixelShaderPCFMap(VertexShaderPCFMapOutput input) : COLOR0
{
	float4 positionLightView = input.PositionLightView;
	positionLightView = positionLightView / positionLightView.w;

	// Texturkoordinaten aufgrund der Lichtposition (in das Intervall [0,1] umrechnen)
	float2 shadowMapTextureCoordinate =
		positionLightView.xy * 0.5f + float2(0.5f, 0.5f);
	// in einer Textur steigen die y-Werte von oben nach unten
	shadowMapTextureCoordinate.y = 1.0f - shadowMapTextureCoordinate.y;
	// Tiefe aus Sicht des Lichtes (mit leichter Anpassung)
	float depthValueByLight = positionLightView.z - correctionValue;

	float texelSize = 1.0f / ShadowMapSize;
	float shadow = 0.0f;

	// 3x3 PCF Map für die umliegenden Werte
	if(!IsInShadow(depthValueByLight, positionLightView, shadowMapTextureCoordinate + float2(-texelSize, -texelSize), ShadowMapLight0Sampler))
		shadow += 1.0f;
	if(!IsInShadow(depthValueByLight, positionLightView, shadowMapTextureCoordinate + float2(0, -texelSize), ShadowMapLight0Sampler))
		shadow += 1.0f;
	if(!IsInShadow(depthValueByLight, positionLightView, shadowMapTextureCoordinate + float2(texelSize, -texelSize), ShadowMapLight0Sampler))
		shadow += 1.0f;
	if(!IsInShadow(depthValueByLight, positionLightView, shadowMapTextureCoordinate + float2(-texelSize, 0), ShadowMapLight0Sampler))
		shadow += 1.0f;
	if(!IsInShadow(depthValueByLight, positionLightView, shadowMapTextureCoordinate + float2(0, 0), ShadowMapLight0Sampler))
		shadow += 1.0f;
	if(!IsInShadow(depthValueByLight, positionLightView, shadowMapTextureCoordinate + float2(texelSize, 0), ShadowMapLight0Sampler))
		shadow += 1.0f;
	if(!IsInShadow(depthValueByLight, positionLightView, shadowMapTextureCoordinate + float2(-texelSize, texelSize), ShadowMapLight0Sampler))
		shadow += 1.0f;
	if(!IsInShadow(depthValueByLight, positionLightView, shadowMapTextureCoordinate + float2(0, texelSize), ShadowMapLight0Sampler))
		shadow += 1.0f;
	if(!IsInShadow(depthValueByLight, positionLightView, shadowMapTextureCoordinate + float2(texelSize, texelSize), ShadowMapLight0Sampler))
		shadow += 1.0f;

	// Das Mittel berechnen
	shadow = shadow / 9.0f;

	return float4(shadow, shadow, shadow, 1.0f);
}


/****************************************************************/
/************************BlurMap Effect************************/
/****************************************************************/

// Ein- und Ausgabestrukturen für die ShadowMap
struct VertexShaderBlurMapInput
{
	float4 Position : POSITION0;
	float2 TextureCoordinate : TEXCOORD0;
};

struct VertexShaderBlurMapOutput
{
	float4 Position	: POSITION0;
	float2 TextureCoordinate : TEXCOORD0;
};

VertexShaderBlurMapOutput VertexShaderBlurMap(VertexShaderBlurMapInput input)
{
	VertexShaderBlurMapOutput output;
	
	output.Position = input.Position;
	output.TextureCoordinate = input.TextureCoordinate;
	
	return output;
}

float4 PixelShaderBlurMap(VertexShaderBlurMapOutput input) : COLOR0
{
	float4 accumulatedColor = float4(0, 0, 0, 0);
	// Neue Schattenfarbe mit Hilfe der Bluring Offsets und Weights berechnen
	for(int i = 0; i < 15; i++)
		accumulatedColor += tex2D(ShadowMapLight0Sampler, input.TextureCoordinate + SampleOffsets[i]) * SampleWeights[i];

	return accumulatedColor;
}

/***********************************************TECHNIKEN***********************************************/

// Technik ShadowMap
technique ShadowMap
{
	pass Pass0
	{
		VertexShader = compile vs_1_1 VertexShaderShadowMap();
		PixelShader = compile ps_2_0 PixelShaderShadowMap();
	}
}

// Technik ShadowMap
technique PCFMap
{
	pass Pass0
	{
		VertexShader = compile vs_3_0 VertexShaderPCFMap();
		PixelShader = compile ps_3_0 PixelShaderPCFMap();
	}
}

technique BlurMap
{
	pass Pass0
	{
		VertexShader = compile vs_1_1 VertexShaderBlurMap();
		PixelShader = compile ps_2_0 PixelShaderBlurMap();
	}
}