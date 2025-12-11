using Polly;
using System;
using System.Collections.Generic;

namespace AIFORBI.Tools;
public static class PollySyncHelpers
{
    public static T ExecuteWithFallbackAltRetry<T>(
        Func<T> primary,
        Func<Exception?, T> alternative,
        int alternativeMaxRetries = 3,
        Func<int, TimeSpan>? sleepDurationProvider = null,
        Action<Exception>? onFallback = null,
        Action<Exception, TimeSpan, int>? onAlternativeRetry = null)
    {
        var retryOnAlternative = Policy
            .Handle<Exception>()
            .WaitAndRetry(
                retryCount: alternativeMaxRetries,
                sleepDurationProvider: attempt =>
                    sleepDurationProvider?.Invoke(attempt)
                    ?? TimeSpan.FromMilliseconds(200 * Math.Pow(2, attempt - 1)),
                onRetry: (ex, delay, attempt, ctx) =>
                {
                    ctx["lastError"] = ex; // bu denemenin hatası → sonraki deneme
                    onAlternativeRetry?.Invoke(ex, delay, attempt);
                });

        var fallback = Policy<T>
            .Handle<Exception>()
            .Fallback(
                fallbackAction: (ctx) =>
                {
                    // Retry’e de context veriyoruz; action Context parametresi almalı
                    return retryOnAlternative.Execute(
                        c =>
                        {
                            var prev = c.TryGetValue("lastError", out var obj) ? obj as Exception : null;
                            return alternative(prev);
                        },
                        ctx
                    );
                },
                onFallback: (outcome, ctx) =>
                {
                    if (outcome.Exception != null)
                    {
                        ctx["lastError"] = outcome.Exception; // ilk lastError = primary hatası
                        onFallback?.Invoke(outcome.Exception);
                    }
                });

        var context = new Context("primary", new Dictionary<string, object?>());
        return fallback.Execute(c => primary(), context);
    }

    public static void ExecuteWithFallbackAltRetry(
        Action primary,
        Action<Exception?> alternative,
        int alternativeMaxRetries = 3,
        Func<int, TimeSpan>? sleepDurationProvider = null,
        Action<Exception>? onFallback = null,
        Action<Exception, TimeSpan, int>? onAlternativeRetry = null)
    {
        var retryOnAlternative = Policy
            .Handle<Exception>()
            .WaitAndRetry(
                retryCount: alternativeMaxRetries,
                sleepDurationProvider: attempt =>
                    sleepDurationProvider?.Invoke(attempt)
                    ?? TimeSpan.FromMilliseconds(200 * Math.Pow(2, attempt - 1)),
                onRetry: (ex, delay, attempt, ctx) =>
                {
                    ctx["lastError"] = ex;
                    onAlternativeRetry?.Invoke(ex, delay, attempt);
                });

        var fallback = Policy
            .Handle<Exception>()
            .Fallback(
                fallbackAction: (ctx) =>
                {
                    retryOnAlternative.Execute(
                        c =>
                        {
                            var prev = c.TryGetValue("lastError", out var obj) ? obj as Exception : null;
                            alternative(prev);
                        },
                        ctx
                    );
                },
                onFallback: (ex, ctx) =>
                {
                    ctx["lastError"] = ex;
                    onFallback?.Invoke(ex);
                });

        var context = new Context("primary", new Dictionary<string, object?>());
        fallback.Execute(c => primary(), context);
    }
}
