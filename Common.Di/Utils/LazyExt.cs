using Microsoft.Extensions.Options;

namespace Common.Di.Utils;

public static class LazyExt
{
    extension<T>(Lazy<IOptions<T>> lazy) where T : class
    {
        public T Get()
        {
            return lazy.Value.Value;
        }
        
        public T? GetOrNull()
        {
            if (!lazy.IsValueCreated) return null;

            return lazy.Value.Value;
        }
    }
}