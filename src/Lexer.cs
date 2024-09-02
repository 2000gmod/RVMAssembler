using System.Text.RegularExpressions;

enum TokenType {
    EOF,
    FUNCTION,
    GLOBAL,
    LABEL,
    IDENTIFIER,
    INSTRUCTION,
    EMBED_TYPE,
    EMBED_DATA,
    DATA_LITERAL,
    STRING_LITERAL,
    LEFT_BRACE,
    RIGHT_BRACE
}

record Token {
    public TokenType TokenT = TokenType.EOF;
    public string IdentString = "";
    public OpCode Instruction = OpCode.NOP;
    public DataType EmbedType = DataType.NONE;
    public int EmbedData = 0;
    public VMValue DataLiteral = new();
    public string StringLiteral = "";

    public int Line = 0;

    public override string ToString() {
        return TokenT switch {
            TokenType.IDENTIFIER => $"IDENTIFIER: {IdentString}",
            TokenType.INSTRUCTION => $"INSTRUCTION: {Instruction}",
            TokenType.EMBED_TYPE => $"EMBED_TYPE: {EmbedType}",
            TokenType.EMBED_DATA => $"EMBED_DATA: {EmbedData}",
            TokenType.DATA_LITERAL => $"DATA_LITERAL: {DataLiteral.i64}",
            TokenType.STRING_LITERAL => $"STRING_LITERAL: {Regex.Unescape(StringLiteral)}",
            _ => $"{TokenT}"
        };
    }
}

class Lexer {
    readonly string source;
    readonly Dictionary<string, TokenType> keywords;
    readonly Dictionary<string, DataType> datatypes;
    readonly List<Token> tokens = [];

    int start = 0, current = 0, line = 1;

    public static List<Token> TokenizeSource(in string src) {
        var lexer = new Lexer(src);
        return lexer.Tokenize();
    }

    Lexer(in string src) {
        source = src;
        keywords = new() {
            { "function", TokenType.FUNCTION },
            { "global", TokenType.GLOBAL },
            { "label", TokenType.LABEL }
        };

        datatypes = new() {
            { "i8", DataType.I8 },
            { "i16", DataType.I16 },
            { "i32", DataType.I32 },
            { "i64", DataType.I64 },
            { "f32", DataType.F32 },
            { "f64", DataType.F64 },
            { "ptr", DataType.PTR },
        };
    }

    List<Token> Tokenize() {
        while (!AtEnd()) {
            start = current;
            ScanToken();
        }
        return tokens;
    }

    void ScanToken() {
        char c = Advance();

        switch (c) {
            case '\n':
                line++;
                break;
            case ';':
                while(Peek() != '\n' && !AtEnd()) Advance(); 
                break;
            case '{':
                AddTokenT(TokenType.LEFT_BRACE);
                break;
            case '}':
                AddTokenT(TokenType.RIGHT_BRACE);
                break;
            case '@':
                HandleTypeSpecifier();
                break;
            case '!':
                HandleDataLiteral();
                break;
            case '[':
                HandleEmbededData();
                break;
            case '$':
                HandleStringLiteral();
                break;
            case ' ':
            case '\t':
                break;
            default: {
                if (IsAlpha(c)) ScanName();
                else ReportError($"Invalid character: '{c}'");
                break;
            }
        }
    }

    char Advance() {
        char c = source[current];
        current++;
        return c;
    }

    bool AtEnd() => current >= source.Length;

    char Peek() {
        if (AtEnd()) return '\0';
        return source[current];
    }

    static bool IsAlpha(char c) => (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || c == '_';
    static bool IsDigit(char c) => c >= '0' && c <= '9';
    static bool IsAlphaNum(char c) => IsAlpha(c) || IsDigit(c);

    void ReportError(in string msg) {
        ErrorReporter.ReportError($"Lexer error at line {line}: {msg}");
    }

    void AddTokenT(TokenType t) {
        tokens.Add(new() {
            TokenT = t,
            Line = line
        });
    }

    void AddToken(Token tok) {
        tok.Line = line;
        tokens.Add(tok);
    }

    void HandleTypeSpecifier() {
        while (IsAlphaNum(Peek())) Advance();
        var specifier = source.Substring(start + 1, current - start - 1);

        if (!datatypes.ContainsKey(specifier)) {
            ReportError($"Unknown type specifier: '{specifier}'");
        }
        AddToken(new() {
            TokenT = TokenType.EMBED_TYPE,
            EmbedType = datatypes[specifier]
        });
    }

    void HandleDataLiteral() {
        while (IsAlphaNum(Peek())) Advance();
        var specifier = source.Substring(start + 1, current - start - 1);

        if (!datatypes.ContainsKey(specifier)) {
            ReportError($"Unknown type specifier at data literal: '{specifier}'");
        }

        var type = datatypes[specifier];

        while(!IsAlphaNum(Peek())) Advance();
        start = current;
        while(IsAlphaNum(Peek()) || Peek() == '.') Advance();

        string num = source.Substring(start, current - start);

        Token outtok = new() {
            TokenT = TokenType.DATA_LITERAL
        };

        System.Globalization.NumberStyles numStyle;
        
        if (num.StartsWith("0x")) {
            num = num[2..];
            numStyle = System.Globalization.NumberStyles.HexNumber;
        }
        else if (num.StartsWith("0b")) {
            num = num[2..];
            numStyle = System.Globalization.NumberStyles.BinaryNumber;
        }
        else numStyle = System.Globalization.NumberStyles.Integer;

        VMValue literalValue = new();

        try {
            switch (type) {
                case DataType.I8: 
                    literalValue.i8 = sbyte.Parse(num, numStyle);
                    break;
                case DataType.I16:
                    literalValue.i16 = short.Parse(num, numStyle);
                    break;
                case DataType.I32:
                    literalValue.i32 = int.Parse(num, numStyle);
                    break;
                case DataType.I64:
                    literalValue.i64 = long.Parse(num, numStyle);
                    break;
                case DataType.F32:
                    literalValue.f32 = float.Parse(num);
                    break;
                case DataType.F64:
                    literalValue.f64 = double.Parse(num);
                    break;
                case DataType.PTR:
                    literalValue.ptr = IntPtr.Parse(num, numStyle);
                    break;
            }
        }
        catch (OverflowException) {
            ReportError($"Value {num} outside of range of type {type}");
        }
        catch (FormatException) {
            ReportError($"Value {num} has incorrect format for type {type}");
        }

        outtok.DataLiteral = literalValue;

        AddToken(outtok);
    }

    void HandleEmbededData() {
        string num = "";
        while (Peek() != ']') num += Advance();
        Advance();

        Token outtok = new() {
            TokenT = TokenType.EMBED_DATA
        };

        int data;

        if (num.StartsWith("0x")) {
            num = num[2..];
            data = int.Parse(num, System.Globalization.NumberStyles.HexNumber);
        }
        if (num.StartsWith("0b")) {
            num = num[2..];
            data = int.Parse(num, System.Globalization.NumberStyles.BinaryNumber);
        }
        else {
            data = int.Parse(num);
        }
        outtok.EmbedData = data;
        AddToken(outtok);
    }

    void HandleStringLiteral() {
        string str = "";

        if (Advance() != '"') ReportError("Expected string literal");
        
        try {
            while (Peek() != '"') {
                char c = Advance();
                if (c == '\\') {
                    str += Advance() switch {
                        '\\' => '\\',
                        '0' => '\0',
                        'n' => '\n',
                        't' => '\t',
                        'r' => '\r',
                        _ => throw new FormatException()
                    };
                }
                else str += c;
            }
        }
        catch (FormatException) {
            ReportError("Invalid escape sequence");
        }

        Advance();

        AddToken(new Token() {
            TokenT = TokenType.STRING_LITERAL,
            StringLiteral = str,
        });
    }

    void ScanName() {
        while (IsAlphaNum(Peek())) Advance();
        string text = source.Substring(start, current - start);

        Token token = new();

        if (keywords.ContainsKey(text)) {
            token.TokenT = keywords[text];
        }
        else if (InstructionHelper.Mnemonics.ContainsKey(text)) {
            token.TokenT = TokenType.INSTRUCTION;
            token.Instruction = InstructionHelper.Mnemonics[text];
        }
        else {
            token.TokenT = TokenType.IDENTIFIER;
            token.IdentString = text;
        }
        AddToken(token);
    }
}