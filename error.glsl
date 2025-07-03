#version 140
#define HAS_MOD
#define HAS_DFDX
#define HAS_FLOAT_TEXTURES
#define HAS_SRGB
#define HAS_UNIFORM_BUFFERS
#define VERTEX_SHADER

// -- Utilities Start --

// It's literally just called the Z-Library for alphabetical ordering reasons.
//  - 20kdc

// -- varying/attribute/texture2D --

#ifndef HAS_VARYING_ATTRIBUTE
#define texture2D texture
#ifdef VERTEX_SHADER
#define varying out
#define attribute in
#else
#define varying in
#define attribute in
#define gl_FragColor colourOutput
out highp vec4 colourOutput;
#endif
#endif

#ifndef NO_ARRAY_PRECISION
#define ARRAY_LOWP lowp
#define ARRAY_MEDIUMP mediump
#define ARRAY_HIGHP highp
#else
#define ARRAY_LOWP lowp
#define ARRAY_MEDIUMP mediump
#define ARRAY_HIGHP highp
#endif

// -- shadow depth --

// If float textures are supported, puts the values in the R/G fields.
// This assumes RG32F format.
// If float textures are NOT supported.
// This assumes RGBA8 format.
// Operational range is "whatever works for FOV depth"
highp vec4 zClydeShadowDepthPack(highp vec2 val) {
#ifdef HAS_FLOAT_TEXTURES
    return vec4(val, 0.0, 1.0);
#else
    highp vec2 valH = floor(val);
    return vec4(valH / 255.0, val - valH);
#endif
}

// Inverts the previous function.
highp vec2 zClydeShadowDepthUnpack(highp vec4 val) {
#ifdef HAS_FLOAT_TEXTURES
    return val.xy;
#else
    return (val.xy * 255.0) + val.zw;
#endif
}

// -- srgb/linear conversion core --

highp vec4 zFromSrgb(highp vec4 sRGB)
{
    highp vec3 higher = pow((sRGB.rgb + 0.055) / 1.055, vec3(2.4));
    highp vec3 lower = sRGB.rgb / 12.92;
    highp vec3 s = max(vec3(0.0), sign(sRGB.rgb - 0.04045));
    return vec4(mix(lower, higher, s), sRGB.a);
}

highp vec4 zToSrgb(highp vec4 sRGB)
{
    highp vec3 higher = (pow(sRGB.rgb, vec3(0.41666666666667)) * 1.055) - 0.055;
    highp vec3 lower = sRGB.rgb * 12.92;
    highp vec3 s = max(vec3(0.0), sign(sRGB.rgb - 0.0031308));
    return vec4(mix(lower, higher, s), sRGB.a);
}

// -- uniforms --

#ifdef HAS_UNIFORM_BUFFERS
layout (std140) uniform projectionViewMatrices
{
    highp mat3 projectionMatrix;
    highp mat3 viewMatrix;
};

layout (std140) uniform uniformConstants
{
    highp vec2 SCREEN_PIXEL_SIZE;
    highp float TIME;
};
#else
uniform highp mat3 projectionMatrix;
uniform highp mat3 viewMatrix;
uniform highp vec2 SCREEN_PIXEL_SIZE;
uniform highp float TIME;
#endif

uniform sampler2D TEXTURE;
uniform highp vec2 TEXTURE_PIXEL_SIZE;

// -- srgb emulation --

#ifdef HAS_SRGB

highp vec4 zTextureSpec(sampler2D tex, highp vec2 uv)
{
    return texture2D(tex, uv);
}

highp vec4 zAdjustResult(highp vec4 col)
{
    return col;
}
#else
uniform lowp vec2 SRGB_EMU_CONFIG;

highp vec4 zTextureSpec(sampler2D tex, highp vec2 uv)
{
    highp vec4 col = texture2D(tex, uv);
    if (SRGB_EMU_CONFIG.x > 0.5)
    {
        return zFromSrgb(col);
    }
    return col;
}

highp vec4 zAdjustResult(highp vec4 col)
{
    if (SRGB_EMU_CONFIG.y > 0.5)
    {
        return zToSrgb(col);
    }
    return col;
}
#endif

highp vec4 zTexture(highp vec2 uv)
{
    return zTextureSpec(TEXTURE, uv);
}

// -- color --

// Grayscale function for the ITU's Rec BT-709. Primarily intended for HDTVs, but standard sRGB monitors are coincidentally extremely close.
highp float zGrayscale_BT709(highp vec3 col) {
    return dot(col, vec3(0.2126, 0.7152, 0.0722));
}

// Grayscale function for the ITU's Rec BT-601, primarily intended for SDTV, but amazing for a handful of niche use-cases.
highp float zGrayscale_BT601(highp vec3 col) {
    return dot(col, vec3(0.299, 0.587, 0.114));
}

// If you don't have any reason to be specifically using the above grayscale functions, then you should default to this.
highp float zGrayscale(highp vec3 col) {
    return zGrayscale_BT709(col);
}

// -- noise --

//zRandom, zNoise, and zFBM are derived from https://godotshaders.com/snippet/2d-noise/ and https://godotshaders.com/snippet/fractal-brownian-motion-fbm/
highp vec2 zRandom(highp vec2 uv){
    uv = vec2( dot(uv, vec2(127.1,311.7) ),
               dot(uv, vec2(269.5,183.3) ) );
    return -1.0 + 2.0 * fract(sin(uv) * 43758.5453123);
}

highp float zNoise(highp vec2 uv) {
    highp vec2 uv_index = floor(uv);
    highp vec2 uv_fract = fract(uv);

    highp vec2 blur = smoothstep(0.0, 1.0, uv_fract);

    return mix( mix( dot( zRandom(uv_index + vec2(0.0,0.0) ), uv_fract - vec2(0.0,0.0) ),
                     dot( zRandom(uv_index + vec2(1.0,0.0) ), uv_fract - vec2(1.0,0.0) ), blur.x),
                mix( dot( zRandom(uv_index + vec2(0.0,1.0) ), uv_fract - vec2(0.0,1.0) ),
                     dot( zRandom(uv_index + vec2(1.0,1.0) ), uv_fract - vec2(1.0,1.0) ), blur.x), blur.y) * 0.5 + 0.5;
}

highp float zFBM(highp vec2 uv) {
    const int octaves = 6;
    highp float amplitude = 0.5;
    highp float frequency = 3.0;
    highp float value = 0.0;

    for(int i = 0; i < octaves; i++) {
        value += amplitude * zNoise(frequency * uv);
        amplitude *= 0.5;
        frequency *= 2.0;
    }
    return value;
}


// -- generative --

// Function that creates a circular gradient. Screenspace shader bread n butter.
highp float zCircleGradient(highp vec2 ps, highp vec2 coord, highp float maxi, highp float radius, highp float dist, highp float power) {
    highp float rad = (radius * ps.y) * 0.001;
    highp float aspectratio = ps.x / ps.y;
    highp vec2 totaldistance = ((ps * 0.5) - coord) / (rad * ps);
    totaldistance.x *= aspectratio;
    highp float length = (length(totaldistance) * ps.y) - dist;
    return pow(clamp(length, 0.0, maxi), power);
}

// -- Utilities End --

// Vertex position.
/*layout (location = 0)*/ attribute vec2 aPos;
// Texture coordinates.
/*layout (location = 1)*/ attribute vec2 tCoord;
/*layout (location = 2)*/ attribute vec2 tCoord2;
// Colour modulation.
/*layout (location = 3)*/ attribute vec4 modulate;

varying vec2 UV;
varying vec2 UV2;
varying vec2 Pos;
varying vec4 VtxModulate;

// Maybe we should merge these CPU side.
// idk yet.
uniform mat3 modelMatrix;

// Allows us to do texture atlassing with texture coordinates 0->1
// Input texture coordinates get mapped to this range.
uniform vec4 modifyUV;
// TODO CLYDE Is this still needed?

uniform sampler2D SCREEN_TEXTURE;
uniform ARRAY_HIGHP float intensity;


ARRAY_HIGHP float noise( ARRAY_HIGHP vec2 p) {
 const highp float K1 = 0.366025404 ;
 const highp float K2 = 0.211324865 ;
 highp vec2 i = floor ( p + ( p . x + p . y ) * K1 ) ;
 highp vec2 a = p - i + ( i . x + i . y ) * K2 ;
 highp float m = step ( a . y , a . x ) ;
 highp vec2 o = vec2 ( m , 1.0 - m ) ;
 highp vec2 b = a - o + K2 ;
 highp vec2 c = a - 1.0 + 2.0 * K2 ;
 highp vec3 h = max ( 0.5 - vec3 ( dot ( a , a ) , dot ( b , b ) , dot ( c , c ) ) , 0.0 ) ;
 highp vec3 n = h * h * h * h * vec3 ( dot ( a , fract ( sin ( i * 43758.5 ) * 2.0 - 1.0 ) , dot ( b , fract ( sin ( ( i + o ) * 43758.5 ) * 2.0 - 1.0 ) , dot ( c , fract ( sin ( ( i + 1.0 ) * 43758.5 ) * 2.0 - 1.0 ) ) ;
 return dot ( n , vec3 ( 70.0 ) ) ;

}
ARRAY_HIGHP vec3 hsv2rgb_smooth( ARRAY_HIGHP vec3 c) {
 highp vec3 rgb = clamp ( abs ( mod ( c . x * 6.0 + vec3 ( 0.0 , 4.0 , 2.0 ) , 6.0 ) - 3.0 ) - 1.0 , 0.0 , 1.0 ) ;
 rgb = rgb * rgb * ( 3.0 - 2.0 * rgb ) ;
 return c . z * mix ( vec3 ( 1.0 ) , rgb , c . y ) ;

}
ARRAY_HIGHP float mixNoise( ARRAY_HIGHP vec2 point,  ARRAY_HIGHP float phase2) {
 highp float time = TIME * 0.15 + phase2 ;
 highp float a = noise ( 4.0 * point - time ) ;
 highp float b = noise ( 4.0 * point + time ) ;
 return mix ( a , b , 0.5 ) ;

}
ARRAY_HIGHP float genGradient( ARRAY_HIGHP vec2 center,  ARRAY_HIGHP vec2 coord,  ARRAY_HIGHP float radius,  ARRAY_HIGHP float minDist,  ARRAY_HIGHP float power) {
 return pow ( clamp ( ( length ( center - coord ) / radius ) - minDist , 0.0 , 1.0 ) , power ) ;

}


void main()
{
    vec3 transformed = projectionMatrix * viewMatrix * modelMatrix * vec3(aPos, 1.0);
    vec2 VERTEX = transformed.xy;

    // [SHADER_CODE]

    // Pixel snapping to avoid sampling issues on nvidia.
    VERTEX += 1.0;
    VERTEX /= SCREEN_PIXEL_SIZE*2.0;
    VERTEX = floor(VERTEX + 0.5);
    VERTEX *= SCREEN_PIXEL_SIZE*2.0;
    VERTEX -= 1.0;

    gl_Position = vec4(VERTEX, 0.0, 1.0);
    Pos = (VERTEX + 1.0) / 2.0;
    UV = mix(modifyUV.xy, modifyUV.zw, tCoord);
    UV2 = tCoord2;

    // Negative modulation is being used as a hacky way to squeeze in lighting data.
    // I.e., negative modulation implies we ignore the lighting.
    if (modulate.x < 0.0)
    {
        VtxModulate = -1.0 - zFromSrgb(-1.0 - modulate);
    }
    else
    {
        VtxModulate = zFromSrgb(modulate);
    }
}
