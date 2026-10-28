namespace Core.Localization;

internal static class RedisTranslationScripts
{
    // One hash holds only the current revision and payload. Never convert SQL BIGINT to a Lua number.
    // Check and write happen atomically; a stale publisher changes neither the data nor its TTL.
    public const string PublishCurrentIfNewer = """
        local previous = redis.call('HGET', KEYS[1], 'revision')
        local incoming = ARGV[1]
        if previous and (#incoming < #previous or (#incoming == #previous and incoming < previous)) then
            return 0
        end
        redis.call('HSET', KEYS[1], 'revision', incoming, 'payload', ARGV[2])
        redis.call('EXPIRE', KEYS[1], ARGV[3])
        return 1
        """;
}
