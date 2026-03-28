using System.Collections.Generic;
using System.Linq;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Utility;

public static class EnumerableExtensions
{
    extension<T>(IEnumerable<T> enumerable)
    {
        public IEnumerable<TestCaseData> WithIgnores(IReadOnlyDictionary<T, string> ignores)
        {
            foreach (var entry in enumerable)
            {
                if (ignores.TryGetValue(entry, out var reason))
                    yield return new TestCaseData(entry).Ignore(reason);
                else
                    yield return new TestCaseData(entry);
            }
        }
    }

    extension(IEnumerable<string> enumerable)
    {
        public IEnumerable<TestCaseData> WithIgnores<T>(IReadOnlyDictionary<ProtoId<T>, string> ignores)
            where T : class, IPrototype
        {
            return enumerable
                .WithIgnores(ignores
                    .ToDictionary(
                        x => (string)x.Key,
                        x => x.Value
                        )
                );
        }
    }
}
