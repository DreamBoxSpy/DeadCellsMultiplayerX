using dc;
using dc.en;
using dc.h3d.mat;
using dc.haxe.io;
using dc.hxd;
using dc.hxd.res;
using dc.hxsl;
using dc.libs.heaps.slib;
using dc.pr;
using dc.tool;
using DeadCellsMultiplayerX.Server.Events;
using Hashlink;
using Hashlink.Marshaling;
using Hashlink.Proxy;
using Hashlink.Proxy.Clousre;
using Hashlink.Proxy.Objects;
using Hashlink.Reflection;
using Hashlink.Reflection.Types;
using HaxeProxy.Runtime;
using ModCore;
using ModCore.Events;
using ModCore.Events.Interfaces.Game;
using ModCore.Modules;
using ModCore.Utilities;
using StbImageSharp;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace DeadCellsMultiplayerX.Server
{
    internal class ServerMain : Module<ServerMain>,
        IOnBeforeGameInit
    {
        /// <summary>
        /// 服务器入口点
        /// 
        /// </summary>
        public static void Entrypoint()
        {
            _ = new ServerMain();

            // 启动游戏
            System.Type.GetType("ModCore.Startup, ModCore", true)!
                .GetMethod("StartGame")!
                .CreateDelegate<Func<int>>()
                ();
        }

        void IOnBeforeGameInit.OnBeforeGameInit()
        {
            Init();
        }

        public Thread? MainThread { get; private set; }

        public readonly string savePath = System.IO.Path.GetTempFileName();
        public readonly Dictionary<object, string> spriteLib2altas = [];

        private ServerSession? session;
        /// <summary>
        /// 初始化服务器
        /// </summary>
        internal void Init()
        {
            MainThread = Thread.CurrentThread;

            //隐藏服务器的窗口
            //Hook__Window.__constructor__ += Hook__Window___constructor__;
            Hook_Window.setFullScreen += Hook_Window_setFullScreen;

            //减少不必要的资源加载
            Hook_Loader.loadCache += Hook_Loader_loadCache;
            Hook_Texture.dispose += Hook_Texture_dispose;
            //Hook_Boot.render += Hook_Boot_render;
            //Hook_SpriteLib.getNormalMapFromGroup += Hook_SpriteLib_getNormalMapFromGroup;
            //Hook_SpriteLib.getNormalMapFromSprite += Hook_SpriteLib_getNormalMapFromSprite;

            dc.libs.heaps.slib.assets.Hook__Atlas.load += Hook__Atlas_load;
            Hook_Entity.setColorMap += Hook_Entity_setColorMap;
            Hook_Hero.initColorMap += Hook_Hero_initColorMap;

            //避免影响本地存档
            Hook__Save.fileName += Hook__Save_fileName;

            //避免影响本地配置
            var options = Options.Class.load();
            dc.tool.File.Class.PATH = System.IO.Path.GetDirectoryName(savePath)!.AsHaxeString();
            dc.Options.Class.FILE = System.IO.Path.GetFileName(System.IO.Path.GetTempFileName()).AsHaxeString();

            options.skipCinematics = true; //跳过动画
            options.disableLoreRooms = true; //关闭剧情房

            options.save();

            Hook_TitleScreen.initTitleScreen += Hook_TitleScreen_initTitleScreen;

            if (GameInfo.Platform == GameInfo.PlatformKind.Steam)
            {
                HashlinkHooks.Instance.CreateHook("tool.$File", "getSteamCloudStatus", Hook__File_getSteamCloudStatus);
                HashlinkHooks.Instance.CreateHook("tool.$File", "saveSteamCloudStatus", Hook__File_saveSteamCloudStatus);
            }

            Hook_Entity._isOnScreen += Hook_Entity__isOnScreen;
            // Hook_Entity.setGlowColor += Hook_Entity_setGlowColor;
            // Hook_Entity.setGlowData += Hook_Entity_setGlowData;
            Hook_Entity.dispose += Hook_Entity_dispose;

            //Hook_Game.init += Hook_Game_init;
        }

        private void Hook_Game_init(Hook_Game.orig_init orig, dc.pr.Game self)
        {
            orig(self);
            self.gameSignals.heroInitDone.add(new HlAction<Hero>((hero) => EventSystem.BroadcastEvent<IOnHeroInitDone, Hero>(hero)),
            null, null, null);
        }

        private void Hook_Entity_dispose(Hook_Entity.orig_dispose orig, Entity self)
        {
            orig(self);

            EventSystem.BroadcastEvent<IOnEntityDisposed, Entity>(self);
        }

        private void Hook__File_saveSteamCloudStatus(HashlinkClosure orig)
        {

        }

        private bool? Hook__File_getSteamCloudStatus(HashlinkClosure orig)
        {
            return null;
        }

        private void Hook_Entity_setGlowData(Hook_Entity.orig_setGlowData orig, Entity self, int index,
            Hashlink.Virtuals.virtual_animationIntensity_animationScale_animationSpeed_animationTextureMask_inner_key_outer_power_ glowData,
            HSprite sprite)
        {
            EventSystem.BroadcastEvent<IOnEntitySetGlowData, IOnEntitySetGlowData.Data>(new(self, index, glowData));

            orig(self, index, glowData, sprite);
        }

        private void Hook_Entity_setGlowColor(Hook_Entity.orig_setGlowColor orig, Entity self, int inner, int? outer, double? power, HSprite sprite)
        {
            self.setGlowData(0, new()
            {
                inner = inner,
                outer = outer,
                power = power
            }, sprite);
        }

        private bool Hook_Entity__isOnScreen(Hook_Entity.orig__isOnScreen orig, Entity self)
        {
            return true;
        }

        private void Hook_Hero_initColorMap(Hook_Hero.orig_initColorMap orig, Hero self)
        {
            orig(self);

            var inf = self.getSkinInfo();

            EventSystem.BroadcastEvent<IOnEntitySetColorMap, IOnEntitySetColorMap.Data>(new(self, inf.model.ToString(), inf.colorMap.ToString()));
        }

        private void Hook_Entity_setColorMap(Hook_Entity.orig_setColorMap orig, Entity self, dc.String model, dc.String skin, HSprite sspr)
        {
            orig(self, model, skin, sspr);

            EventSystem.BroadcastEvent<IOnEntitySetColorMap, IOnEntitySetColorMap.Data>(new(self, model.ToString(), skin.ToString()));
        }

        private SpriteLib Hook__Atlas_load(dc.libs.heaps.slib.assets.Hook__Atlas.orig_load orig, dc.String atlasPath,
            HaxeProxy.Runtime.HlAction onReload, dc.hl.types.ArrayObj notZeroBaseds, dc.hl.types.ArrayObj properties)
        {
            var lib = orig(atlasPath, onReload, notZeroBaseds, properties);
            spriteLib2altas[lib] = atlasPath.ToString();
            return lib;
        }

        private void Hook_TitleScreen_initTitleScreen(Hook_TitleScreen.orig_initTitleScreen orig, TitleScreen self,
            SpriteLib titleLib, HaxeProxy.Runtime.Ref<int> bgType)
        {
            orig(self, titleLib, bgType);

            if (session != null)
            {
                return;
            }

            session = new();
            _ = session.Init();
        }

        private void Hook_Boot_render(Hook_Boot.orig_render orig, Boot self, dc.h3d.Engine e)
        {

        }


        private dc.String Hook__Save_fileName(Hook__Save.orig_fileName orig, int? slot)
        {
            return System.IO.Path.GetFileName(savePath).AsHaxeString();
        }

        private void Hook_Window_setFullScreen(Hook_Window.orig_setFullScreen orig, Window self, bool v)
        {

        }

        private void Hook__Window___constructor__(Hook__Window.orig___constructor__ orig, Window arg1, dc.String title, int width, int height)
        {
            orig(arg1, title, 1, 1);

            SDL2.SDL.SDL_HideWindow(arg1.window.win);
        }

        private Texture Hook_SpriteLib_getNormalMapFromSprite(Hook_SpriteLib.orig_getNormalMapFromSprite orig, SpriteLib self, HSprite sprite)
        {
            return null!;
        }

        private Texture Hook_SpriteLib_getNormalMapFromGroup(Hook_SpriteLib.orig_getNormalMapFromGroup orig, SpriteLib self, dc.String groupName)
        {
            return null!;
        }

        private void Hook_Texture_dispose(Hook_Texture.orig_dispose orig, Texture self)
        {

        }

        private Image? img4096x4096;
        private Sound? soundEmpty;

        private Resource Hook_Loader_loadCache(Hook_Loader.orig_loadCache orig, Loader self, dc.String path, dc.hl.Class c)
        {
            var p = path.ToString();

            if (c == Image.Class)
            {
                //return img4096x4096 ??= new Image(self.load("atlas/beheaded0.png".AsHaxeString()).entry);
            }
            else if (c == Sound.Class)
            {
                return soundEmpty ??= new Sound(self.load("sfx/noSound_Error.wav".AsHaxeString()).entry);
            }

            return orig(self, path, c);

        }

    }
}
