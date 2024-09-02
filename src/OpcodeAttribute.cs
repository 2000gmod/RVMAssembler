[System.AttributeUsage(AttributeTargets.Field)]
class OpCodeArgumentAttribute : System.Attribute {
    public enum TypeParams {
        NONE,
        ONE,
        TWO,
        THREE
    }

    public enum EmbedData {
        NONE,
        YES
    }

    public enum LiteralUsage {
        NONE,
        DATA_LITERAL,
        STRING_LITERAL
    }

    public readonly TypeParams TypeSpecifiers = TypeParams.NONE;
    public readonly EmbedData EmbededDataSpecifier = EmbedData.NONE;
    public readonly LiteralUsage LiteralUsageSpecifier = LiteralUsage.NONE;

    public OpCodeArgumentAttribute() { }

    public OpCodeArgumentAttribute(TypeParams tps, EmbedData ed, LiteralUsage lu) {
        TypeSpecifiers = tps;
        EmbededDataSpecifier = ed;
        LiteralUsageSpecifier = lu;
    }

    public OpCodeArgumentAttribute(TypeParams tps) {
        TypeSpecifiers = tps;
    }

    public OpCodeArgumentAttribute(EmbedData ed) {
        EmbededDataSpecifier = ed;
    }

    public OpCodeArgumentAttribute(LiteralUsage lu) {
        LiteralUsageSpecifier = lu;
    }
}