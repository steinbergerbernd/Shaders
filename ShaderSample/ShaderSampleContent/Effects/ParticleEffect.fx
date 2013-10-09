String Name = "ParticleEffect";

// Transformationsmatrizen
float4x4 View;
float4x4 Projection;

// Die aktuelle Zeit des Partikelsystems
float CurrentTime;


// maximale Lebenszeit eines Partikels
float MaxParticleLifeTime;

// Farben f�r die Abschw�chung der Texturenfarbe
float4 MinColor;
float4 MaxColor;


// Die folgenden drei float2 Variablen beinhalten einen Min- und Maxwert in x und y
// Range f�r die Rotation
float2 RotationSpeed;
// Range f�r die Startgr��e
float2 StartSize;
// Range f�r die Zielgr��e
float2 EndSize;


// Particle texture and sampler.
texture ParticleTexture;

// Sampler f�r die Partikeltextur
sampler2D ParticleTextureSampler = sampler_state
{
    Texture = (ParticleTexture);
    MinFilter = Linear;
    MagFilter = Linear;
    MipFilter = Point;
    AddressU = Clamp;
    AddressV = Clamp;
};


/*******************************************SHARED FUNCTIONS******************************************/

// Berechnung der Partikelposition
float4 ComputeParticlePosition(float3 position, float3 velocity, float normalizedLifeTime)
{
	// L�nge des Geschwindigkeitsvektors
    float startVelocity = length(velocity);
    
    // Berechnung der einzelnen Geschwindigkeitsintegrale (von Startgeschwindigkeit gegen 0)
    float velocityIntegral = startVelocity * normalizedLifeTime * (1 - normalizedLifeTime / 2);
	// Position um Geschwindigkeitsvektor verschieben unter Ber�cksichtigung der maximalen Lebensdauer
    position += normalize(velocity) * velocityIntegral * MaxParticleLifeTime;
    
    // Position transformieren
    return mul(mul(float4(position, 1), View), Projection);
}


// Gr��e des Partikels berechnen
float ComputeParticleSize(float randomValue, float normalizedLifeTime)
{
    // interpolieren einer zuf�lligen Startgr��e
    float startSize = lerp(StartSize.x, StartSize.y, randomValue);
	// interpolieren einer zuf�lligen Zielgr��e
    float endSize = lerp(EndSize.x, EndSize.y, randomValue);
    
    // Die aktuelle Gr��e berechnen und zur�ckgeben
    return lerp(startSize, endSize, normalizedLifeTime);
}


// Die Farbe f�r den Partikel berechnen
float4 ComputeParticleColor(float4 projectedPosition, float randomValue, float normalizedLifeTime)
{
    // Zuf�lligen Farbwert interpolieren
    float4 color = lerp(MinColor, MaxColor, randomValue);
    
	// Berechnungsfunktion f�r den Alphawert aus dem Paritkelsystembeispiel der MSDN
    // Fade the alpha based on the age of the particle. This curve is hard coded
    // to make the particle fade in fairly quickly, then fade out more slowly:
    // plot x*(1-x)*(1-x) for x=0:1 in a graphing program if you want to see what
    // this looks like. The 6.7 scaling factor normalizes the curve so the alpha
    // will reach all the way up to fully solid.
    color.a *= normalizedLifeTime * (1-normalizedLifeTime) * (1-normalizedLifeTime) * 6.7;
   
    return color;
}


// Partikelrotation berechnen
float2x2 ComputeParticleRotation(float randomValue, float particleLifeTime)
{    
    // Zuf�llige Rotation interpolieren
    float rotationSpeed = lerp(RotationSpeed.x, RotationSpeed.y, randomValue);
    
	// Roation aufgrund der Lebensdauer berechnen
    float rotation = rotationSpeed * particleLifeTime;

    // 2x2 Matrix f�r Rotation
    float c = cos(rotation);
    float s = sin(rotation);
    
    return float2x2(c, -s, s, c);
}


/*******************************************PARTICLE EFFECT******************************************/


// Ein- und Ausgabestrukturen
struct VertexShaderInput
{
    float2 Corner : POSITION0;
    float3 Position : POSITION1;
    float3 Velocity : NORMAL0;
    float4 Random : COLOR0;
    float Time : TEXCOORD0;
};


struct VertexShaderOutput
{
    float4 Position : POSITION0;
    float4 Color : COLOR0;
    float2 TextureCoordinate : COLOR1;
};

// Custom vertex shader animates particles entirely on the GPU.
VertexShaderOutput ParticleVertexShader(VertexShaderInput input)
{
    VertexShaderOutput output;
    
    // Lebenszeit des Partikels berechnen mit leichter zuf�lliger Abweichung, um unterschiedliche Lebenszeiten zu erreichen
    float particleLifeTime = CurrentTime - input.Time + input.Random.x;
    
    // Normalisierte Lebenszeit zwischen 0 und 1
    float normalizedLifeTime = saturate(particleLifeTime / MaxParticleLifeTime);

    // Position des Partikels ermitteln
    output.Position = ComputeParticlePosition(input.Position, input.Velocity, normalizedLifeTime);

	// Gr��e ermitteln
    float size = ComputeParticleSize(input.Random.y, normalizedLifeTime);
	// Rotationsmatrix berechnen
    float2x2 rotation = ComputeParticleRotation(input.Random.w, particleLifeTime);

	// Rotation auf xy-Ebene anwenden, Gr��e anpassen und auf Bildschirmverh�ltnis anpassen
    output.Position.xy += mul(input.Corner, rotation) * size;
    
    output.Color = ComputeParticleColor(output.Position, input.Random.z, normalizedLifeTime);
    output.TextureCoordinate = (input.Corner + 1) / 2;
    
    return output;
}


// Pixel shader for drawing particles.
float4 ParticlePixelShader(VertexShaderOutput input) : COLOR0
{
    return tex2D(ParticleTextureSampler, input.TextureCoordinate) * input.Color;
}


/***********************************************TECHNIKEN***********************************************/

// Effect technique for drawing particles.
technique Particles
{
    pass Pass0
    {
        VertexShader = compile vs_2_0 ParticleVertexShader();
        PixelShader = compile ps_2_0 ParticlePixelShader();
    }
}
