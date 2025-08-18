using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KYCDocumentAPI.ML.OCR.Enums
{
    public enum PageSegmentationMode
    {
        OsdOnly = 0,
        AutoOsd = 1,
        AutoOnly = 2,
        Auto = 3,
        SingleColumn = 4,
        SingleBlockVertText = 5,
        SingleBlock = 6,
        SingleLine = 7,
        SingleWord = 8,
        CircleWord = 9,
        SingleChar = 10,
        SparseText = 11,
        SparseTextOsd = 12,
        RawLine = 13
    }
}
