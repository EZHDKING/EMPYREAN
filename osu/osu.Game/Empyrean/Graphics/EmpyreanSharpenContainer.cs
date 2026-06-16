// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shaders;

namespace osu.Game.Empyrean.Graphics
{
    /// <summary>
    /// A buffered container whose final blit-to-screen runs the EZHDSR sharpening shader. Wrapping
    /// content in this captures it to a framebuffer, then draws that framebuffer to the screen using
    /// the contrast-adaptive sharpen pass — recovering edge clarity lost to low-resolution upscaling.
    ///
    /// <para>When sharpening is disabled it should not be used at all (the wrapper adds a buffer
    /// pass); the desktop only inserts it when the EZHDSR toggle is on.</para>
    /// </summary>
    public partial class EmpyreanSharpenContainer : BufferedContainer
    {
        public EmpyreanSharpenContainer()
            : base(cachedFrameBuffer: false)
        {
            RelativeSizeAxes = Axes.Both;
            RedrawOnScale = false;
        }

        protected override IShader GetCustomTextureShader(ShaderManager shaders)
            => shaders.Load(VertexShaderDescriptor.TEXTURE_2, "EZHDSR");
    }
}
