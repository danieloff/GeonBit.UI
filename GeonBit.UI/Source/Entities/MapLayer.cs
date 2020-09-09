using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using System;
using System.Collections.Generic;
using System.Text;

namespace GeonBit.UI.Entities
{
    class MapLayerTile
    {
        public RectangleF WorldBox;
        public int zoom;
        public Point TIleId;
        public Texture2D Texture;
    }
    class MapLayer : Entity
    {
        public List<MapLayerTile> Tiles;
        protected override void DrawEntity(SpriteBatch spriteBatch, DrawPhase phase)
        {
            if (phase == DrawPhase.Base)
            {
                foreach (var tile in Tiles)
                {
                    var rectSource = new Rectangle(0, 0, tile.Texture.Width, tile.Texture.Height);
                    var rectDest = new Rectangle(rectSource.X, rectSource.Y, rectSource.Width, rectSource.Height);

                    rectDest.Location += _destRect.Location;
                    UserInterface.Active.DrawUtils.DrawImage(spriteBatch, tile.Texture, rectDest, FillColor, Scale, rectSource);

                }
            }
            base.DrawEntity(spriteBatch, phase);
        }
    }
}
