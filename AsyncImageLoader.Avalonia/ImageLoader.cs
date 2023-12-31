using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using AsyncImageLoader.Loaders;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace AsyncImageLoader; 

public static class ImageLoader
{
    public const string AsyncImageLoaderLogArea = "AsyncImageLoader";

    public static readonly AttachedProperty<int?> WidthProperty =
            AvaloniaProperty.RegisterAttached<Image, int?>("Width", typeof(ImageLoader));
    
    public static readonly AttachedProperty<int?> HeightProperty =
            AvaloniaProperty.RegisterAttached<Image, int?>("Height", typeof(ImageLoader));
    
    public static readonly AttachedProperty<string?> SourceProperty =
        AvaloniaProperty.RegisterAttached<Image, string?>("Source", typeof(ImageLoader));

    public static readonly AttachedProperty<bool> IsLoadingProperty =
        AvaloniaProperty.RegisterAttached<Image, bool>("IsLoading", typeof(ImageLoader));

    static ImageLoader()
    {
        SourceProperty.Changed.AddClassHandler<Image>(OnSourceChanged);
    }

    public static IAsyncImageLoader AsyncImageLoader { get; set; } = new RamCachedWebImageLoader();

	private static ConcurrentDictionary<Image, CancellationTokenSource> _pendingOperations = new ConcurrentDictionary<Image, CancellationTokenSource>();
	private static async void OnSourceChanged(Image sender, AvaloniaPropertyChangedEventArgs args) {
		var width = GetWidth(sender).GetValueOrDefault(-1);
		var height = GetHeight(sender).GetValueOrDefault(-1);
		var url = args.GetNewValue<string?>();
		
		// Cancel/Add new pending operation
		CancellationTokenSource? cts = _pendingOperations.AddOrUpdate(sender, new CancellationTokenSource(),
			(x, y) =>
			{
				y.Cancel();
				return new CancellationTokenSource();
			});

		if (url == null)
		{
			((ICollection<KeyValuePair<Image, CancellationTokenSource>>)_pendingOperations).Remove(new KeyValuePair<Image, CancellationTokenSource>(sender, cts));
			sender.Source = null;
			return;
		}

		SetIsLoading(sender, true);

		Bitmap? bitmap = await Task.Run(async () =>
		{
			try
			{
				// A small delay allows to cancel early if the image goes out of screen too fast (eg. scrolling)
				// The Bitmap constructor is expensive and cannot be cancelled
				await Task.Delay(10, cts.Token);

				return await AsyncImageLoader.ProvideImageAsync(url, width, height);
			}
			catch (TaskCanceledException)
			{
				return null;
			}
		});

		if (bitmap != null && !cts.Token.IsCancellationRequested)
			sender.Source = bitmap!;

		// "It is not guaranteed to be thread safe by ICollection, but ConcurrentDictionary's implementation is. Additionally, we recently exposed this API for .NET 5 as a public ConcurrentDictionary.TryRemove"
		((ICollection<KeyValuePair<Image, CancellationTokenSource>>)_pendingOperations).Remove(new KeyValuePair<Image, CancellationTokenSource>(sender, cts));
		SetIsLoading(sender, false);
	}
    
    public static int? GetWidth(Image element)
    {
        return element.GetValue(WidthProperty);
    }
    
    public static void SetWidth(Image element, int? value)
    {
        element.SetValue(WidthProperty, value);
    }
    
    public static int? GetHeight(Image element)
    {
        return element.GetValue(HeightProperty);
    }
    
    public static void SetHeight(Image element, int? value)
    {
        element.SetValue(HeightProperty, value);
    }

    public static string? GetSource(Image element)
    {
        return element.GetValue(SourceProperty);
    }

    public static void SetSource(Image element, string? value)
    {
        element.SetValue(SourceProperty, value);
    }

    public static bool GetIsLoading(Image element)
    {
        return element.GetValue(IsLoadingProperty);
    }

    private static void SetIsLoading(Image element, bool value)
    {
        element.SetValue(IsLoadingProperty, value);
    }
}