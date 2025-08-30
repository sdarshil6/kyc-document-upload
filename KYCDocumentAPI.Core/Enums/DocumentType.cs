using System.ComponentModel;

namespace KYCDocumentAPI.Core.Enums
{
    public enum DocumentType
    {
        [Description("Aadhaar Regular")]
        AadhaarRegular = 1,

        [Description("Aadhaar Front")]
        AadhaarFront = 2,

        [Description("Aadhaar Back")]
        AadhaarBack = 3,

        [Description("PAN")]
        PAN = 4,

        [Description("Passport")]
        Passport = 5,

        [Description("Voter Id")]
        VoterId = 6,

        [Description("Driving License")]
        DrivingLicense = 7       
    }
}
