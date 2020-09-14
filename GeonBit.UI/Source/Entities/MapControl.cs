using GoodOrBad.Framework;
using Xna = Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Routes.Graphics;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using MatterHackers.VectorMath;

using GameTime = Microsoft.Xna.Framework.GameTime;
using MatterHackers.Agg.Image;
using MatterHackers.Agg.VertexSource;
using MatterHackers.Agg;
using MonoGame.Extended;

namespace GeonBit.UI.Entities
{
    public class MapControl : Panel
    {
        private EarthView _mapsphere;
        private ImageBuffer _basetexture;
        //private Texture2D _overviewtexture;
        private Image _overview;
        private List<CylinderTextureInvisibleRegion> _regions;

        //2d ui
        public MapControl(Xna.Vector2 size, PanelSkin skin) : base(size, skin)
        {
            var mcolor = new Xna.Color(Xna.Color.White, 0.0f);
            //mcolor.A = (byte)(255 * 0.1f);
            //mcolor.A = (byte)(255 * 0.3f);

            FillColor = mcolor;

            _regions = new List<CylinderTextureInvisibleRegion>();
        }

        //3d
        public void Init(GoodOrBadGame game)
        {
            _overview = new Image();
            _overview.Anchor = Anchor.BottomRight;
            this.AddChild(_overview);

            _mapsphere = new EarthView();
            _mapsphere.OnInvisibleRegionsChange += OnInvisibleRegionsChange;
            _mapsphere.OnEarthTextureChange += OnEarthTextureChange;
            _mapsphere.Init(game); //this calls events on this control
        }

        private void ApplyRegionsToBase()
        {
            //copy the base texture
            var cur = new ImageBuffer(_basetexture);

            //draw on the regions
            VertexStorage poly = new VertexStorage();
            Graphics2D curGraphics2D = cur.NewGraphics2D();
            foreach (var entry in _regions)
            {
                var pts = entry.points;
                if (pts.Count > 0)
                {
                    var pt = pts[0];
                    pt.X *= cur.Width;
                    pt.Y = cur.Height - cur.Height*pt.Y;
                    poly.MoveTo(pt.X, pt.Y);
                }
                for (var i = 1; i<pts.Count; i++)
                {
                    var pt = pts[i];
                    pt.X *= cur.Width;
                    pt.Y = cur.Height - cur.Height*pt.Y;
                    poly.LineTo(pt.X, pt.Y);
                }

                curGraphics2D.Render(poly, Color.Red);
            }

            //assign to the overview texture
            var overviewtexture = SKUtils.Texture2DFromImageBuffer(GoodOrBadGame.Active, cur);
            _overview.Size = new Xna.Vector2(overviewtexture.Width, overviewtexture.Height);
            _overview.Texture = overviewtexture;
        }

        private void OnEarthTextureChange(EarthView view)
        {
            var tex = view.CurrentTexture;

            _basetexture = tex.Buffer;

            ApplyRegionsToBase();
        }

        private void OnInvisibleRegionsChange(EarthView view)
        {
            _regions = view.InvisibleMapRegions;

            //draw the lines on the overview texture
            ApplyRegionsToBase();
        }

        //3d
        public void Update3D(GoodOrBadGame game, GameTime gameTime)
        {
            _mapsphere.Update(game, gameTime);
        }

        public override void Draw(SpriteBatch spriteBatch, GameTime gameTime)
        {
            //Draw(GoodOrBadGame.Active, spriteBatch, gameTime);
            base.Draw(spriteBatch, gameTime);
        }

        //3d
        public void Draw3D(SpriteBatch spriteBatch, GoodOrBadGame game, GameTime gameTime)
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

        public override Xna.Rectangle CalcDestRect()
        {
            var rect = base.CalcDestRect();
            _mapsphere.UpdateViewport(rect);
            return rect;
        }

        override protected void DoBeforeUpdate()
        {

            // if focused, and got character input in this frame..
            if (IsFocused && !IsChildFocused)
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
