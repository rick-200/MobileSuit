﻿using System;

namespace PlasticMetal.MobileSuit
{
    /// <summary>
    ///     Indicate that Mobile Suit should Inject to this Object.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public sealed class SuitIncludedAttribute : Attribute
    {
    }
}