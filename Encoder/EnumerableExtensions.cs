using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Encoder
{
    public static class EnumerableExtensions
    {
        public static async IAsyncEnumerable<EncodedElement<T>> RunLengthEncode<T>(this IAsyncEnumerable<T> enumerable)
        {
            await using var enumerator = enumerable.GetAsyncEnumerator();

            if (!await enumerator.MoveNextAsync())
                yield break;

            var encoded = new EncodedElement<T> 
            {
                Value = enumerator.Current, 
                Length = 1
            };

            while (await enumerator.MoveNextAsync()) 
            {
                if (EqualityComparer<T>.Default.Equals(enumerator.Current, encoded.Value))
                    encoded.Length++;
                else
                {
                    yield return encoded;
                    encoded = new EncodedElement<T>
                    {
                        Value = enumerator.Current,
                        Length = 1
                    };
                }
            }

            yield return encoded;
        }
    }
}