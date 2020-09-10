using GoodOrBad.Framework;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
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
            var mcolor = new Color(Color.White, 0.0f);
            mcolor.A = (byte)(255 * 0.1f);

            FillColor = mcolor;
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

        override protected void DoBeforeUpdate()
        {

            // if focused, and got character input in this frame..
            if (IsFocused)
            {
                var dir = Vector2.Zero;
                var keystate = KeyboardInput.NewKeyboardState;
                if (keystate.IsKeyDown(Keys.Left))
                {
                    dir.X -= 1;
                }
                if (keystate.IsKeyDown(Keys.Right))
                {
                    dir.X += 1;
                }
                if (keystate.IsKeyDown(Keys.Up))
                {
                    dir.Y -= 1;
                }
                if (keystate.IsKeyDown(Keys.Down))
                {
                    dir.Y += 1;
                }

                if (dir != Vector2.Zero)
                {
                    _mapsphere.SurfaceRotate(dir);
                }

                var zoom = 0.0f;
                if (keystate.IsKeyDown(Keys.OemPlus) || keystate.IsKeyDown(Keys.Add))
                {
                    zoom -= 1;
                }
                if (keystate.IsKeyDown(Keys.OemMinus) || keystate.IsKeyDown(Keys.Subtract))
                {
                    zoom += 1;
                }
                
                if (zoom != 0)
                {
                    _mapsphere.Zoom(zoom);
                }
            }
            base.DoBeforeUpdate();
        }
    }
}
