using System;
using System.Collections.Generic;
using System.Text;

namespace GeonBit.UI.DataTypes
{
    /// <summary>
    /// Meta data we use for fonts.
    /// The values of these structs are defined in xml files that share the same name as the spritefont with _md suffix.
    /// It tells us the real font we want to use for bold/italic etc.
    /// </summary>
    public class FontData
    {
        /// <summary>Real ttf font name to load at runtime</summary>
        public string FontName;
    }
}
