namespace WinAobscanFast.Enums;

[Flags]
public enum CreateToolhelpSnapshotFlags : uint
{
    TH32CS_SNAPHEAPLIST = 0x00000001,
    TH32CS_SNAPPROCESS = 0x00000002,
    TH32CS_SNAPTHREAD = 0x00000004,
    TH32CS_SNAPMODULE = 0x00000008,
    TH32CS_SNAPMODULE32 = 0x00000010,
    TH32CS_SNAPALL = TH32CS_SNAPHEAPLIST
                          | TH32CS_SNAPPROCESS
                          | TH32CS_SNAPTHREAD
                          | TH32CS_SNAPMODULE,
    TH32CS_INHERIT = 0x80000000
}
