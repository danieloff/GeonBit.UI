using GoodOrBad.Framework;
using MatterHackers.Agg;
using MatterHackers.VectorMath;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGame.Extended;
using Routes.Graphics;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Security.Policy;
using System.Threading.Tasks;
using GameTime = Microsoft.Xna.Framework.GameTime;
using Xna = Microsoft.Xna.Framework;

namespace GeonBit.UI.Entities
{
    public class MapControl : Panel
    {
        private const double REGIONS_WAIT_TIME = 0; //0.05; //max 20/sec

        private EarthView _mapsphere;
        private Label _fpslabel;

        private RawImgData _baseoverviewbuffer;
        private RawImgData _baseheatbuffer;

        private SKBitmap _overviewsk;
        private SKBitmap _heatsk;

        private RawImgData _overviewbuffer;
        private RawImgData _heatbuffer;

        private SKCanvas _overviewskGraphics2D;
        private SKCanvas _heatskGraphics2D;

        private Image _overview;
        private Image _overviewheat;

        private Task<RawImgData> _nextoverviewtexture;
        private Task<RawImgData> _nextheattexture;

        //private ImageBuffer _curagg;
        //private Graphics2D _curGraphics2DAGG;

        private bool _regionsdirty;
        private List<CylinderTextureInvisibleRegion> _regions;
        private double _waitregions;
        private bool _earthtexturechanged;
        private double _nextoverviewtimer = 0.0;
        private bool _showoverlaps = true;
        private bool _nextoverviewtexture_outofdate;
        private bool _nextheattexture_outofdate;
        private bool _useskbitmap = true;
        private bool _updatetexturesusingoverviewarea;

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
            _nextheattexture_outofdate = false;
        }

        //3d
        public void Init(GoodOrBadGame game)
        {
            _overview = new Image();
            _overview.Size = new Xna.Vector2(256, 256);
            _overview.Anchor = Anchor.BottomRight;
            _overview.Visible = false;

            this.AddChild(_overview);

            _overviewheat = new Image();
            _overviewheat.Size = new Xna.Vector2(256, 256);
            _overviewheat.Anchor = Anchor.BottomRight;
            _overviewheat.Offset = new Xna.Vector2(_overview.Size.X + _overview.Size.X / 4, 0);
            _overviewheat.Visible = false;

            this.AddChild(_overviewheat);

            _mapsphere = new EarthView();
            _mapsphere.OnInvisibleRegionsChange += OnInvisibleRegionsChange;
            _mapsphere.OnEarthTextureLowResChange += OnEarthLowResTextureChange;
            _mapsphere.Init(game); //this calls events on this control

            _fpslabel = new Label("FPS: 0", Anchor.TopLeft);
            this.AddChild(_fpslabel);
        }

        /*
        private RawImgData GetBackgroundWithOverlaps()
        {
            //copy the base texture
            if (_curagg == null || _curagg.Width != _baseoverviewbuffer.Width || _curagg.Height != _baseoverviewbuffer.Height)
            {
                _curagg = new ImageBuffer(_baseoverviewbuffer.Width, _baseoverviewbuffer.Height, 32, new BlenderRGBA()); //_basetexture.Copy();
                _curGraphics2DAGG = _curagg.NewGraphics2D();
            }

            var src = _baseoverviewbuffer.Data;
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
        */

        private RawImgData GetBasicBackground()
        {
            return _baseoverviewbuffer;
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
                        _nextoverviewtexture = Task.Run(() => this.GetBackgroundWithOverlapsSK(ref _overviewsk, ref _overviewskGraphics2D, ref _overviewbuffer, _baseoverviewbuffer, null));
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

        private void ApplyRegionsToHeatView()
        { 
            if (_overviewheat.Visible)
            {
                if (_nextheattexture == null)
                {
                    _nextheattexture_outofdate = false;
                    _nextheattexture = Task.Run(() => this.GetBackgroundWithOverlapsSK(ref _heatsk,  ref _heatskGraphics2D, ref _heatbuffer, _baseheatbuffer, new SKColor(255, 255, 255, 255)));
                }
                else
                {
                    _nextheattexture_outofdate = true;
                }
            }
        }

        private RawImgData UpdateTexturesUsingOverviewArea(ref SKBitmap skb, ref SKCanvas skg, RawImgData outbuffer, RawImgData baseimg, SKColor? basecolor)
        {
            var buff = baseimg;
            var data = buff.Data;

            var zoom = 0;

            var xmin = buff.Width;
            var xmax = 0;
            var ymin = buff.Height;
            var ymax = 0;

            bool found = false;
            for (var i = 0; i < buff.Height; i++)
            {
                for (var j = 0; j < buff.Width; j++)
                {
                    var pr = data[i * buff.Width * 4 + j * 4 + 0];
                    var pg = data[i * buff.Width * 4 + j * 4 + 1];
                    var pb = data[i * buff.Width * 4 + j * 4 + 2];
                    var pa = data[i * buff.Width * 4 + j * 4 + 3];

                    if (pr == 255 && pg == 255 && pb == 255 && pa == 255) //white
                    {
                        if (j < xmin)
                        {
                            xmin = j;
                        }
                        if (j > xmax)
                        {
                            xmax = j;
                        }
                        if (i < ymin)
                        {
                            ymin = i;
                        }
                        if (i > ymax)
                        {
                            ymax = i;
                        }
                        found = true;
                    }
                }
            }
            //if (!found)
            {
                //that would be a problem
            }

            //handle possible split
            bool split = false;

            var xdividemin = 0;
            var xdividemax = buff.Width;

            if (xmin == 0 && xmax == buff.Width-1)
            {
                for (var j = 0; j < buff.Width; j++)
                {
                    bool verticaldivider = true;
                    for (var i = 0; i < buff.Height; i++)
                    {
                        var pr = data[i * buff.Width * 4 + j * 4 + 0];
                        var pg = data[i * buff.Width * 4 + j * 4 + 1];
                        var pb = data[i * buff.Width * 4 + j * 4 + 2];
                        var pa = data[i * buff.Width * 4 + j * 4 + 3];

                        if (pr == 255 && pg == 255 && pb == 255 && pa == 255) //white
                        {
                            verticaldivider = false;
                            break;
                        }
                    }
                    if (xdividemin == 0 && verticaldivider)
                    {
                        xdividemin = j;
                        split = true;
                    }
                    if (verticaldivider)
                    {
                        xdividemax = j;
                        split = true;
                    }
                }
            }

            var xmin2 = xmin;
            var ymin2 = ymin;
            var xmax2 = xmax;
            var ymax2 = ymax;

            var xmin2_1 = xmin;
            var ymin2_1 = ymin;
            var xmax2_1 = xmax;
            var ymax2_1 = ymax;

            var xmin2_2 = xmin;
            var ymin2_2 = ymin;
            var xmax2_2 = xmax;
            var ymax2_2 = ymax;

            //find power of two that covers the area
            if (!split && found)
            {
                var w = xmax - xmin + 1;
                var h = ymax - ymin + 1;

                var bw = buff.Width;
                var bh = buff.Height;

                var cw = bw;
                var ch = bh;

                while (cw / 2 >= w && ch / 2 >= h) //square textures for now...
                {
                    cw /= 2;
                    zoom += 1;
                    //}

                    //while (ch/2 >= h)
                    //{
                    ch /= 2;
                    //zoomy += 1;
                }

                zoom += 1;
                var byfourw = (int)Math.Ceiling(cw / 2.0); //find the by four width, watch for zero
                var byfourh = (int)Math.Ceiling(ch / 2.0);

                //found the power of two box, now find the actual boxes needed.
                var cx = xmin;
                var cy = ymin;

                var floorcx = cx / byfourw;
                var floorcy = cy / byfourh;

                var left = byfourw * floorcx;
                var bottom = byfourh * floorcy;

                var right = left + cw;
                var top = bottom + ch;

                //make it bigger if shifting it caused a problem
                if (right < xmax)
                {
                    //cx = left;
                    cw *= 2;
                    ch *= 2;
                    zoom -= 1;
                    right = left + cw;
                    top = bottom + ch;
                }
                if (top < ymax)
                {
                    //cy = bottom;
                    ch *= 2;
                    cw *= 2;
                    zoom -= 1;
                    top = bottom + ch;
                    right = left + cw;
                }

                if (top > buff.Height)
                {
                    var diff = (buff.Height) - top;
                    top += diff;
                    bottom += diff;
                }

                if (right > buff.Width)
                {
                    var diff = (buff.Width) - right;
                    right += diff;
                    left += diff;
                }

                xmin2 = left;
                ymin2 = bottom;
                xmax2 = right - 1;
                ymax2 = top - 1;
            }
            else if (found)
            {
                //find power of two that covers each area
                { //left
                    var w = xdividemin + (buff.Width - (xdividemax + 1));
                    var h = ymax - ymin + 1;

                    var bw = buff.Width;
                    var bh = buff.Height;

                    var cw = bw;
                    var ch = bh;

                    while (cw / 2 >= w && ch / 2 >= h) //square textures for now...
                    {
                        cw /= 2;
                        zoom += 1;
                        //}

                        //while (ch/2 >= h)
                        //{
                        ch /= 2;
                        //zoomy += 1;
                    }

                    zoom += 1;
                    var byfourw = (int)Math.Ceiling(cw / 2.0); //find the by four width, watch for zero
                    var byfourh = (int)Math.Ceiling(ch / 2.0);

                    //found the power of two box, now find the actual boxes needed.
                    var cx = xdividemax + 1;
                    var cy = ymin;

                    var floorcx = cx / byfourw;
                    var floorcy = cy / byfourh;

                    var left = byfourw * floorcx;
                    var bottom = byfourh * floorcy;

                    var right = left + cw;
                    var top = bottom + ch;

                    //make it bigger if shifting it caused a problem
                    if (right < xdividemax + 1 + w)
                    {
                        //cx = left;
                        cw *= 2;
                        ch *= 2;
                        zoom -= 1;
                        right = left + cw;
                        top = bottom + ch;
                    }
                    if (top < ymax)
                    {
                        //cy = bottom;
                        ch *= 2;
                        cw *= 2;
                        zoom -= 1;
                        top = bottom + ch;
                        right = left + cw;
                    }

                    if (top > buff.Height)
                    {
                        var diff = (buff.Height) - top;
                        top += diff;
                        bottom += diff;
                    }

                    xmin2_2 = left;
                    ymin2_2 = bottom;
                    xmax2_2 = buff.Width - 1;
                    ymax2_2 = top - 1;

                    xmin2_1 = 0;
                    ymin2_1 = bottom;
                    xmax2_1 = right - buff.Width - 1;
                    ymax2_1 = top - 1;
                }
            }

            //request more detail

            int destzoom = _mapsphere.LowResZoomLevel;
            Point2D tilebottomleft;
            Point2D tiletopright;

            if (!split && found)
            {
                var bl = new Vector2(xmin2 / (double)buff.Width, ymin2 / (double)buff.Height);
                var tr = new Vector2(xmax2 / (double)buff.Width, ymax2 / (double)buff.Height);

                tilebottomleft = new Point2D((int)(bl.X * Math.Pow(2, zoom)), (int)(bl.Y * Math.Pow(2, zoom)));
                tiletopright = new Point2D((int)(tr.X * Math.Pow(2, zoom)), (int)(tr.Y * Math.Pow(2, zoom))); //inclusive 


                _mapsphere.SetHighResZone(tilebottomleft, tiletopright, zoom);
            }
            else
            {
                //throw new NotImplementedException("need to implement split zoom");
            }
            

            //update the overview with the viewbox

            //copy the texture over
            BeforeSkDraw(ref skb, ref skg, buff, null);

            SKPaint paint = new SKPaint()
            {
                Style = SKPaintStyle.Stroke,
                Color = SKColors.Blue
            };

            SKPaint paint2 = new SKPaint()
            {
                Style = SKPaintStyle.Stroke,
                Color = SKColors.Red
            };

            if (split == false && found)
            {
                {

                    SKPath poly = new SKPath();
                    poly.MoveTo(xmin, ymin);
                    poly.LineTo(xmax, ymin);
                    poly.LineTo(xmax, ymax);
                    poly.LineTo(xmin, ymax);
                    poly.LineTo(xmin, ymin);

                    skg.DrawPath(poly, paint);
                }

                //bounds
                {

                    SKPath poly = new SKPath();

                    poly.MoveTo(xmin2, ymin2);
                    poly.LineTo(xmax2, ymin2);
                    poly.LineTo(xmax2, ymax2);
                    poly.LineTo(xmin2, ymax2);
                    poly.LineTo(xmin2, ymin2);

                    skg.DrawPath(poly, paint2);
                }

            }
            else if (split == true && found)
            {
                {   
                    // left
                    {
                        SKPath poly = new SKPath();

                        poly.MoveTo(0, ymin);
                        poly.LineTo(xdividemin, ymin);
                        poly.LineTo(xdividemin, ymax);
                        poly.LineTo(0, ymax);
                        poly.LineTo(0, ymin);

                        skg.DrawPath(poly, paint);
                    }

                    //left bounds
                    {
                        SKPath poly = new SKPath();

                        poly.MoveTo(xmin2_1, ymin2_1);
                        poly.LineTo(xmax2_1, ymin2_1);
                        poly.LineTo(xmax2_1, ymax2_1);
                        poly.LineTo(xmin2_1, ymax2_1);
                        poly.LineTo(xmin2_1, ymin2_1);

                        skg.DrawPath(poly, paint2);
                    }
                }
                {
                    //right
                    {

                        SKPath poly = new SKPath();

                        poly.MoveTo(xdividemax, ymin);
                        poly.LineTo(buff.Width-1, ymin);
                        poly.LineTo(buff.Width-1, ymax);
                        poly.LineTo(xdividemax, ymax);
                        poly.LineTo(xdividemax, ymin);

                        skg.DrawPath(poly, paint);
                    }

                    //right bounds
                    {
                        SKPath poly = new SKPath();

                        poly.MoveTo(xmin2_2, ymin2_2);
                        poly.LineTo(xmax2_2, ymin2_2);
                        poly.LineTo(xmax2_2, ymax2_2);
                        poly.LineTo(xmin2_2, ymax2_2);
                        poly.LineTo(xmin2_2, ymin2_2);

                        skg.DrawPath(poly, paint2);
                    }
                }
            }


            AfterSkDraw(ref skb, ref outbuffer);

            return outbuffer;
        }

        private void BeforeSkDraw(ref SKBitmap skb, ref SKCanvas skg, RawImgData baseimg, SKColor? color = null)
        {
            //check arguments
            if (color == null && baseimg.Data == null)
            {
                throw new ArgumentException("Either color or img data must be valid");
            }

            //get the right dimesions in the bitmap
            if (skb == null || skb.Width != baseimg.Width || skb.Height != baseimg.Height)
            {
                skb = new SKBitmap(baseimg.Width, baseimg.Height, SKColorType.Rgba8888, SKAlphaType.Unpremul);
                skg = new SKCanvas(skb);
            }
            
            //set the sk bitmap up with the right data
            if (color == null)
            {
                var ptraddr = skb.GetPixels();

                unsafe
                {
                    var dstptr = (byte*)ptraddr;
                    fixed (byte* srcptr = baseimg.Data)
                    {
                        var bytesize = baseimg.Data.Length;

                        Buffer.MemoryCopy(srcptr, dstptr, bytesize, bytesize);
                    }
                }
            }
            else
            {
                var ptraddr = skb.GetPixels();

                unsafe
                {
                    var dstptr = (uint*)ptraddr;

                    var c = color.Value;

                    uint colorint = (uint)((c.Red << (3 * 8)) + (c.Green << (2 * 8)) + (c.Blue << (1 * 8)) + c.Alpha);

                    var bytesize = skb.ByteCount;

                    for (var i = 0; i < bytesize / 4; i++)
                    {
                        *dstptr++ = 0xFFFFFFFF;
                    }
                }
            }
        }

        private void AfterSkDraw(ref SKBitmap skb, ref RawImgData img)
        {
            var spand = skb.GetPixelSpan();

            if (img == null || img.Width != skb.Width || img.Height != skb.Height)
            {
                var buff = spand.ToArray();
                img = new RawImgData(skb.Width, skb.Height, buff);
            }
            else
            {
                spand.CopyTo(img.Data);
            }
        }

        private RawImgData GetBackgroundWithOverlapsSK(ref SKBitmap skb, ref SKCanvas skg, ref RawImgData outbuffer, RawImgData baseimg, SKColor? basecolor)
        {
            BeforeSkDraw(ref skb, ref skg, baseimg, basecolor);
   
            //draw on the regions
            foreach (var entry in _regions)
            {
                SKPath poly = new SKPath();
                // overlaps are holes
                poly.FillType = SKPathFillType.EvenOdd;

                var points = entry.points;
                var edgetexdir = entry.edgedirs;

                var two = new Vector2(2, 2);

                for (var i = 0; i < points.Count; i++)
                {
                    var pt = points[i];
                    pt.X *= skb.Width;
                    pt.Y *= skb.Height;
                    if (edgetexdir[i] == two)
                    {
                        poly.MoveTo((float)pt.X, (float)pt.Y);
                    }
                    else
                    {
                        poly.LineTo((float)pt.X, (float)pt.Y);
                    }
                }

                SKPaint paint = new SKPaint()
                {
                    Style = SKPaintStyle.Fill,
                    Color = SKColors.Cyan
                };

                skg.DrawPath(poly, paint);
            }

            AfterSkDraw(ref skb, ref outbuffer);

            return outbuffer;
        }

        private void OnEarthLowResTextureChange(EarthView view)
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
                var tex = _mapsphere.CurrentTextureLowRes;

                var raw = tex.Buffer;

                var img1 = new SKBitmap(raw.Width, raw.Height, SKColorType.Rgba8888, SKAlphaType.Unpremul);

                SKUtils.SetBuffer(img1, raw.Data);
                var info = img1.Info;
                info.Width = (int)_overview.Size.X;
                info.Height = (int)_overview.Size.Y;
                img1 = img1.Resize(info, SKFilterQuality.High);

                var spand = img1.GetPixelSpan();

                var data = spand.ToArray();

                _baseoverviewbuffer = new RawImgData(img1.Width, img1.Height, data);
                _baseheatbuffer = new RawImgData(img1.Width, img1.Height, null);

                ApplyRegionsToFlatView();
                ApplyRegionsToHeatView();
            }

            if (_regionsdirty && _waitregions == 0.0)
            {

                _regionsdirty = false;
                _waitregions = REGIONS_WAIT_TIME;

                _regions = _mapsphere.InvisibleMapRegions; //always pull the fresh regions

                //update textures based on regions

                //draw the lines on the overview texture, if we want to show them. Drawing takes more cpu!
                ApplyRegionsToFlatView();
                ApplyRegionsToHeatView();
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

            if (_nextheattexture != null)
            {
                if (_nextheattexture != null && _nextheattexture.IsCompleted)
                {
                    var img = _nextheattexture.Result;

                    //assign to the overview texture
                    var tex = _overviewheat.Texture;
                    if (tex == null || tex.Width != img.Width || tex.Height != img.Height)
                    {
                        tex = new Texture2D(GoodOrBadGame.Active.GraphicsDevice, img.Width, img.Height);
                        _overviewheat.Texture = tex;
                    }

                    tex.SetData(img.Data);

                    _nextheattexture = null;
                }
            }
            else if (_nextheattexture_outofdate)
            {
                ApplyRegionsToHeatView();
            }

            if (_updatetexturesusingoverviewarea)
            {
                var img = UpdateTexturesUsingOverviewArea(ref _heatsk, ref _heatskGraphics2D, _heatbuffer, _heatbuffer, null);

                //assign to the overview texture
                var tex = _overviewheat.Texture;
                if (tex == null || tex.Width != img.Width || tex.Height != img.Height)
                {
                    tex = new Texture2D(GoodOrBadGame.Active.GraphicsDevice, img.Width, img.Height);
                    _overview.Texture = tex;
                }

                tex.SetData(img.Data);

                _updatetexturesusingoverviewarea = false;
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

                if (zoom != 0)
                {
                    _mapsphere.ZoomElevator(zoom);
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

                if (oldkeystate.IsKeyDown(Keys.F2) && keystate.IsKeyUp(Keys.F2))
                {
                    _overview.Visible = !_overview.Visible;
                    if (_overview.Visible)
                    {
                        _regionsdirty = true;
                    }

                    _overviewheat.Visible = !_overviewheat.Visible;
                    if (_overviewheat.Visible)
                    {
                        _regionsdirty = true;
                    }
                }

                if (oldkeystate.IsKeyDown(Keys.Space) && keystate.IsKeyUp(Keys.Space))
                {
                    _updatetexturesusingoverviewarea = true;
                }
            }
            base.DoBeforeUpdate();
        }
    }
}
