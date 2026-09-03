namespace SwordsAndIdles.Combat
{
    // System.Random kullanmiyoruz: algoritmasi .NET surumleri arasinda degisebiliyor,
    // yani ayni tohum her platformda ayni diziyi vermiyor. Bu xorshift64* sabit.
    public sealed class DeterministicRandom
    {
        private const ulong FallbackSeed = 0x9E3779B97F4A7C15UL;
        private const ulong Multiplier = 2685821657736338717UL;

        private ulong _state;

        public DeterministicRandom(ulong seed)
        {
            _state = seed == 0UL ? FallbackSeed : seed;
        }

        public ulong State => _state;

        public static DeterministicRandom FromState(ulong state) => new DeterministicRandom(state);

        public uint NextUInt()
        {
            var x = _state;
            x ^= x >> 12;
            x ^= x << 25;
            x ^= x >> 27;
            _state = x;

            return (uint)((x * Multiplier) >> 32);
        }

        public int NextInt(int minInclusive, int maxExclusive)
        {
            if (maxExclusive <= minInclusive)
            {
                throw new System.ArgumentOutOfRangeException(
                    nameof(maxExclusive),
                    $"Üst sınır alt sınırdan büyük olmalı: [{minInclusive}, {maxExclusive}).");
            }

            var range = (uint)((long)maxExclusive - minInclusive);

            // modulo sapmasini kirp
            var threshold = (uint)((0x1_0000_0000UL - range) % range);

            uint draw;
            do
            {
                draw = NextUInt();
            }
            while (draw < threshold);

            return minInclusive + (int)(draw % range);
        }

        public int Roll(int sides) => NextInt(1, sides + 1);

        // Sans %0 ya da %100 olsa bile zar atilir; yoksa denge sayilari degisince
        // rastgele dizisi kayar ve kayitli dovusler tutmaz.
        public bool Chance(int percent) => Roll(100) <= percent;
    }
}
