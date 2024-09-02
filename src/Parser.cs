class Parser {
    List<Token> tokens;
    int current = 0;
    List<GlobalUnit> globals;

    public static List<GlobalUnit> FromSource(in string src) {
        return new Parser(src).Parse();
    }

    public static byte[] CompileFromFile(in string path) {
        try {
            var source = File.ReadAllText(path);
            var guList = FromSource(source);

            List<byte> bytes = [];
            foreach (var gdu in guList) {
                bytes.AddRange(gdu.ToByteArray());
            }
            return bytes.ToArray();
        }
        catch (Exception e) {
            ReportError($"Exception reported: {e.Message}");
            return [];
        }
    }

    public static void CompileFromTo(in string inPath, in string outPath) {
        var bytes = CompileFromFile(inPath);
        File.WriteAllBytes(outPath, bytes);
    }

    Parser(in string source) {
        tokens = Lexer.TokenizeSource(source);
        globals = [];
    }

    List<GlobalUnit> Parse() {
        while (!AtEnd()) {
            if (Match(TokenType.FUNCTION)) ParseFunction();
            else if (Match(TokenType.GLOBAL)) ParseGlobal();
            else ReportError(Peek(), "Unexpected token.");
        }
        return globals;
    }

    static void ReportError(Token tok, string msg) {
        ErrorReporter.ReportError($"Parser error at line {tok.Line} (token: {tok}): {msg}");
    }

    static void ReportError(string msg) {
        ErrorReporter.ReportError($"Parser error: {msg}");
    }

    bool AtEnd() => current == tokens.Count;

    Token Advance() {
        var tok = tokens[current];
        current++;
        return tok;
    }

    Token Consume(TokenType tokenType, string msg) {
        var tok = Advance();
        if (tok.TokenT != tokenType) ReportError(tok, msg);
        return tok;
    }

    Token Peek() => tokens[current];
    Token Previous() => tokens[current - 1];

    bool Match(TokenType type) {
        if (Check(type)) {
            Advance();
            return true;
        }
        return false;
    }

    bool Check(TokenType type) {
        if (AtEnd()) return false;
        return Peek().TokenT == type;
    }

    void ParseFunction() {
        var funcname = Consume(TokenType.IDENTIFIER, "Expected function identifier.").IdentString;
        Consume(TokenType.LEFT_BRACE, "Expected function block");

        GlobalUnit unit = new(funcname);

        ulong streamOffset = 0;
        Dictionary<string, ulong> labelOffsets = [];
        Dictionary<ulong, string> jmpStreamLocations = [];

        while (true) {
            if (Match(TokenType.INSTRUCTION)) {
                var ins = Previous();
                InstructionHeader toInsert = new() {
                    Opcode = ins.Instruction
                };

                switch (toInsert.Opcode) {
                    default:
                        HandleOpcodes(toInsert.Opcode, ref streamOffset, ref toInsert, ref unit);
                        break;
                    case OpCode.JMP:
                    case OpCode.JMPIF: {
                        var name = Consume(TokenType.IDENTIFIER, "Expected label identifier.").IdentString;
                        unit.Add(toInsert);
                        jmpStreamLocations[streamOffset] = name;
                        streamOffset++;
                        break;
                    }

                }
            }
            else if (Match(TokenType.LABEL)) {
                var labelName = Consume(TokenType.IDENTIFIER, "Expected label identifier.");
                if (labelOffsets.ContainsKey(labelName.IdentString)) {
                    ReportError(labelName, $"Label {labelName} already exists in this function.");
                }
                labelOffsets[labelName.IdentString] = streamOffset;
            }
            else if (Match(TokenType.RIGHT_BRACE)) {
                break;
            }
            else {
                ReportError(Peek(), "Unexpected token.");
            }
        }

        foreach (var (offset, toLabel) in jmpStreamLocations) {
            if (!labelOffsets.ContainsKey(toLabel)) {
                ReportError($"Unknown label: {toLabel}");
            }
            ulong labelLocation = labelOffsets[toLabel];

            unit.Code[(int) offset] = new() {
                Header = new() {
                    Opcode = unit.Code[(int) offset].Header.Opcode,
                    Data = (int) labelLocation - (int) offset
                }
            };
        }

        globals.Add(unit);
    }

    void ParseGlobal() {
        var globalName = Consume(TokenType.IDENTIFIER, "Expected global identifier.").IdentString;
        Consume(TokenType.LEFT_BRACE, "Expected global data block.");

        GlobalUnit unit = new(globalName);

        while (true) {
            if (Match(TokenType.RIGHT_BRACE)) break;
            else if (Match(TokenType.STRING_LITERAL)) {
                var data = GlobalUnit.CreateInstructionStream(Previous().StringLiteral);
                unit.AddRange(data);
            }
            else if (Match(TokenType.DATA_LITERAL)) {
                unit.Add(new() {
                    Data = Previous().DataLiteral
                });
            }
            else {
                ReportError(Peek(), "Only literals are allowed inside global data block.");
            }
        }
        globals.Add(unit);
    }

    void HandleOpcodes(OpCode op, ref ulong streamOffset, ref InstructionHeader header, ref GlobalUnit unit) {
        var field = typeof(OpCode).GetField(op.ToString());
        var attribute = field?.GetCustomAttributes(typeof(OpCodeArgumentAttribute), false).FirstOrDefault() as OpCodeArgumentAttribute;

        if (attribute is null) {
            ReportError(Previous(), "Could not get argument description for instruction.");
            return;
        }

        switch (attribute.TypeSpecifiers) {
            case OpCodeArgumentAttribute.TypeParams.NONE:
                break;
            case OpCodeArgumentAttribute.TypeParams.ONE: {
                var t0 = Consume(TokenType.EMBED_TYPE, "Expected a type specifier.");
                header.Type0 = t0.EmbedType;
                break;
            }
            case OpCodeArgumentAttribute.TypeParams.TWO: {
                var t0 = Consume(TokenType.EMBED_TYPE, "Expected a type specifier.");
                var t1 = Consume(TokenType.EMBED_TYPE, "Expected a second type specifier.");
                header.Type0 = t0.EmbedType;
                header.Type1 = t1.EmbedType;
                break;
            }
            case OpCodeArgumentAttribute.TypeParams.THREE: {
                var t0 = Consume(TokenType.EMBED_TYPE, "Expected a type specifier.");
                var t1 = Consume(TokenType.EMBED_TYPE, "Expected a second type specifier.");
                var t2 = Consume(TokenType.EMBED_TYPE, "Expected a third type specifier.");
                header.Type0 = t0.EmbedType;
                header.Type1 = t1.EmbedType;
                header.Type2 = t2.EmbedType;
                break;
            }
        }

        switch (attribute.EmbededDataSpecifier) {
            case OpCodeArgumentAttribute.EmbedData.NONE:
                break;
            case OpCodeArgumentAttribute.EmbedData.YES: {
                var data = Consume(TokenType.EMBED_DATA, "Expected embeded data.");
                header.Data = data.EmbedData;
                break;
            }
        }

        unit.Add(header);
        streamOffset++;

        if (attribute.LiteralUsageSpecifier == OpCodeArgumentAttribute.LiteralUsage.DATA_LITERAL) {
            var datalit = Consume(TokenType.DATA_LITERAL, "Expected data literal.").DataLiteral;
            unit.Add(new() {
                Data = datalit
            });
            streamOffset++;
        }
        if (attribute.LiteralUsageSpecifier == OpCodeArgumentAttribute.LiteralUsage.STRING_LITERAL) {
            var strlit = Consume(TokenType.STRING_LITERAL, "Expected string literal.");
            var strstream = GlobalUnit.CreateInstructionStream(strlit.StringLiteral);

            unit.AddRange(strstream);
            streamOffset += (ulong) strstream.Count;
        }
    }
}