using GoodOrBad.Framework;
using Xna = Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Routes.Graphics;
using System;
using System.Collections.Generic;
using MatterHackers.VectorMath;

using GameTime = Microsoft.Xna.Framework.GameTime;
using MatterHackers.Agg.Image;
using MatterHackers.Agg.VertexSource;
using MatterHackers.Agg;
using MonoGame.Extended;
using SkiaSharp;
using System.Threading.Tasks;

namespace GeonBit.UI.Entities
{
    public class MapControl : Panel
    {
        private const double REGIONS_WAIT_TIME = 0; //0.05; //max 20/sec

        private EarthView _mapsphere;
        private Label _fpslabel;
        private RawImgData _basetexture;
        private SKBitmap _cur;
        private SKCanvas _curGraphics2D;

        //private Texture2D _overviewtexture;
        private Image _overview;
        private bool _regionsdirty;
        private List<CylinderTextureInvisibleRegion> _regions;
        private double _waitregions;
        private ImageBuffer _curagg;
        private Graphics2D _curGraphics2DAGG;
        private bool _earthtexturechanged;
        private Action _nextoverviewtexture;
        private double _nextoverviewtimer = 0.0;
        private bool _showoverlaps = true;

        //2d ui
        public MapControl(Xna.Vector2 size, PanelSkin skin) : base(size, skin)
        {
            var mcolor = new Xna.Color(Xna.Color.White, 0.0f);
            //mcolor.A = (byte)(255 * 0.1f);
            //mcolor.A = (byte)(255 * 0.3f);

            FillColor = mcolor;

            _regions = new List<CylinderTextureInvisibleRegion>();
            _regionsdirty = false;
            _earthtexturechanged = false;
            _waitregions = 0.0;
        }

        //3d
        public void Init(GoodOrBadGame game)
        {
            _overview = new Image();
            _overview.Size = new Xna.Vector2(256, 256); // overviewtexture.Width, overviewtexture.Height);
            _overview.Anchor = Anchor.BottomRight;
            _overview.Visible = false;

            this.AddChild(_overview);

            _mapsphere = new EarthView();
            _mapsphere.OnInvisibleRegionsChange += OnInvisibleRegionsChange;
            _mapsphere.OnEarthTextureChange += OnEarthTextureChange;
            _mapsphere.Init(game); //this calls events on this control

            _fpslabel = new Label("FPS: 0", Anchor.TopLeft);
            this.AddChild(_fpslabel);
        }

        private void ApplyRegionsToFlatViewAGG()
        {
            if (_overview.Visible)
            {
                if (_showoverlaps)
                {
                    _nextoverviewtexture = this.SetBackgroundWithOverlaps;
                }
                else
                {
                    _nextoverviewtexture = this.SetBasicBackground;
                }
                if (_nextoverviewtimer == 0.0)
                {
                    _nextoverviewtimer = 0.33333;
                }
            }
            
        }

        private void SetBackgroundWithOverlaps()
        {
            //copy the base texture
            if (_curagg == null || _curagg.Width != _basetexture.Width || _curagg.Height != _basetexture.Height)
            {
                _curagg = new ImageBuffer(_basetexture.Width, _basetexture.Height, 32, new BlenderRGBA()); //_basetexture.Copy();
                _curGraphics2DAGG = _curagg.NewGraphics2D();
            }

            var src = _basetexture.Data;
            var dst = _curagg.GetBuffer();

            Array.Copy(src, dst, dst.Length);

            VertexStorage poly = new VertexStorage();
            foreach (var entry in _regions)
            {
                var pts = entry.points;
                if (pts.Count > 0)
                {
                    var pt = pts[0];
                    pt.X *= _curagg.Width;
                    pt.Y *= _curagg.Height;
                    poly.MoveTo(pt.X, pt.Y);
                }
                for (var i = 1; i < pts.Count; i++)
                {
                    var pt = pts[i];
                    pt.X *= _curagg.Width;
                    pt.Y *= _curagg.Height;
                    poly.LineTo(pt.X, pt.Y);
                }

                _curGraphics2DAGG.Render(poly, Color.Green);
            }

            //assign to the overview texture
            var overviewtexture = _overview.Texture;
            if (overviewtexture == null || overviewtexture.Width != _curagg.Width || overviewtexture.Height != _curagg.Height)
            {
                overviewtexture = new Texture2D(GoodOrBadGame.Active.GraphicsDevice, _curagg.Width, _curagg.Height);
            }

            var data = _curagg.GetBuffer();

            overviewtexture.SetData(data);
            _overview.Texture = overviewtexture;
        }

        private void SetBasicBackground()
        {
            var data = _basetexture.Data;

            var overviewtexture = _overview.Texture;
            if (overviewtexture == null || overviewtexture.Width != _basetexture.Width || overviewtexture.Height != _basetexture.Height)
            {
                _overview.Texture = new Texture2D(GoodOrBadGame.Active.GraphicsDevice, _basetexture.Width, _basetexture.Height);
                overviewtexture = _overview.Texture;
            }
            overviewtexture.SetData(data);
        }

        private void ApplyRegionsToFlatView()
        {
            ApplyRegionsToFlatViewAGG();
        }

        private void ApplyRegionsToFlatViewSK()
        {
            if (_overview.Visible)
            {
                //copy the base texture
                if (_cur == null || _cur.Width != _basetexture.Width || _cur.Height != _basetexture.Height)
                {
                    //_cur = new ImageBuffer(_basetexture.Width, _basetexture.Height, 32, new BlenderRGBA()); //_basetexture.Copy();
                    //_curGraphics2D = _cur.NewGraphics2D();
                    _cur = new SKBitmap(_basetexture.Width, _basetexture.Height, SKColorType.Rgba8888, SKAlphaType.Unpremul);
                    _curGraphics2D = new SKCanvas(_cur);
                }
                else
                {
                    //var src = _basetexture.GetBuffer();
                    //var dst = _cur.GetBuffer();

                    //Array.Copy(src, dst, dst.Length);

                    var ptraddr = _cur.GetPixels();

                    unsafe
                    {
                        var ptr = (byte*)ptraddr;
                        var idx = 0;
                        for (var row = 0; row < _basetexture.Height; row++)
                        {
                            for (var col = 0; col < _basetexture.Width * 4; col++)
                            {
                                *ptr++ = _basetexture.Data[idx++];
                            }
                        }
                    }
                }

                /*else
                {
                    //_basetexture.GetBuffer().CopyTo(_cur.GetBuffer(), 0);
                    //_basetexture.CopyTo(_cur);
                    var addrsrc = _basetexture.GetPixels();
                    var addrdst = _cur.GetPixels();

                    var w = _cur.Width;
                    var h = _cur.Height;

                    unsafe
                    {
                        int* ptr = (int*)addrsrc.ToPointer();
                        int* ptrdest = (int*)addrdst.ToPointer();

                        for (int row = 0; row < h; row++)
                        {
                            for (int col = 0; col < w; col++)
                            {
                                *ptrdest++ = *ptr++;
                            }
                        }
                    }
                }*/

                //_cur = new ImageBuffer(_basetexture);
                //_curGraphics2D.Clear(new Color(0, 0, 0, 0));
                //draw on the regions

                foreach (var entry in _regions)
                {
                    SKPath poly = new SKPath();
                    var pts = entry.points;
                    if (pts.Count > 0)
                    {
                        var pt = pts[0];
                        pt.X *= _cur.Width;
                        pt.Y *= _cur.Height;
                        poly.MoveTo((float)pt.X, (float)pt.Y);
                    }
                    for (var i = 1; i < pts.Count; i++)
                    {
                        var pt = pts[i];
                        pt.X *= _cur.Width;
                        pt.Y *= _cur.Height;
                        poly.LineTo((float)pt.X, (float)pt.Y);
                    }

                    SKPaint paint = new SKPaint()
                    {
                        Style = SKPaintStyle.Fill,
                        Color = SKColors.Cyan
                    };

                    _curGraphics2D.DrawPath(poly, paint);
                }

                /*
                VertexStorage poly = new VertexStorage();
                foreach (var entry in _regions)
                {
                    var pts = entry.points;
                    if (pts.Count > 0)
                    {
                        var pt = pts[0];
                        pt.X *= _cur.Width;
                        pt.Y *= _cur.Height;
                        poly.MoveTo(pt.X, pt.Y);
                    }
                    for (var i = 1; i < pts.Count; i++)
                    {
                        var pt = pts[i];
                        pt.X *= _cur.Width;
                        pt.Y *= _cur.Height;
                        poly.LineTo(pt.X, pt.Y);
                    }

                    _curGraphics2D.Render(poly, Color.Green);
                }
                */

                //assign to the overview texture
                var overviewtexture = _overview.Texture;
                if (overviewtexture == null || overviewtexture.Width != _cur.Width || overviewtexture.Height != _cur.Height)
                {
                    overviewtexture = new Texture2D(GoodOrBadGame.Active.GraphicsDevice, _cur.Width, _cur.Height);
                }

                //var curdata = _cur.GetBuffer();

                var spand = _cur.GetPixelSpan();
                var data = spand.ToArray();

                overviewtexture.SetData(data);
                _overview.Texture = overviewtexture;
            }
        }

        private void OnEarthTextureChange(EarthView view)
        {
            _earthtexturechanged = true;
        }

        private void OnInvisibleRegionsChange(EarthView view)
        {
            _regionsdirty = true;
        }

        public override void Update(ref Entity targetEntity, ref Entity dragTargetEntity, ref bool wasEventHandled, Xna.Point scrollVal)
        {
            
            base.Update(ref targetEntity, ref dragTargetEntity, ref wasEventHandled, scrollVal);
        }

        //3d
        public void Update3D(GoodOrBadGame game, GameTime gameTime)
        {
            _mapsphere.Update(game, gameTime);

            bool triggernextoverviewtimer = _nextoverviewtimer == 0;

            if (_earthtexturechanged)
            {

                _earthtexturechanged = false;
                var tex = _mapsphere.CurrentTexture;

                var raw = tex.Buffer;

                var img1 = new SKBitmap(raw.Width, raw.Height, SKColorType.Rgba8888, SKAlphaType.Unpremul);

                SKUtils.SetBuffer(img1, raw.Data);
                var info = img1.Info;
                info.Width = (int)_overview.Size.X;
                info.Height = (int)_overview.Size.Y;
                img1.Resize(info, SKFilterQuality.High);

                var spand = img1.GetPixelSpan();

                var data = spand.ToArray();

                _basetexture = new RawImgData(img1.Width, img1.Height, data);

                ApplyRegionsToFlatView();
            }

            if (_regionsdirty && _waitregions == 0.0)
            {

                _regionsdirty = false;
                _waitregions = REGIONS_WAIT_TIME;

                _regions = _mapsphere.InvisibleMapRegions; //always pull the fresh regions

                //update textures based on regions

                //draw the lines on the overview texture, if we want to show them. Drawing takes more cpu!
                ApplyRegionsToFlatView();
            }
            else if (_waitregions > 0.0)
            {
                _waitregions -= gameTime.GetElapsedSeconds();
                if (_waitregions < 0.0)
                {
                    _waitregions = 0.0;
                }
            }

            if (_nextoverviewtexture != null)
            {
                if (triggernextoverviewtimer)
                {
                    _nextoverviewtexture();
                    _nextoverviewtexture = null;
                }
                
                _nextoverviewtimer -= gameTime.GetElapsedSeconds();
                if (_nextoverviewtimer < 0.0)
                {
                    _nextoverviewtimer = 0.0;
                }
            }

            _fpslabel.Text = "FPS: " + (int)(1.0 / gameTime.GetElapsedSeconds());
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
                var oldkeystate = KeyboardInput.OldKeyboardState;
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

                if (oldkeystate.IsKeyDown(Keys.O) && keystate.IsKeyUp(Keys.O))
                {
                    _overview.Visible = !_overview.Visible;
                    if (_overview.Visible)
                    {
                        _regionsdirty = true;
                    }
                }
            }
            base.DoBeforeUpdate();
        }
    }
}
