﻿using System;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;

namespace AsyncImageLoader; 

public interface IAsyncImageLoader : IDisposable
{
    /// <summary>
    ///     Loads image
    /// </summary>
    /// <param name="url">Target url</param>
    /// <returns>Bitmap</returns>
    public Task<Bitmap?> ProvideImageAsync(string url, int width, int height);
}