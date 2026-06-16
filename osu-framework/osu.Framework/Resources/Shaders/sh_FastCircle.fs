#ifndef FAST_CIRCLE_FS
#define FAST_CIRCLE_FS

// EMPYREAN performance variant of sh_FastCircle.fs.
//
// The upstream shader runs the circle distance computation at highp precision. On a GPU-bottlenecked
// system that per-fragment cost is one of the biggest drains in gameplay, because hit circles cover
// a large amount of screen area and overdraw heavily. This variant drops the hot distance math to
// mediump (ample for a circle edge; on integrated/mobile GPUs mediump can run at up to double rate)
// and keeps a hard, non-antialiased edge as the cheap default when BlendRange is 0 (which EMPYREAN's
// flat-gameplay hit objects request). A smooth edge is still produced when BlendRange is set, so
// nothing else regresses.

#undef HIGH_PRECISION_VERTEX
#define HIGH_PRECISION_VERTEX

#include "sh_Utils.h"
#include "sh_Masking.h"

layout(location = 2) in highp vec2 v_TexCoord;

layout(location = 0) out vec4 o_Colour;

void main(void)
{
    mediump vec2 halfSize = v_TexRect.zw * 0.5;
    mediump vec2 pixelPos = halfSize - abs(v_TexCoord - halfSize);
    mediump float radius = min(v_TexRect.z, v_TexRect.w) * 0.5;

    mediump float dst = max(pixelPos.x, pixelPos.y) > radius
        ? radius - min(pixelPos.x, pixelPos.y)
        : distance(pixelPos, vec2(radius));

    mediump float alpha = v_BlendRange.x == 0.0
        ? float(dst < radius)
        : clamp((radius - dst) / v_BlendRange.x, 0.0, 1.0);

    o_Colour = getRoundedColor(vec4(vec3(1.0), alpha), vec2(0.0));
}

#endif
