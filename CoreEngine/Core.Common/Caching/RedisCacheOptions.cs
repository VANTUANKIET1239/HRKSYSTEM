namespace Core.Common.Caching
{
    public sealed class RedisCacheOptions
    {
        public const string SectionName = "Redis";
        public string ConnectionString { get; set; } = "localhost:6379,abortConnect=false";
        public string InstanceName { get; set; } = "hrk:";
        public int DefaultExpirationMinutes { get; set; } = 10;
    }
}
