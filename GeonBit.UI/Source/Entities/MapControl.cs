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
        private RawImgData _curbuffer;
        private SKCanvas _curGraphics2D;

        //private Texture2D _overviewtexture;
        private Image _overview;
        private bool _regionsdirty;
        private List<CylinderTextureInvisibleRegion> _regions;
        private double _waitregions;
        private ImageBuffer _curagg;
        private Graphics2D _curGraphics2DAGG;
        private bool _earthtexturechanged;
        private Task<RawImgData> _nextoverviewtexture;
        private double _nextoverviewtimer = 0.0;
        private bool _showoverlaps = true;
        private bool _nextoverviewtexture_outofdate;
        private bool _useskbitmap = true;

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
            _nextoverviewtexture_outofdate = false;
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

        private RawImgData GetBackgroundWithOverlaps()
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

            var data = _curagg.GetBuffer();

            return new RawImgData(_curagg.Width, _curagg.Height, data);
        }

        private RawImgData GetBasicBackground()
        {
            return _basetexture;
        }

        private void ApplyRegionsToFlatView()
        {
            if (_overview.Visible)
            {
                if (_nextoverviewtexture == null)
                {
                    _nextoverviewtexture_outofdate = false;
                    if (_showoverlaps)
                    {
                        if (_useskbitmap)
                        {
                            _nextoverviewtexture = Task.Run(this.GetBackgroundWithOverlapsSK);
                        }
                        else
                        {
                            _nextoverviewtexture = Task.Run(this.GetBackgroundWithOverlaps);
                        }
                    }
                    else
                    {
                        _nextoverviewtexture = Task.Run(this.GetBasicBackground);
                    }
                    if (_nextoverviewtimer == 0.0)
                    {
                        _nextoverviewtimer = 0.33333;
                    }
                }
                else
                {
                    _nextoverviewtexture_outofdate = true;
                }
            }
        }

        private RawImgData GetBackgroundWithOverlapsSK()
        {
            //copy the base texture
            if (_cur == null || _cur.Width != _basetexture.Width || _cur.Height != _basetexture.Height)
            {
                _cur = new SKBitmap(_basetexture.Width, _basetexture.Height, SKColorType.Rgba8888, SKAlphaType.Unpremul);
                _curGraphics2D = new SKCanvas(_cur);
            }

            {
                var ptraddr = _cur.GetPixels();

                unsafe
                {
                    var dstptr = (byte*)ptraddr;
                    fixed (byte* srcptr = _basetexture.Data)
                    {
                        var bytesize = _basetexture.Data.Length;

                        Buffer.MemoryCopy(srcptr, dstptr, bytesize, bytesize);
                        /*for (var row = 0; row < _basetexture.Height; row++) //32bits at a time
                        {
                            for (var col = 0; col < _basetexture.Width; col++)
                            {
                                *ptr++ = *idx++;
                            }
                        }*/
                    }
                }
            }

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

            var spand = _cur.GetPixelSpan();

            if (_curbuffer == null || _curbuffer.Width != _cur.Width || _curbuffer.Height != _cur.Height)
            {
                var buff = spand.ToArray();
                _curbuffer = new RawImgData(_cur.Width, _cur.Height, buff);
            }
            else
            {
                spand.CopyTo(_curbuffer.Data);
            }

            return _curbuffer;
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
                if (_nextoverviewtexture != null && _nextoverviewtexture.IsCompleted)
                {
                    var img = _nextoverviewtexture.Result;

                    //assign to the overview texture
                    var overviewtexture = _overview.Texture;
                    if (overviewtexture == null || overviewtexture.Width != img.Width || overviewtexture.Height != img.Height)
                    {
                        overviewtexture = new Texture2D(GoodOrBadGame.Active.GraphicsDevice, img.Width, img.Height);
                        _overview.Texture = overviewtexture;
                    }

                    overviewtexture.SetData(img.Data);

                    _nextoverviewtexture = null;
                }
                if (triggernextoverviewtimer)
                {
                }
                
                _nextoverviewtimer -= gameTime.GetElapsedSeconds();
                if (_nextoverviewtimer < 0.0)
                {
                    _nextoverviewtimer = 0.0;
                }
            }
            else if (_nextoverviewtexture_outofdate)
            {
                ApplyRegionsToFlatView();
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
            _mapsphere.ZoomElevator(delta);
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

                var forward = 0.0f;
                if (keystate.IsKeyDown(Keys.Q))
                {
                    forward += 1;
                }
                if (keystate.IsKeyDown(Keys.Z))
                {
                    forward -= 1;
                }

                if (forward != 0)
                {
                    _mapsphere.Forward(forward);
                }

                var strafe = 0.0f;
                if (keystate.IsKeyDown(Keys.A))
                {
                    strafe += 1;
                }
                if (keystate.IsKeyDown(Keys.D))
                {
                    strafe -= 1;
                }

                if (strafe != 0)
                {
                    _mapsphere.Strafe(strafe);
                }

                var verticalstrafe = 0.0f;
                if (keystate.IsKeyDown(Keys.W))
                {
                    verticalstrafe += 1;
                }
                if (keystate.IsKeyDown(Keys.S))
                {
                    verticalstrafe -= 1;
                }

                if (verticalstrafe != 0)
                {
                    _mapsphere.VerticalStrafe(verticalstrafe);
                }

                var pitch = 0.0f;
                if (keystate.IsKeyDown(Keys.I))
                {
                    pitch -= 1;
                }
                if (keystate.IsKeyDown(Keys.K))
                {
                    pitch += 1;
                }

                if (pitch != 0)
                {
                    _mapsphere.Pitch(pitch);
                }

                var yaw = 0.0f;
                if (keystate.IsKeyDown(Keys.J))
                {
                    yaw -= 1;
                }
                if (keystate.IsKeyDown(Keys.L))
                {
                    yaw += 1;
                }

                if (yaw != 0)
                {
                    _mapsphere.Yaw(yaw);
                }

                var roll = 0.0f;
                if (keystate.IsKeyDown(Keys.U))
                {
                    roll -= 1;
                }
                if (keystate.IsKeyDown(Keys.O))
                {
                    roll += 1;
                }

                if (roll != 0)
                {
                    _mapsphere.Roll(roll);
                }

                if (zoom != 0)
                {
                    _mapsphere.ZoomElevator(zoom);
                }

                if (oldkeystate.IsKeyDown(Keys.F2) && keystate.IsKeyUp(Keys.F2))
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
