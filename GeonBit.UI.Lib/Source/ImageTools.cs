using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace GeonBit.UI
{
    public class ImageTools
    {
        public static Texture2D LoadTextureFromFile(string filePath, GraphicsDevice graphicsDevice)
        {
            try
            {
                using (System.IO.Stream stream = TitleContainer.OpenStream(filePath)) //TODO what is TitleContainer?
                {
                    return LoadTextureFromStream(stream, graphicsDevice);
                }
            }
            catch
            {
                throw new System.IO.FileLoadException("Cannot load '" + filePath + "' file!");
            }
        }

        public static Texture2D LoadTextureFromStream(Stream stream, GraphicsDevice graphicsDevice)
        {
            //https://stackoverflow.com/questions/13541489/load-texture2d-from-stream-with-rendertarget2d-textures-disapear-after-game-win
            Texture2D file = null;
            Texture2D resultTexture;
            RenderTarget2D result = null;

            //try
            //{
                //using (System.IO.Stream titleStream = TitleContainer.OpenStream(filePath))
                //{
                    file = Texture2D.FromStream(graphicsDevice, stream);
                //}
            //}
            //catch
            //{
            //    throw new System.IO.FileLoadException("Cannot load '" + filePath + "' file!");
            //}
            PresentationParameters pp = graphicsDevice.PresentationParameters;
            //Setup a render target to hold our final texture which will have premulitplied alpha values
            result = new RenderTarget2D(graphicsDevice, file.Width, file.Height, true, pp.BackBufferFormat, pp.DepthStencilFormat);

            graphicsDevice.SetRenderTarget(result);
            graphicsDevice.Clear(Color.Black);

            //Multiply each color by the source alpha, and write in just the color values into the final texture
            BlendState blendColor = new BlendState();
            blendColor.ColorWriteChannels = ColorWriteChannels.Red | ColorWriteChannels.Green | ColorWriteChannels.Blue;

            blendColor.AlphaDestinationBlend = Blend.Zero;
            blendColor.ColorDestinationBlend = Blend.Zero;

            blendColor.AlphaSourceBlend = Blend.SourceAlpha;
            blendColor.ColorSourceBlend = Blend.SourceAlpha;

            SpriteBatch spriteBatch = new SpriteBatch(graphicsDevice);
            spriteBatch.Begin(SpriteSortMode.Immediate, blendColor);
            spriteBatch.Draw(file, file.Bounds, Color.White);
            spriteBatch.End();

            //Now copy over the alpha values from the PNG source texture to the final one, without multiplying them
            BlendState blendAlpha = new BlendState();
            blendAlpha.ColorWriteChannels = ColorWriteChannels.Alpha;

            blendAlpha.AlphaDestinationBlend = Blend.Zero;
            blendAlpha.ColorDestinationBlend = Blend.Zero;

            blendAlpha.AlphaSourceBlend = Blend.One;
            blendAlpha.ColorSourceBlend = Blend.One;

            spriteBatch.Begin(SpriteSortMode.Immediate, blendAlpha);
            spriteBatch.Draw(file, file.Bounds, Color.White);
            spriteBatch.End();

            //Release the GPU back to drawing to the screen
            graphicsDevice.SetRenderTarget(null);

            resultTexture = new Texture2D(graphicsDevice, result.Width, result.Height);
            Color[] data = new Color[result.Height * result.Width];
            Color[] textureColor = new Color[result.Height * result.Width];

            result.GetData<Color>(textureColor);

            for (int i = 0; i < result.Height; i++)
            {
                for (int j = 0; j < result.Width; j++)
                {
                    data[j + i * result.Width] = textureColor[j + i * result.Width];
                }
            }

            resultTexture.SetData(data);

            return resultTexture;
        }
    }
}
