using System.Diagnostics;
using System.Runtime.InteropServices;

using static OpCodeArgumentAttribute;

enum OpCode : byte {
    [OpCodeArgument()]
    NOP,

    [OpCodeArgument()]
    HALT,

    [OpCodeArgument(EmbedData.YES)]
    LOAD,

    [OpCodeArgument(EmbedData.YES)]
    STORE,

    [OpCodeArgument(LiteralUsage.DATA_LITERAL)]
    LOADCONST,

    [OpCodeArgument(TypeParams.NONE, EmbedData.YES, LiteralUsage.DATA_LITERAL)]
    STORECONST,

    [OpCodeArgument(TypeParams.TWO)]
    CONVERT,
    
    [OpCodeArgument(TypeParams.ONE)]
    ADD,

    [OpCodeArgument(TypeParams.ONE)]
    SUB,

    [OpCodeArgument(TypeParams.ONE)]
    MUL,

    [OpCodeArgument(TypeParams.ONE)]
    DIV,

    [OpCodeArgument()]
    LAND,

    [OpCodeArgument()]
    LOR,

    [OpCodeArgument()]
    LNOT,

    [OpCodeArgument(TypeParams.ONE)]
    GT,

    [OpCodeArgument(TypeParams.ONE)]
    GEQ,

    [OpCodeArgument(TypeParams.ONE)]
    LT,

    [OpCodeArgument(TypeParams.ONE)]
    LEQ,

    [OpCodeArgument(TypeParams.ONE)]
    EQ,

    [OpCodeArgument(TypeParams.ONE)]
    NOTEQ,
    
    [OpCodeArgument()]
    BAND,

    [OpCodeArgument()]
    BOR,

    [OpCodeArgument()]
    BXOR,

    [OpCodeArgument()]
    BNOT,

    [OpCodeArgument()]
    LSHIFT,

    [OpCodeArgument()]
    RSHIFT,

    [OpCodeArgument(EmbedData.YES)]
    JMP,

    [OpCodeArgument(EmbedData.YES)]
    JMPIF,

    [OpCodeArgument(EmbedData.YES)]
    CREATELOCALS,

    [OpCodeArgument(TypeParams.NONE, EmbedData.YES, LiteralUsage.STRING_LITERAL)]
    CALL,

    [OpCodeArgument(EmbedData.YES)]
    RET,

    [OpCodeArgument(EmbedData.YES)]
    CALLINDIRECT,

    [OpCodeArgument(LiteralUsage.STRING_LITERAL)]
    GETGLOBAL,
}

static class InstructionHelper {
    public static readonly Dictionary<string, OpCode> Mnemonics;

    static InstructionHelper() {
        Mnemonics = [];

        foreach (var op in Enum.GetValues<OpCode>()) {
            Mnemonics.Add(op.ToString().ToLower(), op);
        }
    }
}

enum DataType : byte {
    NONE,
    I8,
    I16,
    I32,
    I64,
    F32,
    F64,
    PTR
}

[StructLayout(LayoutKind.Sequential)]
struct InstructionHeader {
    public OpCode Opcode = OpCode.NOP;
    public DataType Type0 = DataType.NONE;
    public DataType Type1 = DataType.NONE;
    public DataType Type2 = DataType.NONE;
    public int Data = 0;

    public InstructionHeader() { }
}

[StructLayout(LayoutKind.Explicit)]
struct VMValue {
    [FieldOffset(0)] public sbyte i8;
    [FieldOffset(0)] public Int16 i16;
    [FieldOffset(0)] public Int32 i32;
    [FieldOffset(0)] public Int64 i64;
    [FieldOffset(0)] public float f32;
    [FieldOffset(0)] public double f64;
    [FieldOffset(0)] public IntPtr ptr;
    [FieldOffset(0)] public unsafe fixed byte array[8];

    public VMValue() {
        i64 = 0;
    }
}

[StructLayout(LayoutKind.Explicit)]
struct InstructionUnit {
    [FieldOffset(0)] public InstructionHeader Header;
    [FieldOffset(0)] public VMValue Data;

    public InstructionUnit() {
        Data = new();
    }

    public static implicit operator InstructionUnit(InstructionHeader h) {
        return new() {
            Header = h
        };
    }

    static InstructionUnit() {
        Debug.Assert(Marshal.SizeOf<InstructionHeader>() == 8, "InstructionHeader size failed assertion");
        Debug.Assert(Marshal.SizeOf<VMValue>() == 8, "VMValue size failed assertion");
        Debug.Assert(Marshal.SizeOf<InstructionUnit>() == 8, "InstructionUnit size failed assertion");
    }
}