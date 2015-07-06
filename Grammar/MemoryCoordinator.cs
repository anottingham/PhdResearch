using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Grammar;

namespace Grammar
{
    internal static class MemoryCoordinator
    {

        private static int boolRuleCount;
        private static int intResultCount;
        private static int boolResultCount;

        private static int anonFilterCount;

        private static  List<int> lookupMemory; 

        static MemoryCoordinator()
        {
            lookupMemory = new List<int>();
            Reset();
        }

        public static void Reset()
        {
            boolRuleCount = 0;
            intResultCount = 0;
            boolResultCount = 0;
            anonFilterCount = 0;
            lookupMemory.Clear();
        }

        /// <summary>
        /// Store a static integer in the lookup table
        /// </summary>
        /// <returns>the index of the value in lookup memory</returns>
        public static int RegisterStaticInteger(int value)
        {
            if (!lookupMemory.Contains(value)) lookupMemory.Add(value);
            return lookupMemory.FindIndex(i => i == value);
        }

        /// <summary>
        /// Reserve an index for an boolean rule
        /// </summary>
        /// <returns>the index of the reserved area in memory</returns>
        public static int GetBoolRuleIndex()
        {
            return boolRuleCount++;
        }

        /// <summary>
        /// Reserve an index for an integer result
        /// </summary>
        /// <returns>the index of the reserved area in memory</returns>
        public static int GetIntResultIndex()
        {
            return intResultCount++;
        }

        /// <summary>
        /// Reserve an index for an boolean result
        /// </summary>
        /// <returns>the index of the reserved area in memory</returns>
        public static int GetBoolResultIndex()
        {
            return boolResultCount++;
        }

        public static string GetAnonFilter()
        {
            return "_" + anonFilterCount++;
        }

        public static ProgramMemory GetProgramMemory()
        {
            var mem = new ProgramMemory
            {
                RuleCount = boolRuleCount,
                FilterCount = boolResultCount,
                IntCount = intResultCount,
                LookupMemory = lookupMemory,
            };

            return mem;
        }
    }

    public class ProgramMemory
    {

        public List<int> LookupMemory { get; set; }

        public int RuleCount { get; set; }
        public int FilterCount { get; set; }
        public int IntCount { get; set; }


        public ProgramMemory()
        {
            RuleCount = 0;
            FilterCount = 0;
            IntCount = 0;
            LookupMemory = null;
        }
    }

}
