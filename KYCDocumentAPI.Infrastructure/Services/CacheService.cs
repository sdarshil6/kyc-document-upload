using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace KYCDocumentAPI.Infrastructure.Services
{
    public class CacheService : ICacheService
    {
        private readonly IMemoryCache _memoryCache;
        private readonly ILogger<CacheService> _logger;
        private readonly HashSet<string> _cacheKeys = new();
        private readonly object _lockObject = new();

        public CacheService(IMemoryCache memoryCache, ILogger<CacheService> logger)
        {
            _memoryCache = memoryCache;
            _logger = logger;
        }

        public async Task<T?> GetAsync<T>(string key)
        {
            try
            {
                await Task.CompletedTask;

                if (_memoryCache.TryGetValue(key, out var cachedValue))
                {
                    _logger.LogDebug("Cache hit for key: {Key}", key);

                    if (cachedValue is string jsonString && typeof(T) != typeof(string))
                    {
                        return JsonSerializer.Deserialize<T>(jsonString);
                    }

                    return (T?)cachedValue;
                }

                _logger.LogDebug("Cache miss for key: {Key}", key);
                return default;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving cache key: {Key}", key);
                return default;
            }
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
        {
            try
            {
                await Task.CompletedTask;

                var options = new MemoryCacheEntryOptions();

                if (expiration.HasValue)
                {
                    options.SetAbsoluteExpiration(expiration.Value);
                }
                else
                {
                    // Default expiration times based on data type
                    if (key.Contains("user"))
                        options.SetAbsoluteExpiration(TimeSpan.FromMinutes(30));
                    else if (key.Contains("document"))
                        options.SetAbsoluteExpiration(TimeSpan.FromMinutes(15));
                    else if (key.Contains("analytics"))
                        options.SetAbsoluteExpiration(TimeSpan.FromMinutes(5));
                    else
                        options.SetAbsoluteExpiration(TimeSpan.FromMinutes(10));
                }

                // Add removal callback to track keys
                options.RegisterPostEvictionCallback((k, v, r, s) =>
                {
                    lock (_lockObject)
                    {
                        _cacheKeys.Remove(k.ToString() ?? "");
                    }
                    _logger.LogDebug("Cache key evicted: {Key}, Reason: {Reason}", k, r);
                });

                var cacheValue = typeof(T) == typeof(string) ? value as string : JsonSerializer.Serialize(value);

                _memoryCache.Set(key, cacheValue, options);

                lock (_lockObject)
                {
                    _cacheKeys.Add(key);
                }

                _logger.LogDebug("Cache set for key: {Key} with expiration: {Expiration}", key, expiration);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting cache key: {Key}", key);
            }
        }

        public async Task RemoveAsync(string key)
        {
            try
            {
                await Task.CompletedTask;

                _memoryCache.Remove(key);

                lock (_lockObject)
                {
                    _cacheKeys.Remove(key);
                }

                _logger.LogDebug("Cache key removed: {Key}", key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing cache key: {Key}", key);
            }
        }

        public async Task RemoveByPatternAsync(string pattern)
        {
            try
            {
                await Task.CompletedTask;

                List<string> keysToRemove;

                lock (_lockObject)
                {
                    keysToRemove = _cacheKeys.Where(k => k.Contains(pattern)).ToList();
                }

                foreach (var key in keysToRemove)
                {
                    await RemoveAsync(key);
                }

                _logger.LogDebug("Removed {Count} cache keys matching pattern: {Pattern}", keysToRemove.Count, pattern);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing cache keys by pattern: {Pattern}", pattern);
            }
        }

        public async Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null)
        {
            try
            {
                var cachedValue = await GetAsync<T>(key);
                if (cachedValue != null)
                {
                    return cachedValue;
                }

                var value = await factory();
                if (value != null)
                {
                    await SetAsync(key, value, expiration);
                }

                return value;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetOrSet for key: {Key}", key);
                return await factory(); // Fallback to factory method
            }
        }
    }
}
