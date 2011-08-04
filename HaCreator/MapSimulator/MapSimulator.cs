﻿//#define FULLSCREEN

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Input;
using System.IO;
using MapleLib.WzLib;
using MapleLib.WzLib.WzProperties;
using System.Collections;
using HaCreator.MapEditor;
using HaCreator.WzStructure;
using MapleLib.WzLib.WzStructure.Data;
using MapleLib.WzLib.WzStructure;
using HaRepackerLib;

namespace HaCreator.MapSimulator
{
    public partial class MapSimulator : Form, IServiceProvider
    {
        public static int mapShiftX = 0;
        public static int mapShiftY = 0;
        public static Point mapCenter;

        public const int width = 1024;
        public const int height = 768;

        private GraphicsDevice DxDevice;
        private SpriteBatch sprite;
        private PresentationParameters pParams = new PresentationParameters();
        private IGraphicsDeviceService graphicsDeviceService;
        private ContentManager contentMan;
        private SpriteFont defaultFont;
        public List<MapItem>[] mapObjects = CreateLayersArray();
        public List<BackgroundItem> backgrounds = new List<BackgroundItem>();
        public static Hashtable footholds;
        //public Character character;
        private Rectangle vr;
        private Texture2D minimap;
        //private bool debug = true;
        private Texture2D pixel;
        private WzMp3Streamer audio;
        private bool usePhysics = false;

        private static List<MapItem>[] CreateLayersArray()
        {
            List<MapItem>[] result = new List<MapItem>[8];
            for (int i = 0; i < 8; i++)
                result[i] = new List<MapItem>();
            return result;
        }

        public MapSimulator(Board mapBoard)
        {
            WzSoundProperty bgm = Program.InfoManager.BGMs[mapBoard.MapInfo.bgm];
            if (bgm != null)
            {
                audio = new WzMp3Streamer(bgm, true);
            }
            MapSimulator.mapCenter = mapBoard.CenterPoint;
            if (mapBoard.MapInfo.VR == null) vr = new Rectangle(0, 0, mapBoard.MapSize.X, mapBoard.MapSize.Y);
            else vr = new Rectangle(mapBoard.MapInfo.VR.Value.X + mapCenter.X, mapBoard.MapInfo.VR.Value.Y + mapCenter.Y, mapBoard.MapInfo.VR.Value.Width, mapBoard.MapInfo.VR.Value.Height);
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.Opaque, true);
            InitializeComponent();
            this.Width = width;
            this.Height = height;
#if FULLSCREEN
            pParams.BackBufferWidth = Math.Max(width, 1);
            pParams.BackBufferHeight = Math.Max(height, 1);
            pParams.BackBufferFormat = SurfaceFormat.Color;
            pParams.IsFullScreen = false;
            pParams.DepthStencilFormat = DepthFormat.Depth24;
#else
            pParams.BackBufferWidth = Math.Max(width, 1);
            pParams.BackBufferHeight = Math.Max(height, 1);
            pParams.BackBufferFormat = SurfaceFormat.Color;
            pParams.DepthStencilFormat = DepthFormat.Depth24;
            pParams.DeviceWindowHandle = Handle;
            pParams.IsFullScreen = false;
#endif
/*            try
            {
                DxDevice = new GraphicsDevice(GraphicsAdapter.DefaultAdapter, DeviceType.Hardware, Handle, pParams);
            }
            catch
            {
                DxDevice = new GraphicsDevice(GraphicsAdapter.DefaultAdapter, DeviceType.NullReference, Handle, pParams);
            }*/
            try
            {
                GraphicsProfile profile = GraphicsProfile.Reach;
                if (GraphicsAdapter.DefaultAdapter.IsProfileSupported(GraphicsProfile.HiDef))
                    profile = GraphicsProfile.HiDef;
                else if (!GraphicsAdapter.DefaultAdapter.IsProfileSupported(GraphicsProfile.Reach))
                    throw new NotSupportedException();
                DxDevice = new GraphicsDevice(GraphicsAdapter.DefaultAdapter, profile, pParams);
            }
            catch
            {
                HaRepackerLib.Warning.Error("Graphics adapter is not supported");
                Application.Exit();
            }
            graphicsDeviceService = new GraphicsDeviceService(DxDevice);
            this.minimap = BoardItem.TextureFromBitmap(DxDevice, mapBoard.MiniMap);
            System.Drawing.Bitmap bmp = new System.Drawing.Bitmap(1, 1);
            bmp.SetPixel(0, 0, System.Drawing.Color.White);
            pixel = BoardItem.TextureFromBitmap(DxDevice, bmp);

            //pixel = BoardItem.TextureFromBitmap(DxDevice, new System.Drawing.Bitmap(1, 1));
            contentMan = new ContentManager(this);
            defaultFont = contentMan.Load<SpriteFont>("Arial");
            sprite = new SpriteBatch(DxDevice);
            //character = new Character(400 + mapCenter.X, 300 + mapCenter.Y);
            //character.DoFly();
        }

        public new object GetService(Type serviceType)
        {
            if (serviceType == typeof(Microsoft.Xna.Framework.Graphics.IGraphicsDeviceService))
                return this.graphicsDeviceService;
            else
                return base.GetService(serviceType);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            //if (WindowState == FormWindowState.Minimized) { if (usePhysics) character.ProcessPhysics(); return; }
            base.OnPaint(e);
            DxDevice.Clear(ClearOptions.Target, Color.Black, 1.0f, 0); // Clear the window to black
            sprite.Begin(SpriteSortMode.Immediate, BlendState.NonPremultiplied);
            //sprite.Begin(SpriteBlendMode.AlphaBlend, SpriteSortMode.Immediate, SaveStateMode.None);
            foreach (BackgroundItem bg in backgrounds)
                if (!bg.Front)
                    bg.Draw(sprite);
            for (int i = 0; i < mapObjects.Length; i++)
            {
                foreach (MapItem item in mapObjects[i])
                    item.Draw(sprite);
                /*if (i == character.Layer)
                    character.Draw(sprite,pixel);*/
            }
            foreach (BackgroundItem bg in backgrounds)
                if (bg.Front)
                    bg.Draw(sprite);
            /*            if (debug)
                        {
                            foreach (DictionaryEntry fhEntry in footholds)
                            {
                                Foothold fh = (Foothold)fhEntry.Value;
                                int x1 = fh.x1 + mapCenter.X - mapShiftX - 3;
                                int x2 = fh.x2 + mapCenter.X - mapShiftX - 3;
                                int y1 = fh.y1 + mapCenter.Y - mapShiftY - 3;
                                int y2 = fh.y2 + mapCenter.Y - mapShiftY - 3;
                                FillRectangle(sprite, new Rectangle(x1, y1, 6, 6), Color.Red);
                                FillRectangle(sprite, new Rectangle(x2, y2, 6, 6), Color.Red);
                                DrawLine(sprite, new Vector2(x1, y1), new Vector2(x2, y2), Color.Red);
                                sprite.DrawString(defaultFont, fh.prev.ToString(), new Vector2(x1, y1 - 20), Color.Black);
                                sprite.DrawString(defaultFont, fh.next.ToString(), new Vector2(x2, y2 + 20), Color.Black);
                            }
                            sprite.DrawString(defaultFont, character.X.ToString(), new Vector2(), Color.Black);
                            sprite.DrawString(defaultFont, character.Y.ToString(), new Vector2(0,50), Color.Black);
                        }*/

            if (minimap != null)
            {
                sprite.Draw(minimap, new Rectangle(0, 0, minimap.Width, minimap.Height), Color.White);
                int minimapPosX = (mapShiftX + 400) / 16;
                int minimapPosY = (mapShiftY + 300) / 16;
                FillRectangle(sprite, new Rectangle(minimapPosX - 4, minimapPosY - 4, 4, 4), Color.Yellow);
            }
            sprite.End();
            try
            {
                DxDevice.Present();
            }
            catch (DeviceNotResetException)
            {
                try
                {
                    ResetDevice();
                }
                catch (DeviceLostException)
                {
                }
            }
            catch (DeviceLostException)
            {
            }
            HandleKeyPresses();
            //if (usePhysics) character.ProcessPhysics();
            System.Threading.Thread.Sleep(10);
            Invalidate();
        }

        private void ResetDevice()
        {
            pParams.BackBufferHeight = Height;
            pParams.BackBufferWidth = Width;
            pParams.BackBufferFormat = SurfaceFormat.Color;
            //pParams.EnableAutoDepthStencil = true;
            pParams.DepthStencilFormat = DepthFormat.Depth24;
            pParams.DeviceWindowHandle = Handle;
            //pParams.AutoDepthStencilFormat = DepthFormat.Depth24;
            DxDevice.Reset(DxDevice.PresentationParameters);
        }

        public void DrawLine(SpriteBatch sprite, Vector2 start, Vector2 end, Color color)
        {
            int width = (int)Vector2.Distance(start, end);
            float rotation = (float)Math.Atan2((double)(end.Y - start.Y), (double)(end.X - start.X));
            sprite.Draw(pixel, new Rectangle((int)start.X, (int)start.Y, width, UserSettings.LineWidth), null, color, rotation, new Vector2(0f, 0f), SpriteEffects.None, 1f);
        }

        public void FillRectangle(SpriteBatch sprite, Rectangle rectangle, Color color)
        {
            sprite.Draw(pixel, rectangle, color);
        }

        //int lastHotKeyPressTime = 0;

        void HandleKeyPresses()
        {
            if (usePhysics)
            {
            }
            KeyboardState state = Keyboard.GetState();
            /*            if (state[Microsoft.Xna.Framework.Input.Keys.F5] == KeyState.Down && Environment.TickCount - lastHotKeyPressTime > 500)
                        {
                            usePhysics = !usePhysics;
                            lastHotKeyPressTime = Environment.TickCount;
                            character.ResetLastProcessTime();
                        }*/
            /*if (!usePhysics)
            {*/
            if (state[Microsoft.Xna.Framework.Input.Keys.Left] == KeyState.Down)
                mapShiftX = Math.Max(vr.Left, mapShiftX - 10);
            if (state[Microsoft.Xna.Framework.Input.Keys.Up] == KeyState.Down)
                mapShiftY = Math.Max(vr.Top, mapShiftY - 10);
            if (state[Microsoft.Xna.Framework.Input.Keys.Right] == KeyState.Down)
                mapShiftX = Math.Min(vr.Right - width, mapShiftX + 10);
            if (state[Microsoft.Xna.Framework.Input.Keys.Down] == KeyState.Down)
                mapShiftY = Math.Min(vr.Bottom - height, mapShiftY + 10);
            if (state[Microsoft.Xna.Framework.Input.Keys.Escape] == KeyState.Down)
            {
                DxDevice.Dispose();
                Close();
            }
            /*character.X = mapShiftX + 400;
            character.Y = mapShiftY + 300;*/
            /*}
            else
            {
                mapShiftX = character.X - 400;
                mapShiftY = character.Y - 300;
                bool prone = true;
                if (state[Microsoft.Xna.Framework.Input.Keys.Left] == KeyState.Down)
                {
                    character.DoWalk(-1);
                    prone = false;
                }
                if (state[Microsoft.Xna.Framework.Input.Keys.Right] == KeyState.Down)
                {
                    character.DoWalk(1);
                    prone = false;
                }
                if (state[Microsoft.Xna.Framework.Input.Keys.Space] == KeyState.Down)
                {
                    character.DoJump();
                    prone = false;
                }
                if (state[Microsoft.Xna.Framework.Input.Keys.Escape] == KeyState.Down)
                    Close();
                if (prone) character.DoProne();
            }*/
        }

        private static MapItem CreateMapItemFromProperty(IWzImageProperty source, int x, int y, int mapCenterX, int mapCenterY, GraphicsDevice device, ref List<IWzObject> usedProps, bool flip)
        {
            source = WzInfoTools.GetRealProperty(source);
            if (source is WzSubProperty && ((WzSubProperty)source).WzProperties.Count == 1)
                source = ((WzSubProperty)source).WzProperties[0];
            if (source is WzCanvasProperty) //one-frame
            {
                WzVectorProperty origin = (WzVectorProperty)source["origin"];
                if (source.MSTag == null)
                {
                    source.MSTag = BoardItem.TextureFromBitmap(device, ((WzCanvasProperty)source).PngProperty.GetPNG(false));
                    usedProps.Add(source);
                }
                return new MapItem(new DXObject(x - origin.X.Value + mapCenterX, y - origin.Y.Value + mapCenterY, (Texture2D)source.MSTag), flip);
            }
            else if (source is WzSubProperty) //animooted
            {
                WzCanvasProperty frameProp;
                int i = 0;
                List<DXObject> frames = new List<DXObject>();
                while ((frameProp = (WzCanvasProperty)WzInfoTools.GetRealProperty(source[(i++).ToString()])) != null)
                {
                    int? delay = InfoTool.GetOptionalInt(frameProp["delay"]);
                    if (delay == null) delay = 100;
                    if (frameProp.MSTag == null)
                    {
                        frameProp.MSTag = BoardItem.TextureFromBitmap(device, frameProp.PngProperty.GetPNG(false));
                        usedProps.Add(frameProp);
                    }
                    WzVectorProperty origin = (WzVectorProperty)frameProp["origin"];
                    frames.Add(new DXObject(x - origin.X.Value + mapCenterX, y - origin.Y.Value + mapCenterY, (int)delay, (Texture2D)frameProp.MSTag));
                }
                return new MapItem(frames, flip);
            }
            else throw new Exception("unsupported property type in map simulator");
        }

        public static BackgroundItem CreateBackgroundFromProperty(IWzImageProperty source, int x, int y, int rx, int ry, int cx, int cy, int a, BackgroundType type, bool front, int mapCenterX, int mapCenterY, GraphicsDevice device, ref List<IWzObject> usedProps, bool flip)
        {
            source = WzInfoTools.GetRealProperty(source);
            if (source is WzSubProperty && ((WzSubProperty)source).WzProperties.Count == 1)
                source = ((WzSubProperty)source).WzProperties[0];
            if (source is WzCanvasProperty) //one-frame
            {
                WzVectorProperty origin = (WzVectorProperty)source["origin"];
                if (source.MSTag == null)
                {
                    source.MSTag = BoardItem.TextureFromBitmap(device, ((WzCanvasProperty)source).PngProperty.GetPNG(false));
                    usedProps.Add(source);
                }
                return new BackgroundItem(cx, cy, rx, ry, type, a, front, new DXObject(x - origin.X.Value/* - mapCenterX*/, y - origin.Y.Value/* - mapCenterY*/, (Texture2D)source.MSTag), flip);
            }
            else if (source is WzSubProperty) //animooted
            {
                WzCanvasProperty frameProp;
                int i = 0;
                List<DXObject> frames = new List<DXObject>();
                while ((frameProp = (WzCanvasProperty)WzInfoTools.GetRealProperty(source[(i++).ToString()])) != null)
                {
                    int? delay = InfoTool.GetOptionalInt(frameProp["delay"]);
                    if (delay == null) delay = 100;
                    if (frameProp.MSTag == null)
                    {
                        frameProp.MSTag = BoardItem.TextureFromBitmap(device, frameProp.PngProperty.GetPNG(false));
                        usedProps.Add(frameProp);
                    }
                    WzVectorProperty origin = (WzVectorProperty)frameProp["origin"];
                    frames.Add(new DXObject(x - origin.X.Value/* - mapCenterX*/, y - origin.Y.Value/* - mapCenterY*/, (int)delay, (Texture2D)frameProp.MSTag));
                }
                return new BackgroundItem(cx, cy, rx, ry, type, a, front, frames, flip);
            }
            else throw new Exception("unsupported property type in map simulator");
        }

        private static string DumpFhList(List<FootholdLine> fhs)
        {
            string res = "";
            foreach (FootholdLine fh in fhs)
                res += fh.FirstDot.X + "," + fh.FirstDot.Y + " : " + fh.SecondDot.X + "," + fh.SecondDot.Y + "\r\n";
            return res;
        }


        #region Warning - I am not responsible for death caused by excessive wtfing or a blown mind after reading this code
        public static Hashtable ConvertToMapleFootholds2(List<FootholdLine> footholds, List<FootholdAnchor> anchors)
        {
            Hashtable fhListByPoint = new Hashtable();
            foreach (FootholdAnchor anchor in anchors)
            {
                Point anchorPos = new Point(anchor.X, anchor.Y);
                List<FootholdLine> fhList = (List<FootholdLine>)fhListByPoint[anchorPos];
                if (fhList == null)
                {
                    fhList = new List<FootholdLine>(2);
                    fhListByPoint.Add(anchorPos, fhList);
                }
                fhList.AddRange(anchor.connectedLines.Cast<FootholdLine>());
            }
            Hashtable fhByNum = new Hashtable();
            for (int i = 1; i <= footholds.Count; i++)
            {
                FootholdLine fhClass = footholds[i - 1];
                if (fhClass.FirstDot.X == fhClass.SecondDot.X && fhClass.FirstDot.Y == fhClass.SecondDot.Y) continue;
                fhClass.num = i;
                Foothold mapleFh = new Foothold();
                mapleFh.num = i;
                mapleFh.layer = ((FootholdAnchor)fhClass.FirstDot).LayerNumber;
                fhByNum[i] = mapleFh;
            }
            for (int i = 1; i <= footholds.Count; i++)
            {
                FootholdLine fhClass = footholds[i - 1];
                Foothold mapleFh = (Foothold)fhByNum[i];
                FootholdLine firstOtherFh = GetOtherFh((FootholdAnchor)fhClass.FirstDot, fhClass, fhListByPoint);
                FootholdLine secondOtherFh = GetOtherFh((FootholdAnchor)fhClass.SecondDot, fhClass, fhListByPoint);
                if (fhClass.FirstDot.X < fhClass.SecondDot.X)
                {
                    mapleFh.x1 = fhClass.FirstDot.X;
                    mapleFh.x2 = fhClass.SecondDot.X;
                    mapleFh.y1 = fhClass.FirstDot.Y;
                    mapleFh.y2 = fhClass.SecondDot.Y;
                    mapleFh.prev = firstOtherFh == null ? 0 : firstOtherFh.num;
                    mapleFh.next = secondOtherFh == null ? 0 : secondOtherFh.num;
                }
                else if (fhClass.FirstDot.X > fhClass.SecondDot.X)
                {
                    mapleFh.x1 = fhClass.SecondDot.X;
                    mapleFh.x2 = fhClass.FirstDot.X;
                    mapleFh.y1 = fhClass.SecondDot.Y;
                    mapleFh.y2 = fhClass.FirstDot.Y;
                    mapleFh.prev = secondOtherFh == null ? 0 : secondOtherFh.num;
                    mapleFh.next = firstOtherFh == null ? 0 : firstOtherFh.num;
                }
                else
                {
                    bool fhDir = GetVerticalFootholdDirection(fhClass, fhListByPoint);
                    if (fhDir) //prev = firstdot
                    {
                        mapleFh.x1 = fhClass.FirstDot.X;
                        mapleFh.x2 = fhClass.SecondDot.X;
                        mapleFh.y1 = fhClass.FirstDot.Y;
                        mapleFh.y2 = fhClass.SecondDot.Y;
                        mapleFh.prev = firstOtherFh == null ? 0 : firstOtherFh.num;
                        mapleFh.next = secondOtherFh == null ? 0 : secondOtherFh.num;
                    }
                    else //prev = seconddot
                    {
                        mapleFh.x1 = fhClass.SecondDot.X;
                        mapleFh.x2 = fhClass.FirstDot.X;
                        mapleFh.y1 = fhClass.SecondDot.Y;
                        mapleFh.y2 = fhClass.FirstDot.Y;
                        mapleFh.prev = secondOtherFh == null ? 0 : secondOtherFh.num;
                        mapleFh.next = firstOtherFh == null ? 0 : firstOtherFh.num;
                    }
                }
                fhByNum[i] = mapleFh;
            }
            return fhByNum;
        }

        private static FootholdLine GetOtherFh(FootholdAnchor anchor, FootholdLine source, Hashtable fhListByPoint)
        {
            List<FootholdLine> connectedLines = (List<FootholdLine>)fhListByPoint[new Point(anchor.X, anchor.Y)];
            if (connectedLines.Count < 2) return null;
            else if (connectedLines.Count == 2)
            {
                return connectedLines[1].FhEquals(source) ? connectedLines[0] : connectedLines[1];
            }
            else //reaching this part means whoever made the map is a fucking idiot
            {
                FootholdLine longestFh = null;
                int longestFhLenth = 0;
                foreach (FootholdLine fh in connectedLines)
                {
                    int length = (int)Math.Sqrt(Math.Pow((fh.SecondDot.X - fh.FirstDot.X), 2) + Math.Pow((fh.SecondDot.Y - fh.FirstDot.Y), 2));
                    if (!fh.FhEquals(source) && length > longestFhLenth)
                    {
                        longestFh = fh;
                        longestFhLenth = length;
                    }
                }
                return longestFh;
            }
        }
        #endregion

        private static FootholdAnchor GetTopAnchor(FootholdLine fh)
        {
            return fh.FirstDot.Y > fh.SecondDot.Y ? (FootholdAnchor)fh.SecondDot : (FootholdAnchor)fh.FirstDot;
        }

        private static FootholdAnchor GetBottomAnchor(FootholdLine fh)
        {
            return fh.FirstDot.Y < fh.SecondDot.Y ? (FootholdAnchor)fh.SecondDot : (FootholdAnchor)fh.FirstDot;
        }

        //false = prev is in second point, true = prev is in first point
        private static bool GetVerticalFootholdDirection(FootholdLine fh, Hashtable fhListByPoint)
        {
            FootholdLine oldFh = null;
            FootholdLine currFh = fh;
            while (currFh.FirstDot.X == currFh.SecondDot.X)
            {
                oldFh = currFh;
                currFh = GetOtherFh(GetTopAnchor(currFh), currFh, fhListByPoint);
                if (currFh == null) goto try_other_side; //no more connections, try from other side
            }

            FootholdAnchor oldFhConnectAnchor = GetTopAnchor(oldFh);
            FootholdAnchor otherAnchor = oldFhConnectAnchor == currFh.FirstDot ? (FootholdAnchor)currFh.SecondDot : (FootholdAnchor)currFh.FirstDot;
            return !((otherAnchor.X > oldFhConnectAnchor.X) ^ (fh.FirstDot.Y > fh.SecondDot.Y));

        try_other_side:
            oldFh = null;
            currFh = fh;
            while (currFh.FirstDot.X == currFh.SecondDot.X)
            {
                oldFh = currFh;
                currFh = GetOtherFh(GetBottomAnchor(currFh), currFh, fhListByPoint);
                if (currFh == null) return false; //no more connections, return false (could be true too) because this foothold is 100% wall
            }

            oldFhConnectAnchor = GetBottomAnchor(oldFh);
            otherAnchor = oldFhConnectAnchor == currFh.FirstDot ? (FootholdAnchor)currFh.SecondDot : (FootholdAnchor)currFh.FirstDot;
            return !((otherAnchor.X > oldFhConnectAnchor.X) ^ (fh.FirstDot.Y < fh.SecondDot.Y));
        }

        public static MapSimulator CreateMapSimulator(Board mapBoard)
        {
            if (mapBoard.MiniMap == null) mapBoard.RegenerateMinimap();
            mapShiftX = 0;
            mapShiftY = 0;
            MapSimulator result = new MapSimulator(mapBoard);
            List<IWzObject> usedProps = new List<IWzObject>();
            WzFile MapFile = Program.WzManager["map"];
            WzDirectory tileDir = (WzDirectory)MapFile["Tile"];
            GraphicsDevice device = result.DxDevice;
            foreach (LayeredItem tileObj in mapBoard.BoardItems.TileObjs)
                result.mapObjects[tileObj.LayerNumber].Add(CreateMapItemFromProperty((IWzImageProperty)tileObj.BaseInfo.ParentObject, tileObj.X, tileObj.Y, mapBoard.CenterPoint.X, mapBoard.CenterPoint.Y, result.DxDevice, ref usedProps, tileObj is IFlippable ? ((IFlippable)tileObj).Flip : false));
            foreach (BackgroundInstance background in mapBoard.BoardItems.BackBackgrounds)
                result.backgrounds.Add(CreateBackgroundFromProperty((IWzImageProperty)background.BaseInfo.ParentObject, background.BaseX, background.BaseY, background.rx, background.ry, background.cx, background.cy, background.a, background.type, background.front, mapBoard.CenterPoint.X, mapBoard.CenterPoint.Y, result.DxDevice, ref usedProps, background.Flip));
            foreach (BackgroundInstance background in mapBoard.BoardItems.FrontBackgrounds)
                result.backgrounds.Add(CreateBackgroundFromProperty((IWzImageProperty)background.BaseInfo.ParentObject, background.BaseX, background.BaseY, background.rx, background.ry, background.cx, background.cy, background.a, background.type, background.front, mapBoard.CenterPoint.X, mapBoard.CenterPoint.Y, result.DxDevice, ref usedProps, background.Flip));
            foreach (IWzObject obj in usedProps) obj.MSTag = null;
            Hashtable fhs = ConvertToMapleFootholds2(mapBoard.BoardItems.FootholdLines, mapBoard.BoardItems.FHAnchors);
            MapSimulator.footholds = fhs;
            usedProps.Clear();
            GC.Collect();
            GC.WaitForPendingFinalizers();
            return result;
        }

        private void MapSimulator_Resize(object sender, EventArgs e)
        {
            if (DxDevice != null)
                ResetDevice();
        }

        private void MapSimulator_Load(object sender, EventArgs e)
        {
            if (audio != null) audio.Play();
        }

        private void MapSimulator_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (audio != null)
            {
                audio.Pause();
                audio.Dispose();
            }
        }
    }
}