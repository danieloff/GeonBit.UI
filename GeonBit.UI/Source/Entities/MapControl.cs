using GoodOrBad.Framework;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Routes.Graphics;
using System;
using System.Collections.Generic;
using System.Text;

namespace GeonBit.UI.Entities
{
    public class MapControl : Panel
    {
        private EarthView _mapsphere; 

        //2d ui
        public MapControl(Vector2 size, PanelSkin skin) : base(size, skin)
        {
            FillColor = new Color(Color.White, 0.0f); //0.1f
        }

        //3d
        public void Initialize(GoodOrBadGame game)
        {
            _mapsphere = new EarthView();
            _mapsphere.LoadContent(game);
        }

        //3d
        public void Update(GoodOrBadGame game, GameTime gameTime)
        {
            _mapsphere.Update(game, gameTime);
        }

        //3d
        public void Draw(GoodOrBadGame game, SpriteBatch spriteBatch, GameTime gameTime)
        {
            _mapsphere.Draw(game, spriteBatch, gameTime);
        }

        //2d ui
        protected override void DoOnMouseWheelScroll()
        {
            var delta = UserInterface.Active.MouseInputProvider.MouseWheelChange;
            _mapsphere.Zoom(delta);
            base.DoOnMouseWheelScroll();
        }

    }
}
