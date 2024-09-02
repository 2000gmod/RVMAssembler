using System.Runtime.InteropServices;

class GlobalUnit {
    public string Name;
    public List<InstructionUnit> Code;

    public GlobalUnit(string name) {
        Name = name;
        Code = [];
    }

    public GlobalUnit(string name, List<InstructionUnit> code) {
        Name = name;
        Code = [.. code];
    }

    public void Add(in InstructionUnit unit) => Code.Add(unit);
    public void AddRange(in ICollection<InstructionUnit> units) => Code.AddRange(units);


    public byte[] ToByteArray() {
        List<byte> bytes = [];

        var nLength = Name.Length.ToString();

        foreach (var c in nLength) {
            bytes.Add((byte) c);
        }

        bytes.Add(0xFF);

        foreach (var c in Name) {
            bytes.Add((byte) c);
        }

        var cLength = (Code.Count * 8).ToString();

        foreach (var c in cLength) {
            bytes.Add((byte) c);
        }

        bytes.Add(0xFF);
        foreach (var iu in Code) {
            var iubytes = MemoryMarshal.AsBytes<InstructionUnit>([iu]);
            bytes.AddRange(iubytes);
        }

        return bytes.ToArray();
    }

    public static List<InstructionUnit> CreateInstructionStream(string str) {
        List<InstructionUnit> units = [];
        str += '\0';
        int relpos = 0;

        VMValue data = new();

        foreach (var c in str) {
            if (relpos > 7) {
                units.Add(new() {
                    Data = data
                });
                relpos = 0;
                data.i64 = 0;
            }
            unsafe {
                data.array[relpos] = (byte) c;
            }
            relpos++;
        }
        if (relpos != 0) units.Add(new() {
            Data = data
        });

        return units;
    }
}