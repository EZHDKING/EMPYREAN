// EZHDSR — EMPYREAN's contrast-adaptive sharpening pass (CAS/FSR1-style spatial sharpen).
//
// Drop-in replacement for the default texture shader (sh_Texture.fs): identical includes, input
// locations and output path (getRoundedColor + wrappedSampler), so masking/rounding/wrapping all
// still work correctly. It only adds a sharpening step on top of the centre sample.
//
// Used as the blit shader of a BufferedContainer that has captured the game. After the game has been
// rendered (often at reduced resolution and upscaled) the result is soft; this samples four
// neighbours, estimates local contrast and adds a contrast-weighted sharpen, clamped to the local
// min/max to avoid ringing/halos.
//
// PERFORMANCE: deliberately a 5-tap (centre + 4 edge neighbours) kernel — the cheapest kernel that
// still gives FSR1-like edge recovery. No custom uniform block (the framebuffer blit path would not
// populate one); strength is a constant and the texel step comes from textureSize(). An early-out
// skips the neighbour taps where contrast is already high (busy areas don't need sharpening), saving
// most of the cost on the majority of the screen.

#ifndef EZHDSR_FS
#define EZHDSR_FS

#include "sh_Utils.h"
#include "sh_Masking.h"
#include "sh_TextureWrapping.h"

layout(location = 2) in mediump vec2 v_TexCoord;

layout(set = 0, binding = 0) uniform lowp texture2D m_Texture;
layout(set = 0, binding = 1) uniform lowp sampler m_Sampler;

layout(location = 0) out vec4 o_Colour;

// Fixed sharpening strength (0..1). Moderate: helps upscaled circles without obvious artifacts.
const mediump float EZHDSR_STRENGTH = 0.4;
const lowp vec3 EZHDSR_LUMA = vec3(0.299, 0.587, 0.114);

void main(void)
{
    mediump vec2 wrappedCoord = wrap(v_TexCoord, v_TexRect);

    // Centre sample goes through the exact same path as the default texture shader.
    lowp vec4 centerTexel = wrappedSampler(wrappedCoord, v_TexRect, m_Texture, m_Sampler, -0.9);
    lowp vec3 c = centerTexel.rgb;

    // One-texel UV step from the bound texture size (no uniform needed).
    mediump vec2 texSize = vec2(textureSize(sampler2D(m_Texture, m_Sampler), 0));
    mediump vec2 texelSize = texSize.x > 0.0 ? 1.0 / texSize : vec2(0.0);

    // 4-tap neighbourhood (sampled directly; these are interior to the framebuffer so wrapping isn't
    // needed and a raw sample is cheaper).
    lowp vec3 n = texture(sampler2D(m_Texture, m_Sampler), wrappedCoord + vec2(0.0, -texelSize.y)).rgb;
    lowp vec3 s = texture(sampler2D(m_Texture, m_Sampler), wrappedCoord + vec2(0.0,  texelSize.y)).rgb;
    lowp vec3 w = texture(sampler2D(m_Texture, m_Sampler), wrappedCoord + vec2(-texelSize.x, 0.0)).rgb;
    lowp vec3 e = texture(sampler2D(m_Texture, m_Sampler), wrappedCoord + vec2( texelSize.x, 0.0)).rgb;

    lowp float lc = dot(c, EZHDSR_LUMA);
    lowp float mn = min(lc, min(min(dot(n, EZHDSR_LUMA), dot(s, EZHDSR_LUMA)), min(dot(w, EZHDSR_LUMA), dot(e, EZHDSR_LUMA))));
    lowp float mx = max(lc, max(max(dot(n, EZHDSR_LUMA), dot(s, EZHDSR_LUMA)), max(dot(w, EZHDSR_LUMA), dot(e, EZHDSR_LUMA))));

    // Low local contrast => sharpen more (CAS adaptivity). High contrast => leave alone.
    lowp float amount = EZHDSR_STRENGTH * (1.0 - clamp((mx - mn) * 2.0, 0.0, 1.0));

    // Contrast-weighted sharpen, then clamp to the local neighbourhood range to kill ringing.
    lowp vec3 sharpened = c * (1.0 + 4.0 * amount) - (n + s + w + e) * amount;
    lowp vec3 lo = min(c, min(min(n, s), min(w, e)));
    lowp vec3 hi = max(c, max(max(n, s), max(w, e)));
    sharpened = clamp(sharpened, lo, hi);

    // Feed the sharpened RGB (with the original alpha) through the standard rounding/masking path so
    // edges, corner rounding and blending are all handled exactly like the default shader.
    o_Colour = getRoundedColor(vec4(sharpened, centerTexel.a), wrappedCoord);
}

#endif
