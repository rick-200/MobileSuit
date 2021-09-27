﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlasticMetal.MobileSuit
{
    /// <summary>
    /// Useful static functions for MobileSuit Programming
    /// </summary>
    public static class SuitTools
    {
        /// <summary>
        ///     provides packaging for ContentArray
        /// </summary>
        /// <param name="contents">ContentArray</param>
        /// <returns>packaged ContentArray</returns>
        public static IEnumerable<PrintUnit> CreateContentArray(params (string, ConsoleColor?)[] contents)
        {
            return contents.Select(c => (PrintUnit)c);
        }


        /// <summary>
        ///     provides packaging for ContentArray
        /// </summary>
        /// <param name="contents">ContentArray</param>
        /// <returns>packaged ContentArray</returns>
        public static IEnumerable<PrintUnit> CreateContentArray(
            params (string, ConsoleColor?, ConsoleColor?)[] contents)
        {
            return contents.Select(c => (PrintUnit)c);
        }
    }
}
