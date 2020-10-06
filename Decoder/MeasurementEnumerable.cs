using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Decoder
{
    public class MeasurementEnumerable : IAsyncEnumerable<ushort>
    {
        private readonly Data _data;
        private readonly Task<(long start, long end)> _findPositionRangeTask;

        public MeasurementEnumerable(Data data, string name)
        {
            _data = data ?? throw new ArgumentNullException(nameof(data));
            Name = name ?? throw new ArgumentNullException(nameof(name));

            _findPositionRangeTask = Task.Run(FindPositionRangeAsync);
        }

        private object FileLock => _data.Lock;

        public string Name { get; }

        private long StartPosition => _findPositionRangeTask.Result.start;
        private long EndPosition => _findPositionRangeTask.Result.end;

        public IAsyncEnumerator<ushort> GetAsyncEnumerator(CancellationToken cancellationToken = default)
            => new Enumerator(this, cancellationToken);

        private (long sart, long end) FindPositionRangeAsync()
        {
            long start = -1;
            var nameCharIndex = 0;

            lock (FileLock)
            {
                if (_data.Stream.Position != 0)
                {
                    _ = _data.Stream.Seek(0, SeekOrigin.Begin);
                    if (Program.Verbose) Console.WriteLine($"Set pos: {0}");
                }
                

                while (true)
                {
                    var b = _data.Stream.ReadByte();
                    if (Program.Verbose) Console.Write($"R:{(char)b} ");

                    if (start < 0)
                    {
                        if (b < 0)
                            return (-1, -1);

                        if (b == Name[nameCharIndex])
                        {
                            nameCharIndex++;
                            if (nameCharIndex == Name.Length)
                            {
                                // add two because of '=' symbol
                                start = _data.Stream.Position + 1;
                                if (Program.Verbose) Console.Write($"Found start");
                            }
                        }
                    }
#if NET5
                    else if (b is < 0 or '\r' or '\n')
#else
                    else if (b < 0 || b < '\r' || b < '\n')
#endif
                    {
                        var end = _data.Stream.Position - 1;
                        if (Program.Verbose) Console.WriteLine();
                        Console.WriteLine($"Found start: {start}, end: {end}");
                        // if we reached end of file or end of line, that is the end of the measurements
                        return (start, end);
                    }
                }
            }
        }

        private (int value, bool endOfSeries) GetValueUntil(char until, CancellationToken cancelationToken = default)
        {
            var value = 0;
            while (true)
            {
                if (cancelationToken.IsCancellationRequested)
                    return (-1, default);

                // if end of series, return current value
                if (_data.Stream.Position >= EndPosition)
                    return (value, true);

                var b = _data.Stream.ReadByte();
                if (Program.Verbose) Console.Write($"R{(char)b} ");

                // if end of file, return current value
                if (b < 0)
                    return (-1, false);

                if (b == until)
                    return (value, false);

                // shift the read value to the left
                value *= 10;

#if NET5
                // use the byte values of ASCII to add to the value
                var num = b switch
                {
                    // numeric => 0-9 = ASCII 48-57
                    > '/' and < ':' => '0',
                    // A-F = ASCII 65-70 but -10 because it is hex
                    > '@' and < 'G' => 'A' - 10,
                    // a-f = ASCII 97-102 but -1 becuase it is hex
                    > '`' and < 'g' => 'a' - 10,
                    _ => throw new Exception($"Unexpected character {(char)b}"),
                };
#else
                int num;
                if (b > '/' && b < '>') 
                    num = '0'; // numeric => 0-9 = ASCII 48-57
                else if (b > '@' && b < 'G')
                    num = 'A' - 10;  // A-F = ASCII 65-70 but -10 because it is hex
                else if (b > '`' && b < 'g')
                    num = 'a' - 10;  // a-f = ASCII 97-102 but -1 becuase it is hex
                else
                    throw new Exception($"Unexpected character {(char)b}");
#endif
                value += b - num;
            }
        }

        private Task<(IEnumerable<ushort> values, long streamPosition)> GetNextValueRangeAsync(long searchPosition, CancellationToken cancelationToken = default)
            => Task.Run(() =>
        {
            var range = 0;
            var value = -1;

            lock (_data.Lock)
            {
                if (_data.Stream.Position != searchPosition) 
                {
                    _ = _data.Stream.Seek(searchPosition, SeekOrigin.Begin);
                    if (Program.Verbose) Console.WriteLine($"Set pos: {searchPosition}");
                }

                while (value < 0)
                {
                    if (cancelationToken.IsCancellationRequested)
                        return (default, _data.Stream.Position);

                    bool endOfSeries;
                    (value, endOfSeries) = GetValueUntil(':', cancelationToken);

                    if (endOfSeries)
                        break;

                    if (Program.Verbose) Console.Write($"P{value} ");

                    if (value < 0 )
                        throw new Exception($"Excpected a value at {_data.Stream.Position}");

                    (range, endOfSeries) = GetValueUntil(';', cancelationToken);
                    
                    if (range < 0)
                        throw new Exception($"Excpected a range at {_data.Stream.Position}");

                    if (endOfSeries)
                        break;
                
                    if (Program.Verbose) Console.Write($"P{range} ");
                }

                if (Program.Verbose) Console.Write($"pos: {_data.Stream.Position} ");
                return (Enumerable.Repeat((ushort)value, range), _data.Stream.Position);
            }
        });

        private class Enumerator : IAsyncEnumerator<ushort>
        {
            private readonly MeasurementEnumerable _enumerable;
            private readonly CancellationToken _cancellationToken;

            private long _position;
            private IEnumerator<ushort> _valueEnumerator;

            public Enumerator(MeasurementEnumerable enumerable, CancellationToken cancellationToken)
            {
                _enumerable = enumerable;
                _cancellationToken = cancellationToken;

                _position = default;
                _valueEnumerator = default;
            }

            public ushort Current => _valueEnumerator.Current;

            public async ValueTask<bool> MoveNextAsync()
            {
                if (_cancellationToken.IsCancellationRequested)
                    return false;

                if (_valueEnumerator?.MoveNext() == true)
                    return true;

                _valueEnumerator?.Dispose();
                if (Program.Verbose) Console.WriteLine();

                if (!_enumerable._findPositionRangeTask.IsCompleted)
                {
                    _ = await _enumerable._findPositionRangeTask;
                    _position = _enumerable.StartPosition;
                }

                if (_enumerable.StartPosition < 0)
                    return false;

                if (_position > _enumerable.EndPosition)
                    return false;

                IEnumerable<ushort> valueEnumerable;
                (valueEnumerable, _position) = await _enumerable.GetNextValueRangeAsync(_position, _cancellationToken);

                _valueEnumerator = valueEnumerable.GetEnumerator();
                return _valueEnumerator.MoveNext();
            }

            public ValueTask DisposeAsync() 
            {
#if NET5
                return ValueTask.CompletedTask;
#else
                return default;
#endif
            }
        }
    }
}
