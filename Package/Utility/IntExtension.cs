namespace com.Dunkingmachine.Utility
{
    internal static class IntExtension
    {
        public static int GetSignificantBits(this byte b)
        {
            if (b < 16)
            {
                if (b < 4)
                {

                    return b < 2 ? 1 : 2;
                }

                return b < 8 ? 3 : 4;
            }

            if (b < 64)
            {
                return b < 32 ? 5 : 6;
            }

            return b < 128 ? 7 : 8;
        }
    }
}
