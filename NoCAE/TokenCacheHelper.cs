using System;
using System.IO;
using System.Security.Cryptography;
using Microsoft.Identity.Client;

namespace NoCAE
{
    static class TokenCacheHelper
    {
        /// <summary>
        /// Path to the token cache
        /// </summary>
        public static readonly string CacheFilePath = System.Reflection.Assembly.GetExecutingAssembly().Location + ".msalcache.bin3";

        private static readonly object FileLock = new object();

        /// <summary>
        /// Method to handle actions before accessing the token cache.
        /// </summary>
        /// <param name="args">Token cache notification arguments.</param>
        public static void BeforeAccessNotification(TokenCacheNotificationArgs args)
        {
            Console.WriteLine("BeforeAccessNotification: Attempting to access the token cache.");
            lock (FileLock)
            {
                try
                {
                    // Deserialize the token cache if the cache file exists, otherwise initialize with null
                    args.TokenCache.DeserializeMsalV3(File.Exists(CacheFilePath)
                        ? ProtectedData.Unprotect(File.ReadAllBytes(CacheFilePath), null, DataProtectionScope.CurrentUser)
                        : null);
                    Console.WriteLine("BeforeAccessNotification: Token cache deserialized successfully.");
                }
                catch (Exception ex)
                {
                    // Log the exception and delete the corrupted cache file
                    Console.WriteLine($"BeforeAccessNotification: Exception occurred - {ex.Message}. Deleting cache file.");
                    File.Delete(CacheFilePath);
                    args.TokenCache.DeserializeMsalV3(null);
                }
            }
        }

        /// <summary>
        /// Method to handle actions after accessing the token cache.
        /// </summary>
        /// <param name="args">Token cache notification arguments.</param>
        public static void AfterAccessNotification(TokenCacheNotificationArgs args)
        {
            Console.WriteLine("AfterAccessNotification: Checking if token cache state has changed.");
            // if the access operation resulted in a cache update
            if (args.HasStateChanged)
            {
                lock (FileLock)
                {
                    // Reflect changes in the persistent store
                    Console.WriteLine("AfterAccessNotification: State has changed. Updating the persistent store.");
                    File.WriteAllBytes(CacheFilePath,
                        ProtectedData.Protect(args.TokenCache.SerializeMsalV3(), null, DataProtectionScope.CurrentUser));
                    Console.WriteLine("AfterAccessNotification: Persistent store updated successfully.");
                }
            }
            else
            {
                Console.WriteLine("AfterAccessNotification: No changes detected in token cache state.");
            }
        }

        /// <summary>
        /// Enables serialization for the token cache by setting up before and after access notifications.
        /// </summary>
        /// <param name="tokenCache">The token cache to enable serialization for.</param>
        internal static void EnableSerialization(ITokenCache tokenCache)
        {
            Console.WriteLine("EnableSerialization: Setting up token cache serialization.");
            tokenCache.SetBeforeAccess(BeforeAccessNotification);
            tokenCache.SetAfterAccess(AfterAccessNotification);
            Console.WriteLine("EnableSerialization: Token cache serialization setup complete.");
        }
    }
}