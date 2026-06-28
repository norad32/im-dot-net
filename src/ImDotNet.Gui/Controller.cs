using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ImGuiNET;
using Silk.NET.Input;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;

namespace ImDotNet.Gui;

public sealed class Controller : IDisposable
{
    private readonly GL _gl;
    private readonly IWindow _window;
    private readonly IInputContext _input;

    private uint _vao, _vbo, _ebo;
    private uint _shader;
    private uint _fontTexture;
    private int _attribLocationTex, _attribLocationProjMtx;
    private int _attribLocationVtxPos, _attribLocationVtxUV, _attribLocationVtxColor;

    private bool _frameBegun;
    private bool _disposed;
    private int _windowWidth, _windowHeight;
    private readonly List<(IKeyboard kb, Action<IKeyboard, Key, int> down, Action<IKeyboard, Key, int> up, Action<IKeyboard, char> ch)> _kbHandlers = new();
    private readonly List<(IMouse mouse, Action<IMouse, ScrollWheel> scroll)> _mouseHandlers = new();

    public Controller(GL gl, IWindow window, IInputContext input)
    {
        _gl = gl;
        _window = window;
        _input = input;
        _windowWidth = window.Size.X;
        _windowHeight = window.Size.Y;

        ImGui.CreateContext();
        var io = ImGui.GetIO();
        io.ConfigFlags |= ImGuiConfigFlags.NavEnableKeyboard;

        SetStyle();
        CreateDeviceObjects();
        RegisterInputCallbacks();

        SetPerFrameData(1f / 60f);
        ImGui.NewFrame();
        _frameBegun = true;
    }

    public void WindowResized(int w, int h) { _windowWidth = w; _windowHeight = h; }

    public void Update(float dt)
    {
        if (_frameBegun) ImGui.Render();
        SetPerFrameData(dt);
        UpdateMousePos();
        ImGui.NewFrame();
        _frameBegun = true;
    }

    public void Render()
    {
        if (!_frameBegun) return;
        _frameBegun = false;
        ImGui.Render();
        RenderDrawData(ImGui.GetDrawData());
    }

    private void SetPerFrameData(float dt)
    {
        var io = ImGui.GetIO();
        io.DisplaySize = new Vector2(_windowWidth, _windowHeight);

        // Correct HiDPI framebuffer scale instead of hardcoding Vector2.One
        var fbSize = _window.FramebufferSize;
        io.DisplayFramebufferScale = _windowWidth > 0 && _windowHeight > 0
            ? new Vector2((float)fbSize.X / _windowWidth, (float)fbSize.Y / _windowHeight)
            : Vector2.One;

        io.DeltaTime = dt > 0 ? dt : 1f / 60f;
    }

    private void UpdateMousePos()
    {
        var io = ImGui.GetIO();

        // Only use the first mouse — multiple mice would overwrite each other anyway
        var mouse = _input.Mice.FirstOrDefault();
        if (mouse is null) return;

        io.MousePos = new Vector2(mouse.Position.X, mouse.Position.Y);
        io.MouseDown[0] = mouse.IsButtonPressed(MouseButton.Left);
        io.MouseDown[1] = mouse.IsButtonPressed(MouseButton.Right);
        io.MouseDown[2] = mouse.IsButtonPressed(MouseButton.Middle);
    }

    private void RegisterInputCallbacks()
    {
        foreach (var mouse in _input.Mice)
        {
            Action<IMouse, ScrollWheel> scrollHandler = (_, scroll) =>
            {
                var io = ImGui.GetIO();
                io.MouseWheelH += scroll.X;
                io.MouseWheel  += scroll.Y;
            };
            mouse.Scroll += scrollHandler;
            _mouseHandlers.Add((mouse, scrollHandler));
        }

        foreach (var kb in _input.Keyboards)
        {
            Action<IKeyboard, char> charHandler = (_, c) => ImGui.GetIO().AddInputCharacter(c);
            Action<IKeyboard, Key, int> downHandler = (_, key, _) => UpdateKey(key, true);
            Action<IKeyboard, Key, int> upHandler   = (_, key, _) => UpdateKey(key, false);

            kb.KeyChar += charHandler;
            kb.KeyDown += downHandler;
            kb.KeyUp   += upHandler;

            _kbHandlers.Add((kb, downHandler, upHandler, charHandler));
        }
    }

    private void UnregisterInputCallbacks()
    {
        foreach (var (mouse, scroll) in _mouseHandlers)
            mouse.Scroll -= scroll;

        foreach (var (kb, down, up, ch) in _kbHandlers)
        {
            kb.KeyDown  -= down;
            kb.KeyUp    -= up;
            kb.KeyChar  -= ch;
        }

        _mouseHandlers.Clear();
        _kbHandlers.Clear();
    }

    private static void UpdateKey(Key key, bool down)
    {
        var io = ImGui.GetIO();
        var imk = TranslateKey(key);
        if (imk != ImGuiKey.None) io.AddKeyEvent(imk, down);

        // Modifier events must be sent for both down AND up — no if (down) guard
        if (key is Key.ControlLeft  or Key.ControlRight) io.AddKeyEvent(ImGuiKey.ModCtrl,  down);
        if (key is Key.ShiftLeft    or Key.ShiftRight)   io.AddKeyEvent(ImGuiKey.ModShift, down);
        if (key is Key.AltLeft      or Key.AltRight)     io.AddKeyEvent(ImGuiKey.ModAlt,   down);
        if (key is Key.SuperLeft    or Key.SuperRight)   io.AddKeyEvent(ImGuiKey.ModSuper, down);
    }

    private static ImGuiKey TranslateKey(Key key) => key switch
    {
        Key.Tab           => ImGuiKey.Tab,
        Key.Left          => ImGuiKey.LeftArrow,
        Key.Right         => ImGuiKey.RightArrow,
        Key.Up            => ImGuiKey.UpArrow,
        Key.Down          => ImGuiKey.DownArrow,
        Key.PageUp        => ImGuiKey.PageUp,
        Key.PageDown      => ImGuiKey.PageDown,
        Key.Home          => ImGuiKey.Home,
        Key.End           => ImGuiKey.End,
        Key.Insert        => ImGuiKey.Insert,
        Key.Delete        => ImGuiKey.Delete,
        Key.Backspace     => ImGuiKey.Backspace,
        Key.Space         => ImGuiKey.Space,
        Key.Enter         => ImGuiKey.Enter,
        Key.Escape        => ImGuiKey.Escape,
        Key.Apostrophe    => ImGuiKey.Apostrophe,
        Key.Comma         => ImGuiKey.Comma,
        Key.Minus         => ImGuiKey.Minus,
        Key.Period        => ImGuiKey.Period,
        Key.Slash         => ImGuiKey.Slash,
        Key.Semicolon     => ImGuiKey.Semicolon,
        Key.Equal         => ImGuiKey.Equal,
        Key.LeftBracket   => ImGuiKey.LeftBracket,
        Key.BackSlash     => ImGuiKey.Backslash,
        Key.RightBracket  => ImGuiKey.RightBracket,
        Key.GraveAccent   => ImGuiKey.GraveAccent,
        Key.CapsLock      => ImGuiKey.CapsLock,
        Key.ScrollLock    => ImGuiKey.ScrollLock,
        Key.NumLock       => ImGuiKey.NumLock,
        Key.PrintScreen   => ImGuiKey.PrintScreen,
        Key.Pause         => ImGuiKey.Pause,
        Key.F1  => ImGuiKey.F1,  Key.F2  => ImGuiKey.F2,
        Key.F3  => ImGuiKey.F3,  Key.F4  => ImGuiKey.F4,
        Key.F5  => ImGuiKey.F5,  Key.F6  => ImGuiKey.F6,
        Key.F7  => ImGuiKey.F7,  Key.F8  => ImGuiKey.F8,
        Key.F9  => ImGuiKey.F9,  Key.F10 => ImGuiKey.F10,
        Key.F11 => ImGuiKey.F11, Key.F12 => ImGuiKey.F12,
        Key.A => ImGuiKey.A, Key.B => ImGuiKey.B, Key.C => ImGuiKey.C, Key.D => ImGuiKey.D,
        Key.E => ImGuiKey.E, Key.F => ImGuiKey.F, Key.G => ImGuiKey.G, Key.H => ImGuiKey.H,
        Key.I => ImGuiKey.I, Key.J => ImGuiKey.J, Key.K => ImGuiKey.K, Key.L => ImGuiKey.L,
        Key.M => ImGuiKey.M, Key.N => ImGuiKey.N, Key.O => ImGuiKey.O, Key.P => ImGuiKey.P,
        Key.Q => ImGuiKey.Q, Key.R => ImGuiKey.R, Key.S => ImGuiKey.S, Key.T => ImGuiKey.T,
        Key.U => ImGuiKey.U, Key.V => ImGuiKey.V, Key.W => ImGuiKey.W, Key.X => ImGuiKey.X,
        Key.Y => ImGuiKey.Y, Key.Z => ImGuiKey.Z,
        Key.Number0 => ImGuiKey._0, Key.Number1 => ImGuiKey._1,
        Key.Number2 => ImGuiKey._2, Key.Number3 => ImGuiKey._3,
        Key.Number4 => ImGuiKey._4, Key.Number5 => ImGuiKey._5,
        Key.Number6 => ImGuiKey._6, Key.Number7 => ImGuiKey._7,
        Key.Number8 => ImGuiKey._8, Key.Number9 => ImGuiKey._9,
        Key.Keypad0        => ImGuiKey.Keypad0,
        Key.Keypad1        => ImGuiKey.Keypad1,
        Key.Keypad2        => ImGuiKey.Keypad2,
        Key.Keypad3        => ImGuiKey.Keypad3,
        Key.Keypad4        => ImGuiKey.Keypad4,
        Key.Keypad5        => ImGuiKey.Keypad5,
        Key.Keypad6        => ImGuiKey.Keypad6,
        Key.Keypad7        => ImGuiKey.Keypad7,
        Key.Keypad8        => ImGuiKey.Keypad8,
        Key.Keypad9        => ImGuiKey.Keypad9,
        Key.KeypadDecimal  => ImGuiKey.KeypadDecimal,
        Key.KeypadDivide   => ImGuiKey.KeypadDivide,
        Key.KeypadMultiply => ImGuiKey.KeypadMultiply,
        Key.KeypadSubtract => ImGuiKey.KeypadSubtract,
        Key.KeypadAdd      => ImGuiKey.KeypadAdd,
        Key.KeypadEnter    => ImGuiKey.KeypadEnter,
        _                  => ImGuiKey.None,
    };

    private static void SetStyle()
    {
        ImGui.StyleColorsDark();
        var style = ImGui.GetStyle();
        style.WindowRounding    = 4f;
        style.FrameRounding     = 3f;
        style.ScrollbarRounding = 3f;
        style.GrabRounding      = 3f;
        style.TabRounding       = 3f;
    }

    private void CreateDeviceObjects()
    {
        const string vertSrc = """
            #version 330 core
            layout(location=0) in vec2 Position;
            layout(location=1) in vec2 UV;
            layout(location=2) in vec4 Color;
            uniform mat4 ProjMtx;
            out vec2 Frag_UV;
            out vec4 Frag_Color;
            void main()
            {
                Frag_UV = UV;
                Frag_Color = Color;
                gl_Position = ProjMtx * vec4(Position, 0, 1);
            }
            """;

        const string fragSrc = """
            #version 330 core
            in vec2 Frag_UV;
            in vec4 Frag_Color;
            uniform sampler2D Texture;
            out vec4 Out_Color;
            void main()
            {
                Out_Color = Frag_Color * texture(Texture, Frag_UV.st);
            }
            """;

        uint vert = CompileShader(ShaderType.VertexShader, vertSrc);
        uint frag = CompileShader(ShaderType.FragmentShader, fragSrc);
        _shader = _gl.CreateProgram();
        _gl.AttachShader(_shader, vert);
        _gl.AttachShader(_shader, frag);
        _gl.LinkProgram(_shader);
        _gl.GetProgram(_shader, ProgramPropertyARB.LinkStatus, out int linked);
        if (linked == 0) throw new Exception($"ImGui shader link failed: {_gl.GetProgramInfoLog(_shader)}");
        _gl.DetachShader(_shader, vert); _gl.DeleteShader(vert);
        _gl.DetachShader(_shader, frag); _gl.DeleteShader(frag);

        _attribLocationTex      = _gl.GetUniformLocation(_shader, "Texture");
        _attribLocationProjMtx  = _gl.GetUniformLocation(_shader, "ProjMtx");
        _attribLocationVtxPos   = _gl.GetAttribLocation(_shader, "Position");
        _attribLocationVtxUV    = _gl.GetAttribLocation(_shader, "UV");
        _attribLocationVtxColor = _gl.GetAttribLocation(_shader, "Color");

        _vao = _gl.GenVertexArray();
        _vbo = _gl.GenBuffer();
        _ebo = _gl.GenBuffer();

        var io = ImGui.GetIO();
        io.Fonts.GetTexDataAsRGBA32(out nint pixels, out int w, out int h, out _);
        _fontTexture = _gl.GenTexture();
        _gl.BindTexture(TextureTarget.Texture2D, _fontTexture);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)GLEnum.Linear);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)GLEnum.Linear);
        _gl.PixelStore(PixelStoreParameter.UnpackAlignment, 1);
        unsafe
        {
            _gl.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.Rgba, (uint)w, (uint)h, 0,
                           PixelFormat.Rgba, PixelType.UnsignedByte, (void*)pixels);
        }
        io.Fonts.SetTexID((nint)_fontTexture);
        io.Fonts.ClearTexData();
    }

    private uint CompileShader(ShaderType type, string src)
    {
        uint s = _gl.CreateShader(type);
        _gl.ShaderSource(s, src);
        _gl.CompileShader(s);
        _gl.GetShader(s, ShaderParameterName.CompileStatus, out int ok);
        if (ok == 0) throw new Exception($"Shader compile ({type}): {_gl.GetShaderInfoLog(s)}");
        return s;
    }

    private unsafe void RenderDrawData(ImDrawDataPtr drawData)
    {
        if (drawData.CmdListsCount == 0) return;

        int fbW = (int)(drawData.DisplaySize.X * drawData.FramebufferScale.X);
        int fbH = (int)(drawData.DisplaySize.Y * drawData.FramebufferScale.Y);
        if (fbW <= 0 || fbH <= 0) return;

        // Save GL state
        int[] lastViewport = new int[4];
        int[] lastScissor  = new int[4];
        fixed (int* p = lastViewport) _gl.GetInteger(GLEnum.Viewport,   p);
        fixed (int* p = lastScissor)  _gl.GetInteger(GLEnum.ScissorBox, p);

        _gl.GetInteger(GLEnum.CurrentProgram,     out int lastProgram);
        _gl.GetInteger(GLEnum.VertexArrayBinding, out int lastVao);
        _gl.GetInteger(GLEnum.ArrayBufferBinding, out int lastVbo);

        _gl.GetInteger(GLEnum.BlendEquationRgb,   out int lastBlendEqRgb);
        _gl.GetInteger(GLEnum.BlendEquationAlpha, out int lastBlendEqAlpha);
        _gl.GetInteger(GLEnum.BlendSrcRgb,        out int lastBlendSrcRgb);
        _gl.GetInteger(GLEnum.BlendDstRgb,        out int lastBlendDstRgb);
        _gl.GetInteger(GLEnum.BlendSrcAlpha,      out int lastBlendSrcAlpha);
        _gl.GetInteger(GLEnum.BlendDstAlpha,      out int lastBlendDstAlpha);

        bool lastBlend       = _gl.IsEnabled(EnableCap.Blend);
        bool lastCullFace    = _gl.IsEnabled(EnableCap.CullFace);
        bool lastDepthTest   = _gl.IsEnabled(EnableCap.DepthTest);
        bool lastScissorTest = _gl.IsEnabled(EnableCap.ScissorTest);

        // Setup render state
        _gl.Enable(EnableCap.Blend);
        _gl.BlendEquation(BlendEquationModeEXT.FuncAdd);
        _gl.BlendFuncSeparate(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha,
                              BlendingFactor.One, BlendingFactor.OneMinusSrcAlpha);
        _gl.Disable(EnableCap.CullFace);
        _gl.Disable(EnableCap.DepthTest);
        _gl.Enable(EnableCap.ScissorTest);
        _gl.Viewport(0, 0, (uint)fbW, (uint)fbH);

        float L = drawData.DisplayPos.X;
        float R = drawData.DisplayPos.X + drawData.DisplaySize.X;
        float T = drawData.DisplayPos.Y;
        float B = drawData.DisplayPos.Y + drawData.DisplaySize.Y;
        Matrix4x4 proj = new(
            2f/(R-L),       0,              0, 0,
            0,              2f/(T-B),       0, 0,
            0,              0,             -1, 0,
            (R+L)/(L-R),   (T+B)/(B-T),    0, 1);

        _gl.UseProgram(_shader);
        _gl.Uniform1(_attribLocationTex, 0);
        _gl.UniformMatrix4(_attribLocationProjMtx, 1, false, (float*)Unsafe.AsPointer(ref proj));

        _gl.BindVertexArray(_vao);
        _gl.BindBuffer(BufferTargetARB.ArrayBuffer, _vbo);
        _gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, _ebo);

        _gl.EnableVertexAttribArray((uint)_attribLocationVtxPos);
        _gl.EnableVertexAttribArray((uint)_attribLocationVtxUV);
        _gl.EnableVertexAttribArray((uint)_attribLocationVtxColor);

        int stride = sizeof(ImDrawVert);
        _gl.VertexAttribPointer((uint)_attribLocationVtxPos,   2, VertexAttribPointerType.Float,        false, (uint)stride, (void*)Marshal.OffsetOf<ImDrawVert>("pos"));
        _gl.VertexAttribPointer((uint)_attribLocationVtxUV,    2, VertexAttribPointerType.Float,        false, (uint)stride, (void*)Marshal.OffsetOf<ImDrawVert>("uv"));
        _gl.VertexAttribPointer((uint)_attribLocationVtxColor, 4, VertexAttribPointerType.UnsignedByte, true,  (uint)stride, (void*)Marshal.OffsetOf<ImDrawVert>("col"));

        var clipOff   = drawData.DisplayPos;
        var clipScale = drawData.FramebufferScale;

        for (int n = 0; n < drawData.CmdListsCount; n++)
        {
            var cmdList = drawData.CmdLists[n];
            _gl.BufferData(BufferTargetARB.ArrayBuffer,
                           (nuint)(cmdList.VtxBuffer.Size * stride),
                           (void*)cmdList.VtxBuffer.Data,
                           BufferUsageARB.StreamDraw);
            _gl.BufferData(BufferTargetARB.ElementArrayBuffer,
                           (nuint)(cmdList.IdxBuffer.Size * sizeof(ushort)),
                           (void*)cmdList.IdxBuffer.Data,
                           BufferUsageARB.StreamDraw);

            for (int ci = 0; ci < cmdList.CmdBuffer.Size; ci++)
            {
                var cmd = cmdList.CmdBuffer[ci];
                if (cmd.UserCallback != nint.Zero) continue;

                Vector4 clipRect = new(
                    (cmd.ClipRect.X - clipOff.X) * clipScale.X,
                    (cmd.ClipRect.Y - clipOff.Y) * clipScale.Y,
                    (cmd.ClipRect.Z - clipOff.X) * clipScale.X,
                    (cmd.ClipRect.W - clipOff.Y) * clipScale.Y);

                if (clipRect.X >= fbW || clipRect.Y >= fbH || clipRect.Z < 0 || clipRect.W < 0)
                    continue;

                // Clamp to avoid negative dimensions crashing some drivers
                _gl.Scissor(
                    (int)clipRect.X,
                    (int)(fbH - clipRect.W),
                    (uint)Math.Max(0f, clipRect.Z - clipRect.X),
                    (uint)Math.Max(0f, clipRect.W - clipRect.Y)
                );

                _gl.ActiveTexture(TextureUnit.Texture0);
                _gl.BindTexture(TextureTarget.Texture2D, (uint)cmd.TextureId);
                _gl.DrawElementsBaseVertex(PrimitiveType.Triangles, cmd.ElemCount,
                                           DrawElementsType.UnsignedShort,
                                           (void*)(cmd.IdxOffset * sizeof(ushort)),
                                           (int)cmd.VtxOffset);
            }
        }

        // Restore GL state
        _gl.Viewport(lastViewport[0], lastViewport[1], (uint)lastViewport[2], (uint)lastViewport[3]);
        _gl.Scissor(lastScissor[0],  lastScissor[1],  (uint)lastScissor[2],  (uint)lastScissor[3]);

        _gl.UseProgram((uint)lastProgram);
        _gl.BindVertexArray((uint)lastVao);
        _gl.BindBuffer(BufferTargetARB.ArrayBuffer, (uint)lastVbo);

        _gl.BlendEquationSeparate(
            (BlendEquationModeEXT)lastBlendEqRgb,
            (BlendEquationModeEXT)lastBlendEqAlpha);
        _gl.BlendFuncSeparate(
            (BlendingFactor)lastBlendSrcRgb,  (BlendingFactor)lastBlendDstRgb,
            (BlendingFactor)lastBlendSrcAlpha, (BlendingFactor)lastBlendDstAlpha);

        if (lastBlend)       _gl.Enable(EnableCap.Blend);       else _gl.Disable(EnableCap.Blend);
        if (lastCullFace)    _gl.Enable(EnableCap.CullFace);    else _gl.Disable(EnableCap.CullFace);
        if (lastDepthTest)   _gl.Enable(EnableCap.DepthTest);   else _gl.Disable(EnableCap.DepthTest);
        if (lastScissorTest) _gl.Enable(EnableCap.ScissorTest); else _gl.Disable(EnableCap.ScissorTest);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        UnregisterInputCallbacks();

        _gl.DeleteVertexArray(_vao);
        _gl.DeleteBuffer(_vbo);
        _gl.DeleteBuffer(_ebo);
        _gl.DeleteTexture(_fontTexture);
        _gl.DeleteProgram(_shader);

        ImGui.DestroyContext();
    }
}
