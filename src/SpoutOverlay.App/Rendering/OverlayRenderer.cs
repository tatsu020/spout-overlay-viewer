using SpoutOverlay.App.Models;
using Vortice.Direct2D1;
using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.DirectComposition;
using Vortice.DCommon;
using Vortice.DXGI;
using Vortice.Mathematics;
using static Vortice.Direct2D1.D2D1;
using static Vortice.Direct3D11.D3D11;
using static Vortice.DirectComposition.DComp;
using static Vortice.DXGI.DXGI;
using SpoutOverlay.App.Helpers;

namespace SpoutOverlay.App.Rendering;

public sealed class OverlayRenderer : IDisposable
{
    private readonly IDXGIFactory2 _dxgiFactory;
    private readonly ID3D11Device _d3dDevice;
    private readonly ID3D11DeviceContext _d3dContext;
    private readonly IDXGIDevice _dxgiDevice;
    private readonly ID2D1Factory1 _d2dFactory;
    private readonly ID2D1Device _d2dDevice;
    private readonly ID2D1DeviceContext _d2dContext;
    private readonly IDCompositionDevice _compositionDevice;
    private readonly IDCompositionTarget _compositionTarget;
    private readonly IDCompositionVisual _visual;
    private readonly ID2D1SolidColorBrush _frameBrush;
    private readonly ID2D1SolidColorBrush _placeholderBrush;
    private readonly ID2D1SolidColorBrush _placeholderAccentBrush;
    private readonly ID2D1SolidColorBrush _sliderTrackBrush;
    private readonly ID2D1SolidColorBrush _sliderFillBrush;
    private readonly ID2D1SolidColorBrush _sliderThumbBrush;

    private IDXGISwapChain1? _swapChain;
    private ID2D1Bitmap1? _targetBitmap;
    private ID3D11Texture2D? _spoutTexture;
    private ID2D1Bitmap1? _sourceBitmap;
    private nint _sourceTexturePointer;
    private uint _sourceWidth;
    private uint _sourceHeight;
    private Format _sourceFormat;
    private System.Drawing.Size _currentSize;

    public OverlayRenderer(nint windowHandle, System.Drawing.Size initialSize)
    {
        var featureLevels = new[] { Vortice.Direct3D.FeatureLevel.Level_11_1, Vortice.Direct3D.FeatureLevel.Level_11_0 };
        D3D11CreateDevice(
            nint.Zero,
            DriverType.Hardware,
            DeviceCreationFlags.BgraSupport,
            featureLevels,
            out _d3dDevice,
            out _d3dContext).CheckError();

        _dxgiDevice = _d3dDevice.QueryInterface<IDXGIDevice>();
        _dxgiFactory = CreateDXGIFactory2<IDXGIFactory2>(false);
        _d2dFactory = D2D1CreateFactory<ID2D1Factory1>();
        _d2dDevice = _d2dFactory.CreateDevice(_dxgiDevice);
        _d2dContext = _d2dDevice.CreateDeviceContext(DeviceContextOptions.None);

        _compositionDevice = DCompositionCreateDevice<IDCompositionDevice>(_dxgiDevice);
        _compositionDevice.CreateTargetForHwnd(windowHandle, true, out _compositionTarget).CheckError();
        _compositionDevice.CreateVisual(out _visual).CheckError();
        _compositionTarget.SetRoot(_visual);

        _frameBrush = _d2dContext.CreateSolidColorBrush(new Color4(0.12f, 0.76f, 0.98f, 0.9f));
        _placeholderBrush = _d2dContext.CreateSolidColorBrush(new Color4(0.05f, 0.05f, 0.05f, 0.15f));
        _placeholderAccentBrush = _d2dContext.CreateSolidColorBrush(new Color4(0.90f, 0.90f, 0.90f, 0.45f));
        _sliderTrackBrush = _d2dContext.CreateSolidColorBrush(new Color4(0.06f, 0.06f, 0.06f, 0.65f));
        _sliderFillBrush = _d2dContext.CreateSolidColorBrush(new Color4(0.12f, 0.76f, 0.98f, 0.95f));
        _sliderThumbBrush = _d2dContext.CreateSolidColorBrush(new Color4(0.96f, 0.98f, 1.0f, 0.98f));

        Resize(initialSize);
    }

    public nint D3D11DevicePointer => _d3dDevice.NativePointer;

    public void Resize(System.Drawing.Size newSize)
    {
        if (newSize.Width <= 0 || newSize.Height <= 0)
        {
            return;
        }

        if (_swapChain is null)
        {
            CreateSwapChain(newSize);
            _currentSize = newSize;
            return;
        }

        if (_currentSize == newSize)
        {
            return;
        }

        _targetBitmap?.Dispose();
        _d2dContext.Target = null;
        _swapChain.ResizeBuffers(2u, (uint)newSize.Width, (uint)newSize.Height, Format.B8G8R8A8_UNorm, SwapChainFlags.None).CheckError();
        _currentSize = newSize;
        CreateTargetBitmap();
    }

    public void Render(SpoutFrameResult? frame, OverlayMode mode, bool waitingForSender, float overlayOpacity)
    {
        if (_swapChain is null || _targetBitmap is null)
        {
            return;
        }

        _d2dContext.BeginDraw();
        _d2dContext.Clear(new Color4(0f, 0f, 0f, 0f));

        if (frame is not null)
        {
            try
            {
                EnsureSourceResources(frame);
                DrawSpoutFrame(frame, overlayOpacity);
            }
            catch (Exception)
            {
                DrawPlaceholder(true);
            }
        }
        else if (mode == OverlayMode.Adjust)
        {
            DrawPlaceholder(waitingForSender);
        }

        if (mode == OverlayMode.Adjust)
        {
            DrawAdjustFrame();
            DrawOpacitySlider(overlayOpacity);
        }

        _d2dContext.EndDraw();
        _swapChain.Present(1, PresentFlags.None);
        _compositionDevice.Commit();
    }

    private void CreateSwapChain(System.Drawing.Size size)
    {
        var description = new SwapChainDescription1
        {
            Width = (uint)size.Width,
            Height = (uint)size.Height,
            Format = Format.B8G8R8A8_UNorm,
            Stereo = false,
            SampleDescription = new SampleDescription(1, 0),
            BufferUsage = Usage.RenderTargetOutput,
            BufferCount = 2,
            Scaling = Scaling.Stretch,
            SwapEffect = SwapEffect.FlipSequential,
            AlphaMode = Vortice.DXGI.AlphaMode.Premultiplied
        };

        _swapChain = _dxgiFactory.CreateSwapChainForComposition(_d3dDevice, description);
        _visual.SetContent(_swapChain);
        _currentSize = size;
        CreateTargetBitmap();
        _compositionDevice.Commit();
    }

    private void CreateTargetBitmap()
    {
        using var backBuffer = _swapChain!.GetBuffer<IDXGISurface>(0);
        var properties = new BitmapProperties1(
            new PixelFormat(Format.B8G8R8A8_UNorm, Vortice.DCommon.AlphaMode.Premultiplied),
            96,
            96,
            BitmapOptions.Target | BitmapOptions.CannotDraw);
        _targetBitmap = _d2dContext.CreateBitmapFromDxgiSurface(backBuffer, properties);
        _d2dContext.Target = _targetBitmap;
    }

    private void DrawSpoutFrame(SpoutFrameResult frame, float overlayOpacity)
    {
        _d2dContext.DrawBitmap(
            _sourceBitmap!,
            new Vortice.RawRectF(0, 0, _currentSize.Width, _currentSize.Height),
            OverlaySettings.ClampOverlayOpacity(overlayOpacity),
            Vortice.Direct2D1.BitmapInterpolationMode.Linear,
            new Vortice.RawRectF(0, 0, frame.Sender.Width, frame.Sender.Height));
    }

    private void EnsureSourceResources(SpoutFrameResult frame)
    {
        var sender = frame.Sender;
        var format = (Format)sender.Format;
        if (_spoutTexture is not null &&
            _sourceBitmap is not null &&
            _sourceTexturePointer == frame.TexturePointer &&
            _sourceWidth == sender.Width &&
            _sourceHeight == sender.Height &&
            _sourceFormat == format)
        {
            return;
        }

        _sourceBitmap?.Dispose();
        _spoutTexture?.Dispose();

        System.Runtime.InteropServices.Marshal.AddRef(frame.TexturePointer);
        _spoutTexture = new ID3D11Texture2D(frame.TexturePointer);
        using var sourceSurface = _spoutTexture.QueryInterface<IDXGISurface>();
        _sourceBitmap = _d2dContext.CreateBitmapFromDxgiSurface(
            sourceSurface,
            new BitmapProperties1(
                new PixelFormat(format, Vortice.DCommon.AlphaMode.Premultiplied)));

        _sourceTexturePointer = frame.TexturePointer;
        _sourceWidth = sender.Width;
        _sourceHeight = sender.Height;
        _sourceFormat = format;
    }

    private void DrawPlaceholder(bool waitingForSender)
    {
        var width = _currentSize.Width;
        var height = _currentSize.Height;
        _d2dContext.FillRectangle(new Vortice.RawRectF(0, 0, width, height), _placeholderBrush);
        _d2dContext.DrawLine(new System.Numerics.Vector2(0, 0), new System.Numerics.Vector2(width, height), _placeholderAccentBrush, 2f);
        _d2dContext.DrawLine(new System.Numerics.Vector2(width, 0), new System.Numerics.Vector2(0, height), _placeholderAccentBrush, 2f);

        if (waitingForSender)
        {
            _d2dContext.DrawRectangle(new Vortice.RawRectF(6, 6, width - 6, height - 6), _placeholderAccentBrush, 2f);
        }
    }

    private void DrawAdjustFrame()
    {
        var width = _currentSize.Width;
        var height = _currentSize.Height;
        const float handleSize = 10f;
        _d2dContext.DrawRectangle(new Vortice.RawRectF(1, 1, width - 1, height - 1), _frameBrush, 2f);

        DrawHandle(0, 0);
        DrawHandle(width - handleSize, 0);
        DrawHandle(0, height - handleSize);
        DrawHandle(width - handleSize, height - handleSize);
        DrawHandle((width - handleSize) / 2f, 0);
        DrawHandle((width - handleSize) / 2f, height - handleSize);
        DrawHandle(0, (height - handleSize) / 2f);
        DrawHandle(width - handleSize, (height - handleSize) / 2f);

        void DrawHandle(float x, float y)
        {
            _d2dContext.FillRectangle(new Vortice.RawRectF(x, y, x + handleSize, y + handleSize), _frameBrush);
        }
    }

    private void DrawOpacitySlider(float overlayOpacity)
    {
        var track = OverlayOpacitySliderLayout.GetTrackBounds(_currentSize);
        var thumbCenter = OverlayOpacitySliderLayout.GetThumbCenter(_currentSize, overlayOpacity);
        var fillWidth = Math.Max(0f, thumbCenter.X - track.Left);

        _d2dContext.FillRectangle(new Vortice.RawRectF(track.Left, track.Top, track.Right, track.Bottom), _sliderTrackBrush);
        if (fillWidth > 0f)
        {
            _d2dContext.FillRectangle(new Vortice.RawRectF(track.Left, track.Top, track.Left + fillWidth, track.Bottom), _sliderFillBrush);
        }

        _d2dContext.FillEllipse(new Ellipse(new System.Numerics.Vector2(thumbCenter.X, thumbCenter.Y), 9f, 9f), _sliderThumbBrush);
        _d2dContext.DrawEllipse(new Ellipse(new System.Numerics.Vector2(thumbCenter.X, thumbCenter.Y), 9f, 9f), _frameBrush, 1.5f);
    }

    public void Dispose()
    {
        _targetBitmap?.Dispose();
        _sourceBitmap?.Dispose();
        _spoutTexture?.Dispose();
        _swapChain?.Dispose();
        _frameBrush.Dispose();
        _placeholderBrush.Dispose();
        _placeholderAccentBrush.Dispose();
        _sliderTrackBrush.Dispose();
        _sliderFillBrush.Dispose();
        _sliderThumbBrush.Dispose();
        _visual.Dispose();
        _compositionTarget.Dispose();
        _compositionDevice.Dispose();
        _d2dContext.Dispose();
        _d2dDevice.Dispose();
        _d2dFactory.Dispose();
        _dxgiDevice.Dispose();
        _d3dContext.Dispose();
        _d3dDevice.Dispose();
        _dxgiFactory.Dispose();
    }
}
