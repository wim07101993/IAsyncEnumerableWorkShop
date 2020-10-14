using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace WorkShop
{
    public class MeasurementEnumerable : IAsyncEnumerable<int>
    {
        private readonly Data _data;
        private readonly Task<(long start, long end)> _findPositionRangeTask;

        public MeasurementEnumerable(Data data, string name)
        {
            _data = data ?? throw new ArgumentNullException(nameof(data));
            Name = name ?? throw new ArgumentNullException(nameof(name));

            _findPositionRangeTask = Task.Run(FindPositionRange);
        }

        public string Name { get; }

        private object Lock => _data.Lock;

        public IAsyncEnumerator<int> GetAsyncEnumerator(CancellationToken cancellationToken = default)
            => new Enumerator(this, cancellationToken);

        private (long start, long end) FindPositionRange()
        {
            long start = -1;
            var nameCharIndex = 0;

            lock (Lock)
            {
                _ = _data.Stream.Seek(0, SeekOrigin.Begin);

                while (true)
                {
                    var b = _data.Stream.ReadByte();

                    if (start < 0)
                    {
                        if (b < 0)
                            return (-1, -1);

                        if (b == Name[nameCharIndex])
                        {
                            nameCharIndex++;
                            if (nameCharIndex == Name.Length)
                            {
                                // Temp=1:2;3:2;5:7; skip '=' symbol
                                start = _data.Stream.Position + 1;
                            }
                        }
                    }
                    else if (b is < 0 or '\r' or '\n')
                    {
                        var end = _data.Stream.Position - 1;
                        Console.WriteLine($"Start: {start}; End: {end}");
                        return (start, end);
                    }
                }
            }
        }

        private class Enumerator : IAsyncEnumerator<int>
        {
            private readonly MeasurementEnumerable _enumerable;
            private readonly CancellationToken _cancellationToken;

            private IEnumerator<int> _valueEnumerator;

            private long _position;

            public Enumerator(MeasurementEnumerable enumerable, CancellationToken cancellationToken)
            {
                _enumerable = enumerable;
                _cancellationToken = cancellationToken;
            }

            public int Current => _valueEnumerator?.Current ?? 0;

            private long Start => _enumerable._findPositionRangeTask.Result.start;
            private long End => _enumerable._findPositionRangeTask.Result.end;
            private Stream Stream => _enumerable._data.Stream;

            public async ValueTask<bool> MoveNextAsync()
            {
                if (_cancellationToken.IsCancellationRequested)
                    return false;

                if (_valueEnumerator?.MoveNext() == true)
                    return true;

                if (!_enumerable._findPositionRangeTask.IsCompleted)
                {
                    var (start, _) = await _enumerable._findPositionRangeTask;
                    _position = start;
                }

                // are we within the limits of the measurement
                if (Start < 0)
                    return false;
                if (_position > End)
                    return false;

                var value = 0;
                var count = 0;
                lock (_enumerable.Lock)
                {
                    // move to our position in the stream
                    if (Stream.Position != _position)
                        _ = Stream.Seek(_position, SeekOrigin.Begin);

                    // parse the value and the range 5:18;
                    var endOfValue = false;
                    while (!_cancellationToken.IsCancellationRequested && Stream.Position < End)
                    {
                        var b = Stream.ReadByte();
                        if (b == ';')
                            break;
                        if (b == ':')
                        {
                            endOfValue = true;
                            continue;
                        }

                        var n = b is > '/' and < ':'
                            ? (byte)'0'
                            : throw new Exception($"Unexpected character {(char)b}");

                        if (endOfValue)
                            count = (count * 10) + b - n;
                        else
                            value = (value * 10) + b - n;
                    }

                    _position = Stream.Position;
                }
                
                if (count == 0)
                    return false;

                _valueEnumerator = Enumerable.Repeat(value, count).GetEnumerator();
                return _valueEnumerator.MoveNext();
            }

            public ValueTask DisposeAsync() => ValueTask.CompletedTask;
        }
    }
}
