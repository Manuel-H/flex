namespace com.Dunkingmachine.Utility
{
    public static class IntExtension
    {
        public static int GetSignificantBits(this byte b)
        {
            if (b < 16)
            {
                if (b < 4)
                {

                    return b < 2 ? 1 : 2;
                }
                else
                {
                    return b < 8 ? 3 : 4;
                }
            }
            else
            {
                if (b < 64)
                {
                    return b < 32 ? 5 : 6;
                }
                else
                {
                    return b < 128 ? 7 : 8;
                }
            }
        }
    }
}