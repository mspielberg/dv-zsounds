using System.Linq;

namespace DvMod.ZSounds
{
    public static class LogUtils
    {
        public static string Indent(this string s, int n)
        {
            var indent = string.Concat(Enumerable.Repeat(" ", n));
            return indent + string.Join("\n" + indent, s.Split('\n'));
        }
    }
}