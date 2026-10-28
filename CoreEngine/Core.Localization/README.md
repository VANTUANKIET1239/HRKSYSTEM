# Core.Localization

Register `AddHrkLocalization(configuration, "game")` after `AddHrkInternalApi`.
Configure `InternalApi:Services:language:BaseAddress`, and the shared
`Localization:RedisConnectionString` / `Localization:RedisKeyPrefix` on all hosts.
Keep the key prefix environment-specific and identical across hosts. This cache
does not use each application's Redis InstanceName.

`ITextLocalizer.GetAsync(group, culture, key, fallback, arguments, ct)` reads the
current published snapshot from Redis and uses the internal API on a miss.
Only the language service writes Redis. Failures preserve fallback messages;
request cancellation is propagated. The localizer is scoped and memoizes only
within a request, with no persistent memory cache or broadcast requirement.

Placeholder syntax is `{{name}}`. Published revisions are immutable in SQL.
Redis holds only one hash per group/culture, for example
`hrk:dev:i18n:{game:vi-VN}:bundle`, with `revision` (decimal string) and `payload`
(serialized TranslationBundle) fields. A read fetches the payload in one command.
Publish checks the revision and replaces both fields and the TTL atomically;
an older revision changes neither the bundle nor its TTL. Decimal-string
comparison preserves the full SQL BIGINT range. The Lua implementation is in
`RedisTranslationScripts`; callers continue to use `PublishAsync(bundle)`.
`ITranslationCache.PublishAsync` deliberately throws on failure for outbox retry.

`AddHrkLocalization` registers `Core.Caching`'s singleton
`IRedisConnectionProvider` through `AddHrkRedisConnections`. The provider lazily
creates and reuses one owned connection per configuration string, and releases
connections on application shutdown. Cancelling a caller only cancels its wait;
other callers can keep using the pending/shared connection. The translation
cache receives this provider through DI and does not manage connection lifetime.

Deploy updated language writers/readers together. The former `:current` and
`:revision:<id>` keys are no longer read or written and expire with their existing
TTL. The new `:bundle` key is populated by the outbox or by the language API's
database fallback. No SQL migration or seed rerun is required for this change.
