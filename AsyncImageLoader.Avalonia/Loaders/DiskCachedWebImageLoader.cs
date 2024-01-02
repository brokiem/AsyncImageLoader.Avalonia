﻿using System;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;

namespace AsyncImageLoader.Loaders; 

/// <summary>
///     Provides memory and disk cached way to asynchronously load images for <see cref="ImageLoader" />
///     Can be used as base class if you want to create custom caching mechanism
/// </summary>
public class DiskCachedWebImageLoader : RamCachedWebImageLoader {
    private readonly string _cacheFolder;

    public DiskCachedWebImageLoader(string cacheFolder = "Cache/Images/") {
        _cacheFolder = cacheFolder;
    }

    public DiskCachedWebImageLoader(HttpClient httpClient, bool disposeHttpClient,
                                    string cacheFolder = "Cache/Images/")
        : base(httpClient, disposeHttpClient) {
        _cacheFolder = cacheFolder;
    }

    /// <inheritdoc />
    protected override Task<Bitmap?> LoadFromGlobalCache(string url) {
        var path = Path.Combine(_cacheFolder, CreateMd5(url));

        return File.Exists(path) ? Task.FromResult<Bitmap?>(new Bitmap(path)) : Task.FromResult<Bitmap?>(null);
    }
    
    protected override Task SaveToGlobalCache(string url, Stream stream) {
        var path = Path.Combine(_cacheFolder, CreateMd5(url));
        Directory.CreateDirectory(_cacheFolder);
        using(var outputFileStream = new FileStream(path, FileMode.Create)) {
            stream.CopyTo(outputFileStream);
        }
        return Task.CompletedTask;
    }

    protected static string CreateMd5(string input) {
        // Use input string to calculate MD5 hash
        using var md5 = MD5.Create();
        var inputBytes = Encoding.ASCII.GetBytes(input);
        var hashBytes = md5.ComputeHash(inputBytes);

        // Convert the byte array to hexadecimal string
        return BitConverter.ToString(hashBytes).Replace("-", "");
    }
}