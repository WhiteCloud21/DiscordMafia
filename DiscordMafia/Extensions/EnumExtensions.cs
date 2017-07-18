using System;

namespace DiscordMafia.Extensions
{
    public static class EnumExtensions
    {
        public static T SetFlag<T>(this Enum type, T enumFlag, bool value)
        {
            if (value)
            {
                return (T) (object) ((int) (object) type | (int) (object) enumFlag);
            }
            
            
            return (T) (object) ((int) (object) type & ~(int) (object) enumFlag);
        }
    }
}