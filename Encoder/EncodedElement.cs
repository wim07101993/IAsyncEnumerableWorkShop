namespace Encoder
{
    public struct EncodedElement<T>
    {
        public long Length { get; set; }
        public T Value { get; set; }

        public override string ToString() => $"{Value}:{Length}";
    }
}